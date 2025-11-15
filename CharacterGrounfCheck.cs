using UnityEngine;

public class CharacterGroundCheck : MonoBehaviour
{
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private LayerMask groundLayer;
    
    void Update()
    {
        if (!IsGrounded())
        {
            // Apply gravity manually or reposition
            transform.position += Vector3.down * Time.deltaTime;
        }
    }
    
    bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 
               groundCheckDistance, groundLayer);
    }
}