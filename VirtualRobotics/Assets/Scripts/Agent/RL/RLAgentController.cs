using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AgentMotor))]
public class RLAgentController : Agent
{

    [Header("Rewards")]
    [SerializeField] private float goalReward = 2.0f;
    [SerializeField] private float stepPenalty = -0.0005f;

    [Tooltip("Reward multiplier for getting closer to the goal each physics step.")]
    [SerializeField] private float progressRewardScale = 0.01f;

    [Header("Wall penalty")]
    [SerializeField] private float wallHitPenalty = -0.01f;
    [SerializeField] private float wallPenaltyCooldown = 0.15f;

    [Header("Heuristic (Keyboard)")]
    [SerializeField] private bool enableKeyboardHeuristic = true;
    [SerializeField] private float throttleScale = 1f;
    [SerializeField] private float steerScale = 1f;

    private AgentMotor _motor;
    
    // ZMIANA 1: Agent ma referencję do swojego lokalnego menadżera (Orkiestratora)
    private TrainingMazeManager _localManager; 

    private float _lastWallPenaltyTime = -999f;
    private Transform _goalTf;
    private float _prevDist = 0f;

    public override void Initialize()
    {
        _motor = GetComponent<AgentMotor>();
        
        // Szukamy TrainingMazeManager tylko w obrębie naszego prefaba EnvRoot
        _localManager = GetComponentInParent<TrainingMazeManager>();
    }

    public override void OnEpisodeBegin()
    {
        _lastWallPenaltyTime = -999f;

        // ZMIANA 2: Prosimy LOKALNEGO menadżera o zresetowanie poziomu
        if (_localManager != null)
        {
            _localManager.RefreshLevel();
            _goalTf = _localManager.CurrentGoal; 
        }

        _prevDist = GetDistToGoal();
    }

    private void FixedUpdate()
    {
        ApplyStepRewards();
    }

    private void ApplyStepRewards()
    {
        AddReward(stepPenalty);

        float dist = GetDistToGoal();
        if (float.IsInfinity(dist) || float.IsInfinity(_prevDist))
        {
            _prevDist = dist;
            return;
        }

        float progress = _prevDist - dist; 
        progress = Mathf.Clamp(progress, -1f, 1f);

        AddReward(progress * progressRewardScale);
        _prevDist = dist;
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

        // Tutaj Tag "Goal" jest w porządku, bo reagujemy tylko na fizyczne dotknięcie
        if (other.CompareTag("Goal")) 
        {
            AddReward(goalReward);
            EndEpisode(); // To wywoła OnEpisodeBegin w następnej klatce
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!enabled) return;
        if (!collision.collider.CompareTag("Wall")) return;

        if (Time.time - _lastWallPenaltyTime < wallPenaltyCooldown) return;
        _lastWallPenaltyTime = Time.time;

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

    // ZMIANA 3: Uproszczone obliczanie dystansu na podstawie bezpośredniej referencji
    private float GetDistToGoal()
    {
        if (_goalTf == null) return float.PositiveInfinity;

        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = _goalTf.position;   b.y = 0f;
        return Vector3.Distance(a, b);
    }
}