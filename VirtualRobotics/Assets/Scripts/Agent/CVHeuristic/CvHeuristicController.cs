using UnityEngine;

[RequireComponent(typeof(AgentMotor))]
public class CvHeuristicController : MonoBehaviour
{
    [SerializeField] private AgentMotor motor;
    
    [Header("Evaluation Settings")]
    [SerializeField] private int maxSteps = 5000;
    
    private int _stepCount = 0;
    private bool _episodeEnded = false; // Żeby nie logować wielokrotnie

    private void Awake()
    {
        if (!motor) motor = GetComponent<AgentMotor>();
    }

    private void FixedUpdate()
    {
        // Jeśli agent już wygrał/przegrał i czeka na reset mapy, nic nie robimy
        if (_episodeEnded || EvaluationEnvManager.Instance == null) return;

        _stepCount++;

        // SPRAWDZAMY TIMEOUT
        if (_stepCount >= maxSteps)
        {
            _episodeEnded = true;
            Debug.LogWarning("[CV Agent] Timeout! Przekroczono limit kroków.");
            
            EvaluationEnvManager.Instance.LogResult(maxSteps, false);
            EvaluationEnvManager.Instance.GenerateNewLevel();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!enabled || _episodeEnded || EvaluationEnvManager.Instance == null) return;

        // SPRAWDZAMY SUKCES
        if (other.CompareTag("Goal"))
        {
            _episodeEnded = true;
            EvaluationEnvManager.Instance.LogResult(_stepCount, true);
            EvaluationEnvManager.Instance.GenerateNewLevel();
        }
    }

    public void MoveForward(float distance)
    {
        float throttle = Mathf.Clamp(distance, -1f, 1f);
        motor.Apply(throttle, 0f);
    }

    public void RotateDegrees(float degrees)
    {
        float steer = Mathf.Clamp(degrees / 90f, -1f, 1f);
        motor.Apply(0f, steer);
    }

    public void ResetAgent(Vector3 position)
    {
        motor.ResetPose(position, Quaternion.identity);
    }
}