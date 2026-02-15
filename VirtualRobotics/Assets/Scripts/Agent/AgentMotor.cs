using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AgentMotor : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnSpeed = 120f;
    [SerializeField] private bool noReverse = true;

    [Header("Stability (Idiot-proof)")]
    [Tooltip("Kills yaw spin coming from collisions/physics solver. Recommended ON for predictable env.")]
    [SerializeField] private bool killPhysicsYawSpin = true;

    [Tooltip("If velocity is smaller than this, snap it to zero to avoid micro-drift.")]
    [SerializeField] private float stopLinearThreshold = 0.03f;

    [Tooltip("If angular velocity is smaller than this, snap it to zero to avoid micro-rotation.")]
    [SerializeField] private float stopAngularThreshold = 0.05f;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Keep upright by constraints (this is enough).
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // Strongly recommended defaults for stable, predictable movement:
        // (Set in Inspector if you prefer, but this ensures sane behavior.)
        if (_rb.linearDamping < 1.5f) _rb.linearDamping = 1.5f;
        if (_rb.angularDamping < 2.0f) _rb.angularDamping = 2.0f;

        // Optional: better collision stability for moving RBs
        if (_rb.collisionDetectionMode == CollisionDetectionMode.Discrete)
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Optional: smoother visuals
        if (_rb.interpolation == RigidbodyInterpolation.None)
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    /// <summary>
    /// Apply movement using continuous controls in range [-1..1].
    /// throttle: forward/back, steer: left/right.
    /// </summary>
    public void Apply(float throttle, float steer)
    {
        throttle = Mathf.Clamp(throttle, -1f, 1f);
        steer = Mathf.Clamp(steer, -1f, 1f);
        if (noReverse) throttle = Mathf.Clamp01(throttle);

        float dt = Time.fixedDeltaTime;

        // Rotate (yaw)
        float deltaYaw = steer * turnSpeed * dt;
        _rb.MoveRotation(_rb.rotation * Quaternion.Euler(0f, deltaYaw, 0f));

        // Forward velocity (keep Y for gravity)
        Vector3 v = _rb.linearVelocity;
        Vector3 desired = transform.forward * (throttle * moveSpeed);
        _rb.linearVelocity = new Vector3(desired.x, v.y, desired.z);
    }

    public void ResetPose(Vector3 position, Quaternion rotation)
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;

        _rb.position = position;
        _rb.rotation = rotation;
    }

    public void ResetPose(Vector3 position) => ResetPose(position, Quaternion.identity);

    private void FixedUpdate()
    {
        if (!killPhysicsYawSpin) return;

        var av = _rb.angularVelocity;
        av.y = 0f;
        _rb.angularVelocity = av;
    }

}
