using UnityEngine;

[RequireComponent(typeof(AgentMotor))]
public class CvHeuristicController : MonoBehaviour
{
    [SerializeField] private AgentMotor motor;
    
    [Header("Evaluation Settings")]
    [SerializeField] private int maxSteps = 5000;
    
    private int _stepCount = 0;
    private bool _episodeEnded = false;

    // Stan wirtualnego "pada"
    private float _currentThrottle = 0f;
    private float _currentSteer = 0f;

    private void Awake()
    {
        if (!motor) motor = GetComponent<AgentMotor>();
    }
    
    
    /// <summary>
    /// Ustawia wirtualny joystick. 
    /// throttle: [0, 1] (Tylko jazda do przodu, brak wstecznego)
    /// steer: [-1, 1] (Lewo / Prawo)
    /// </summary>
    public void SetControl(float throttle, float steer)
    {
        // Zmienione Clamp na [0, 1] - odcinamy próby cofania
        _currentThrottle = Mathf.Clamp(throttle, 0f, 1f); 
        _currentSteer = Mathf.Clamp(steer, -1f, 1f);
    }

    /// <summary>
    /// Zatrzymuje agenta (resetuje joystick do zera)
    /// </summary>
    public void StopAgent()
    {
        _currentThrottle = 0f;
        _currentSteer = 0f;
    }

    // =========================================================

    private void FixedUpdate()
    {
        // Jeśli czekamy na reset mapy, nic nie robimy
        if (_episodeEnded || EvaluationEnvManager.Instance == null) return;

        // 1. APLIKUJEMY RUCH Z "PADA" CO KLATKĘ (jak RL Agent)
        motor.Apply(_currentThrottle, _currentSteer);

        // 2. LICZYMY KROKI
        _stepCount++;

        // 3. SPRAWDZAMY TIMEOUT (Porażka)
        if (_stepCount >= maxSteps)
        {
            _episodeEnded = true;
            Debug.LogWarning("[CV] Timeout! Przekroczono limit kroków.");
            
            StopAgent();
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
            StopAgent(); // Hamujemy przed startem nowej mapy
            EvaluationEnvManager.Instance.LogResult(_stepCount, true);
            EvaluationEnvManager.Instance.GenerateNewLevel();
        }
    }

    public void ResetAgent(Vector3 position)
    {
        StopAgent();
        motor.ResetPose(position, Quaternion.identity);
    }
}