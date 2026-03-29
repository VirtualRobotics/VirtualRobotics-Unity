using System.Collections;
using UnityEngine;

[System.Serializable]
public class CvAction
{
    public float throttle;
    public float steer;
}

[RequireComponent(typeof(AgentMotor))]
public class CvHeuristicController : MonoBehaviour
{
    private AgentMotor _motor;
    private TcpClientController _tcp;
    
    private int _stepCount = 0;
    private bool _episodeEnded = false;

    private void Awake()
    {
        _motor = GetComponent<AgentMotor>();
    }

    private void Start()
    {
        _tcp = FindFirstObjectByType<TcpClientController>();
        
        if (_tcp == null)
        {
            Debug.LogError("[CV] TcpClientController not found in scene!");
            enabled = false;
        }
    }

    private void FixedUpdate()
    {
        if (_episodeEnded || _tcp == null) return;

        _stepCount++;

        if (!string.IsNullOrEmpty(_tcp.LatestJsonResponse))
        {
            CvAction action = JsonUtility.FromJson<CvAction>(_tcp.LatestJsonResponse);
            _motor.Apply(action.throttle, action.steer);
        }
    }

    // --- Evaluation & Environment Reset Logic ---

    private void OnTriggerEnter(Collider other)
    {
        if (_episodeEnded) return;

        if (other.CompareTag("Goal"))
        {
            _episodeEnded = true;
            _motor.Apply(0, 0);
            
            if (EvaluationEnvManager.Instance != null)
            {
                EvaluationEnvManager.Instance.LogResult(_stepCount, true);
                StartCoroutine(GenerateNextLevelDelayed());
            }
        }
    }

    private IEnumerator GenerateNextLevelDelayed()
    {
        yield return new WaitForEndOfFrame();
        EvaluationEnvManager.Instance.GenerateNewLevel();
    }
}