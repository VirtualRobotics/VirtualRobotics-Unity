using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine.AI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AgentMotor))]
public class RLAgentController : Agent
{
    private const int InitialMaxStep = 2000; 
    [Header("Rewards")]
    [SerializeField] private float goalReward = 1f;
    [SerializeField] private float stepPenalty = -0.001f;

    [Header("Wall penalty")]
    [SerializeField] private float wallHitPenalty = -0.1f;
    [Header("Path Guidance (NavMesh)")]
    [SerializeField] private float pathGuidanceMultiplier = 0.00f; // "Training Wheels"

    [Header("Heuristic (Keyboard)")]
    [SerializeField] private bool enableKeyboardHeuristic = true;
    [SerializeField] private float throttleScale = 1f;
    [SerializeField] private float steerScale = 1f;
    
    private int _stepsAtSuccess = 0;
    private int _wallHitsThisEpisode = 0;
    private Transform _goalTf;
    private bool _wasSuccessful = false;

    private NavMeshPath _navPath;
    private float _prevPathDistance;
    private AgentMotor _motor;
    // ZMIANA 1: Agent ma referencję do swojego lokalnego menadżera (Orkiestratora)
    private MazeBuilder _localManager; 
    
    public override void Initialize()
    {
        _motor = GetComponent<AgentMotor>();
        
        // Szukamy TrainingMazeManager tylko w obrębie naszego prefaba EnvRoot
        _localManager = GetComponentInParent<MazeBuilder>();

       _navPath = new NavMeshPath();
    }

    public override void OnEpisodeBegin()
    {
        LogEpisodeStatistics();
    
        DetermineLevelParameters(out int width, out int height, out bool isEmpty);
    
        SetupEnvironment(width, height, isEmpty);
    }

// 1. Metoda od statystyk - czyści głowę agenta przed nowym startem
    private void LogEpisodeStatistics()
    {
        if (CompletedEpisodes > 0)
        {

            // Jeśli był sukces -> bierzemy zapamiętany krok sukcesu.
            // Jeśli była porażka -> oznacza to Timeout, więc bierzemy MaxStep.
            int totalStepsInLastEpisode = _wasSuccessful ? _stepsAtSuccess : MaxStep;
            Academy.Instance.StatsRecorder.Add("Custom/EpTotSteps", totalStepsInLastEpisode);
        
            // 2. Kroki do sukcesu - wysyłamy TYLKO jeśli był sukces
            if (_wasSuccessful)
            {
                Academy.Instance.StatsRecorder.Add("Custom/SuccessEpTotSteps", _stepsAtSuccess);
            }
            // Sukces (1) lub Porażka (0)
            Academy.Instance.StatsRecorder.Add("Custom/SuccessRate", _wasSuccessful ? 1.0f : 0.0f);

            // 4. Liczba zderzeń ze ścianą
            Academy.Instance.StatsRecorder.Add("Custom/WallHits", _wallHitsThisEpisode);
        }

        // RESET liczników na nowy epizod
        _wasSuccessful = false;
        _stepsAtSuccess = 0;
        _wallHitsThisEpisode = 0;
    }

// 2. Metoda decyzyjna - tu ustalamy "co" budujemy (Curriculum vs Inspektor)
    private void DetermineLevelParameters(out int width, out int height, out bool isEmpty)
    {
        float clDifficulty = Academy.Instance.EnvironmentParameters.GetWithDefault("maze_difficulty", -1.0f);

        if (clDifficulty >= 0)
        {
            SetCurriculumDifficulty((int)clDifficulty, out width, out height, out isEmpty);
        }
        else
        {
            width = TrainingEnvManager.Instance.MazeWidth;
            height = TrainingEnvManager.Instance.MazeHeight;
            isEmpty = TrainingEnvManager.Instance.GenerateEmptyMaze;
            MaxStep = InitialMaxStep; 
        }
    }

// 3. Metoda wykonawcza - tu faktycznie stawiamy ściany
    private void SetupEnvironment(int width, int height, bool isEmpty)
    {
        if (_localManager != null)
        {
            _localManager.RefreshLevel(width, height, isEmpty);
            _goalTf = _localManager.CurrentGoal;
        }
    }

    // Wydzielona metoda dla przejrzystości "Planu Lekcji"
    private void SetCurriculumDifficulty(int lesson, out int w, out int h, out bool empty)
    {
        // Disciplined Scaling: Skalujemy MaxStep proporcjonalnie do trudności nawigacji
        switch (lesson)
        {
            case 0: // Lesson 0: Korytarz (3x1 area + walls)
                w = 5; h = 3; empty = true;  MaxStep = 400;  break;
            case 1: // Lesson 1: Mały kwadrat (3x3 area + walls)
                w = 5; h = 5; empty = false; MaxStep = 800;  break;
            case 2: // Lesson 2: Średni labirynt (5x5 area + walls)
                w = 7; h = 7; empty = false; MaxStep = 1200; break;
            case 3: // Lesson 3: Duży labirynt (9x9 area + walls)
                w = 11; h = 11; empty = false; MaxStep = 3000; break; 
            default: 
                w = 11; h = 11; empty = false; MaxStep = 3000; break;
        }
    }
    //---------------------------------------------------------
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        float throttle = actions.ContinuousActions[0];
        float steer = actions.ContinuousActions[1];
        _motor.Apply(throttle, steer);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled) return;
        
        if (other.CompareTag("Goal")) 
        {
            _wasSuccessful = true; // Zaznaczamy, że to był sukces
            _stepsAtSuccess = StepCount;
            
            AddReward(goalReward);
            EndEpisode(); // To wywoła OnEpisodeBegin w następnej klatce
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enabled) return;
        if (!collision.collider.CompareTag("Wall")) return;
        
        _wallHitsThisEpisode++; 

        AddReward(wallHitPenalty);
    }
    
    private void FixedUpdate()
    {
        AddReward(stepPenalty);
        //Uncomment to apply reward shaping
        // ApplyPathGuidanceReward();
    }
    
    /// <summary>
    /// Nagradza agenta za skracanie dystansu do celu wzdłuż faktycznej ścieżki (nie przez ściany).
    /// </summary>
    private void ApplyPathGuidanceReward()
    {
        if (pathGuidanceMultiplier <= 0 || _goalTf == null) return;
    
        float currentPathDist = GetNavMeshDistance();
        float diff = _prevPathDistance - currentPathDist;
    
        if (diff > 0) 
        {
            // Przyznajemy nagrodę za postęp (np. 1 metr bliżej celu = +0.01 nagrody)
            AddReward(diff * pathGuidanceMultiplier); 
        }
    
        _prevPathDistance = currentPathDist;
    }

     private float GetNavMeshDistance() {
         // Obliczamy ścieżkę po NavMesh. Jeśli się uda, sumujemy długość segmentów.
         if (NavMesh.CalculatePath(transform.position, _goalTf.position, NavMesh.AllAreas, _navPath)) {
             return GetPathLength(_navPath);
         }
         // Fallback do dystansu euklidesowego, jeśli NavMesh jeszcze nie "wstał"
         return Vector3.Distance(transform.position, _goalTf.position); // Fallback
     }

     private float GetPathLength(NavMeshPath path) {
         float lng = 0.0f;
         for (int i = 1; i < path.corners.Length; i++) {
             lng += Vector3.Distance(path.corners[i - 1], path.corners[i]);
         }
         return lng;
     }
    //---------------------------------------------------------
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var a = actionsOut.ContinuousActions;

        if (!enableKeyboardHeuristic)
        {
            a[0] = 0f;
            a[1] = 0f;
            return;
        }

        var kb = Keyboard.current;
        if (kb == null)
        {
            a[0] = 0f;
            a[1] = 0f;
            return;
        }

        float throttle = 0f;
        float steer = 0f;

        if (kb.upArrowKey.isPressed) throttle += 1f;
        if (kb.downArrowKey.isPressed) throttle -= 1f;
        if (kb.leftArrowKey.isPressed) steer -= 1f;
        if (kb.rightArrowKey.isPressed) steer += 1f;

        a[0] = Mathf.Clamp(throttle * throttleScale, -1f, 1f);
        a[1] = Mathf.Clamp(steer * steerScale, -1f, 1f);
    }
}