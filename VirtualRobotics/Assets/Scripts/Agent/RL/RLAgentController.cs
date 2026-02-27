using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AgentMotor))]
public class RLAgentController : Agent
{
    [Header("Episode")]
    [SerializeField] private bool generateNewMazeOnReset = true;

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

    private float _lastWallPenaltyTime = -999f;

    private Transform _goalTf;
    private float _prevDist = 0f;

    public override void Initialize()
    {
        _motor = GetComponent<AgentMotor>();
    }

    public override void OnEpisodeBegin()
    {
        _lastWallPenaltyTime = -999f;

        if (MazeManager.Instance != null)
        {
            if (generateNewMazeOnReset) MazeManager.Instance.GenerateNewLevel();
            else MazeManager.Instance.ResetAgentPositionOnly();
        }

        // reacquire goal after regen/reset
        _goalTf = FindGoalTransform();
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

        float progress = _prevDist - dist; // >0 when closer
        // optional safety clamp to avoid crazy spikes
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

        if (other.CompareTag("Goal"))
        {
            AddReward(goalReward);
            EndEpisode();
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
    //TODO CO DO KURWY, CZY TO KORZYSTA Z AGENTMOTOR?
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

    // ------------------------
    // Helpers
    // ------------------------

    private Transform FindGoalTransform()
    {
        var g = GameObject.FindGameObjectWithTag("Goal");
        return g ? g.transform : null;
    }

    private float GetDistToGoal()
    {
        if (_goalTf == null)
        {
            _goalTf = FindGoalTransform();
            if (_goalTf == null) return float.PositiveInfinity;
        }

        // planar distance (ignore Y)
        Vector3 a = transform.position; a.y = 0f;
        Vector3 b = _goalTf.position;   b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
