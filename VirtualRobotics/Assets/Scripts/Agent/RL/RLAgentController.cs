using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody))]
public class RLAgentController : Agent
{
    [Header("Game Settings")]
    public bool generateNewMazeOnReset = true;
    public float maxTimePerLevel = 120f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float turnSpeed = 120f;

    private Rigidbody _rb;
    private float _timeSinceStart;

    public override void Initialize()
    {
        _rb = GetComponent<Rigidbody>();
        // Freeze rotation to prevent the robot from tipping over
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public override void OnEpisodeBegin()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        
        _timeSinceStart = 0f;

        if (generateNewMazeOnReset && MazeManager.Instance != null)
        {
            MazeManager.Instance.GenerateNewLevel();
        }
    }

    // Update loop to track time during the episode
    void Update()
    {
        _timeSinceStart += Time.deltaTime;
        
        // Failsafe: If time runs out, give up and try a new map
        if (_timeSinceStart > maxTimePerLevel)
        {
            Debug.Log("Time's up! Resetting agent.");
            EndEpisode();
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 1. Receive decisions from neural network
        float move = actions.ContinuousActions[0]; 
        float rotate = actions.ContinuousActions[1];

        // 2. Execute movement
        Vector3 moveDir = transform.forward * (Mathf.Clamp(move, -1f, 1f) * moveSpeed * Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + moveDir);

        float turnDir = Mathf.Clamp(rotate, -1f, 1f) * turnSpeed * Time.fixedDeltaTime;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0, turnDir, 0));
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var actions = actionsOut.ContinuousActions;
        actions[0] = Input.GetAxisRaw("Vertical");
        actions[1] = Input.GetAxisRaw("Horizontal");
    }

    private void OnTriggerEnter(Collider other)
    {   
        if (!this.enabled) return;
        if (other.CompareTag("Goal"))
        {
            Debug.Log("AI: Goal reached!");
            EndEpisode();
        }
    }
}