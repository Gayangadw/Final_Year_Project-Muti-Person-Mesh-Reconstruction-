using UnityEngine;

public class PhysicsDiagnostic : MonoBehaviour
{
    void Start()
    {
        Debug.Log("=== PHYSICS DIAGNOSTIC STARTED ===");
        CheckPhysicsComponents();
        CheckGroundPresence();
    }

    void CheckPhysicsComponents()
    {
        Debug.Log("Checking physics components for: " + gameObject.name);

        // Check Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log("Rigidbody found:");
            Debug.Log(" - useGravity: " + rb.useGravity);
            Debug.Log(" - isKinematic: " + rb.isKinematic);
            Debug.Log(" - collisionDetection: " + rb.collisionDetectionMode);
        }
        else
        {
            Debug.LogError("No Rigidbody component found!");
        }

        // Check CharacterController
        CharacterController controller = GetComponent<CharacterController>();
        if (controller != null)
        {
            Debug.Log("CharacterController found:");
            Debug.Log(" - isGrounded: " + controller.isGrounded);
        }

        // Check Colliders
        Collider[] colliders = GetComponents<Collider>();
        Debug.Log("Colliders found: " + colliders.Length);
        foreach (Collider col in colliders)
        {
            Debug.Log(" - " + col.GetType() + " enabled: " + col.enabled);
        }
    }

    void CheckGroundPresence()
    {
        Debug.Log("Checking for ground...");

        // Raycast downward to find ground
        RaycastHit hit;
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f))
        {
            Debug.Log("Ground found below character:");
            Debug.Log(" - Ground object: " + hit.collider.gameObject.name);
            Debug.Log(" - Distance: " + hit.distance);
            Debug.Log(" - Ground layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

            // Check if ground has collider
            if (hit.collider == null)
            {
                Debug.LogError("Ground object has NO COLLIDER!");
            }
        }
        else
        {
            Debug.LogError("NO GROUND FOUND BELOW CHARACTER! This is likely the problem.");
            Debug.Log("No objects with colliders detected below the character within 10 units.");
        }

        // Check collision matrix
        Debug.Log("Character layer: " + LayerMask.LayerToName(gameObject.layer));
    }

    void Update()
    {
        // Continuous ground check
        if (Physics.Raycast(transform.position, Vector3.down, 2f))
        {
            Debug.Log("Character is currently above ground");
        }
        else
        {
            Debug.LogWarning("Character is NOT above ground - falling!");
        }
    }
}

