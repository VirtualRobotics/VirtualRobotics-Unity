using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HeuristicMovement : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float rotateSpeed = 90f;
    private Rigidbody _rb;

    void Awake() => _rb = GetComponent<Rigidbody>();

    public void MoveForward(float distance)
    {
        Vector3 targetPosition = _rb.position + transform.forward * distance;
        _rb.MovePosition(targetPosition);
    }
    
    public void RotateDegrees(float degrees)
    {
        Quaternion deltaRot = Quaternion.Euler(0f, degrees, 0f);
        _rb.MoveRotation(_rb.rotation * deltaRot);
        
        _rb.angularVelocity = Vector3.zero;
    }

    public void ResetAgent(Vector3 position)
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.position = position;
        transform.rotation = Quaternion.identity;
    }
    
    private void OnTriggerEnter(Collider other)
    {   
        if (!this.enabled) return;
        if (other.CompareTag("Goal"))
        {
            Debug.Log("[AGENT] Cel osiągnięty! Resetowanie poziomu...");
            if (MazeManager.Instance != null)
            {
                MazeManager.Instance.GenerateNewLevel();
            }
        }
    }
}