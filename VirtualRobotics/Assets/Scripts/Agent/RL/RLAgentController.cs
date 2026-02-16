using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AgentMotor))]
public class RLAgentController : Agent
{
    [Header("Episode")]
    [SerializeField] private bool generateNewMazeOnReset = true;

    [Header("Rewards (minimal)")]
    [SerializeField] private float goalReward = 1.0f;
    [SerializeField] private float stepPenalty = -0.0005f;

    [Header("Wall penalty")]
    [SerializeField] private float wallHitPenalty = -0.01f;
    [SerializeField] private float wallPenaltyCooldown = 0.15f;

    [Header("Heuristic (Keyboard)")]
    [SerializeField] private bool enableKeyboardHeuristic = true;
    [SerializeField] private float throttleScale = 1f;
    [SerializeField] private float steerScale = 1f;

    private AgentMotor _motor;
    private float _lastWallPenaltyTime = -999f;

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
    }

    private void FixedUpdate()
    {
        // One-step living cost. Works nicely with Max Step as the episode limiter.
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
