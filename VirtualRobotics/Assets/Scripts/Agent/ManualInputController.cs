using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AgentMotor))]
public class ManualInputController : MonoBehaviour
{
    [Header("Tuning")]
    [SerializeField] private float throttleScale = 1f;
    [SerializeField] private float steerScale = 1f;

    private AgentMotor _motor;

    private void Awake()
    {
        _motor = GetComponent<AgentMotor>();
    }

    private void FixedUpdate()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float throttle = 0f;
        float steer = 0f;

        if (kb.upArrowKey.isPressed) throttle += 1f;
        if (kb.downArrowKey.isPressed) throttle -= 1f;
        if (kb.leftArrowKey.isPressed) steer -= 1f;
        if (kb.rightArrowKey.isPressed) steer += 1f;

        throttle *= throttleScale;
        steer *= steerScale;

        _motor.Apply(throttle, steer);
    }
}