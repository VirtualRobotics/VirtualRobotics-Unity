using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AgentMotor))]
public class RLAgentController : Agent
{

    [Header("Rewards")]
    [SerializeField] private float goalReward = 1f;
    [SerializeField] private float stepPenalty = -0.001f;

    [Header("Wall penalty")]
    [SerializeField] private float wallHitPenalty = -0.02f;

    [Header("Heuristic (Keyboard)")]
    [SerializeField] private bool enableKeyboardHeuristic = true;
    [SerializeField] private float throttleScale = 1f;
    [SerializeField] private float steerScale = 1f;

    private AgentMotor _motor;
    
    // ZMIANA 1: Agent ma referencję do swojego lokalnego menadżera (Orkiestratora)
    private TrainingMazeManager _localManager; 
    
    private Transform _goalTf;
    private float _prevDist = 0f;
    private bool _wasSuccessful = false;

    public override void Initialize()
    {
        _motor = GetComponent<AgentMotor>();
        
        // Szukamy TrainingMazeManager tylko w obrębie naszego prefaba EnvRoot
        _localManager = GetComponentInParent<TrainingMazeManager>();
    }

    public override void OnEpisodeBegin()
    {
        // Sprawdzamy, czy to nie jest pierwsze uruchomienie (CompletedEpisodes > 0)
        // Jeśli poprzedni epizod się skończył, a flaga sukcesu jest false -> to był Timeout
        if (CompletedEpisodes > 0 && !_wasSuccessful)
        {
            Academy.Instance.StatsRecorder.Add("Custom/SuccessRate", 0.0f);
        }
        _wasSuccessful = false; // Resetujemy flagę na nowy epizod
        
        if (_localManager != null)
        {
            _localManager.RefreshLevel();
            _goalTf = _localManager.CurrentGoal; 
        }
        
    }

    private void FixedUpdate()
    {
        AddReward(stepPenalty);
    }
    

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
            Academy.Instance.StatsRecorder.Add("Custom/SuccessRate", 1.0f);
            
            AddReward(goalReward);
            EndEpisode(); // To wywoła OnEpisodeBegin w następnej klatce
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enabled) return;
        if (!collision.collider.CompareTag("Wall")) return;

        AddReward(wallHitPenalty);
    }

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