using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class AgentController : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotateSpeed = 90f;
    
    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float vertical = keyboard.wKey.isPressed ? 1f : keyboard.sKey.isPressed ? -1f : 0f;
        float horizontal = keyboard.dKey.isPressed ? 1f : keyboard.aKey.isPressed ? -1f : 0f;

        Vector3 move = transform.forward * (vertical * moveSpeed * Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + move);

        Quaternion rotate = Quaternion.Euler(0f, horizontal * rotateSpeed * Time.fixedDeltaTime, 0f);
        _rb.MoveRotation(_rb.rotation * rotate);
    }
    public void MoveForward(float distance)
    {
        Vector3 move = transform.forward * distance;
        _rb.MovePosition(_rb.position + move);
    }
    
    public void RotateDegrees(float degrees)
    {
        Quaternion deltaRot = Quaternion.Euler(0f, degrees, 0f);
        _rb.MoveRotation(_rb.rotation * deltaRot);
    }
}
