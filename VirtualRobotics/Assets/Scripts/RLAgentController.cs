using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(Rigidbody))]
public class RLAgentController : Agent
{
    [Header("Ustawienia Gry")]
    public bool generateNewMazeOnReset = true;
    public float maxTimePerLevel = 60f;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float turnSpeed = 120f;

    private Rigidbody _rb;
    private float _timeSinceStart;

    public override void Initialize()
    {
        _rb = GetComponent<Rigidbody>();
        // Zamrażamy rotacje, żeby robot się nie przewracał
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

    // Dodajemy Update, żeby pilnować czasu w trybie gry
    void Update()
    {
        _timeSinceStart += Time.deltaTime;
        
        // Bezpiecznik: Jeśli minął czas, poddaj się i spróbuj nową mapę
        if (_timeSinceStart > maxTimePerLevel)
        {
            Debug.Log("Czas minął! Resetowanie agenta.");
            EndEpisode();
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // 1. Odbiór decyzji z sieci neuronowej
        float move = actions.ContinuousActions[0]; 
        float rotate = actions.ContinuousActions[1];

        // 2. Wykonanie ruchu
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
            Debug.Log("AI: Cel osiągnięty!");
            EndEpisode();
        }
    }
}