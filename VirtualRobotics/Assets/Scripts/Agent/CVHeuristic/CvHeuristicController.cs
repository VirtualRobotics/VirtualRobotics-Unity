using UnityEngine;

[RequireComponent(typeof(AgentMotor))]
public class CvHeuristicController : MonoBehaviour
{
    [SerializeField] private AgentMotor motor;

    private void Awake()
    {
        if (!motor) motor = GetComponent<AgentMotor>();
    }

    // API pod TcpClientController (zachowujemy te same nazwy komend)
    public void MoveForward(float distance)
    {
        // distance -> przeliczamy na "throttle" na 1 fixed step:
        // najprościej: potraktuj distance jako "ile jednostek w tej klatce"
        // i przemapuj to na throttle w [-1..1]
        float throttle = Mathf.Clamp(distance, -1f, 1f);
        motor.Apply(throttle, 0f);
    }

    public void RotateDegrees(float degrees)
    {
        // degrees -> podobnie mapujemy na steer
        float steer = Mathf.Clamp(degrees / 90f, -1f, 1f); // 90deg => 1.0 steer (heurystycznie)
        motor.Apply(0f, steer);
    }

    public void ResetAgent(Vector3 position)
    {
        motor.ResetPose(position, Quaternion.identity);
    }
}