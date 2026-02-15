using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;

[RequireComponent(typeof(AgentMotor))]
public class RLAgentController : Agent
{
    [Header("Episode")]
    public bool generateNewMazeOnReset = true;
    public float maxTimePerLevel = 120f;

    private AgentMotor _motor;
    private float _timeSinceStart;

    public override void Initialize()
    {
        _motor = GetComponent<AgentMotor>();
    }

    public override void OnEpisodeBegin()
    {
        _timeSinceStart = 0f;

        if (MazeManager.Instance != null)
        {
            if (generateNewMazeOnReset) MazeManager.Instance.GenerateNewLevel();
            else MazeManager.Instance.ResetAgentPositionOnly();
        }
    }

    private void Update()
    {
        _timeSinceStart += Time.deltaTime;
        if (_timeSinceStart > maxTimePerLevel)
        {
            EndEpisode();
        }
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
            EndEpisode();
        }
    }
}