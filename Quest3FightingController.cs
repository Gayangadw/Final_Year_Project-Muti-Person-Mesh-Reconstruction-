using UnityEngine;

public class Quest3FightingController : MonoBehaviour
{
    [Header("Player References")]
    public CharacterController characterController;
    public Animator animator;
    public Transform[] opponents;

    [Header("Movement Settings")]
    public float movementSpeed = 2f;
    public float rotationSpeed = 10f;

    [Header("Combat Settings")]
    public float attackCooldown = 0.5f;
    public int attackDamage = 5;
    public string[] attackAnimations = { "Attack1Animation", "Attack2Animation", "Attack3Animation", "Attack4Animation" };
    public float dodgeDistance = 2f;
    public float attackRadius = 2.2f;
    private float lastAttackTime;

    [Header("Quest 3 Controller Bindings")]
    [Tooltip("X Button")]
    public OVRInput.Button attack1Button = OVRInput.Button.One;
    [Tooltip("Y Button")]
    public OVRInput.Button attack2Button = OVRInput.Button.Two;
    [Tooltip("A Button")]
    public OVRInput.Button attack3Button = OVRInput.Button.One;
    [Tooltip("B Button")]
    public OVRInput.Button attack4Button = OVRInput.Button.Two;
    [Tooltip("Left Thumbstick Click")]
    public OVRInput.Button dodgeButton = OVRInput.Button.PrimaryThumbstick;

    void Update()
    {
        HandleQuest3Movement();
        HandleQuest3Combat();
    }

    void HandleQuest3Movement()
    {
        // Get thumbstick input from left controller
        Vector2 leftThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        if (leftThumbstick.magnitude > 0.1f)
        {
            // Convert to world space movement based on head direction
            Transform head = Camera.main.transform;
            Vector3 movement = (head.forward * leftThumbstick.y + head.right * leftThumbstick.x);
            movement.y = 0f;
            movement = movement.normalized * leftThumbstick.magnitude;

            // Rotate character to movement direction
            if (movement != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }

            // Move character
            characterController.Move(movement * movementSpeed * Time.deltaTime);
            animator.SetBool("Walking", true);
        }
        else
        {
            animator.SetBool("Walking", false);
        }
    }

    void HandleQuest3Combat()
    {
        // Dodge - Left thumbstick click
        if (OVRInput.GetDown(dodgeButton, OVRInput.Controller.LTouch))
            PerformDodge();

        // Attacks - Left controller X/Y, Right controller A/B
        if (OVRInput.GetDown(attack1Button, OVRInput.Controller.LTouch)) PerformAttack(0);
        if (OVRInput.GetDown(attack2Button, OVRInput.Controller.LTouch)) PerformAttack(1);
        if (OVRInput.GetDown(attack3Button, OVRInput.Controller.RTouch)) PerformAttack(2);
        if (OVRInput.GetDown(attack4Button, OVRInput.Controller.RTouch)) PerformAttack(3);
    }

    void PerformAttack(int attackIndex)
    {
        if (Time.time - lastAttackTime > attackCooldown)
        {
            animator.Play(attackAnimations[attackIndex]);
            lastAttackTime = Time.time;

            // Haptic feedback on correct controller
            OVRInput.SetControllerVibration(0.3f, 0.1f,
                attackIndex < 2 ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch);

            // Damage logic
            foreach (Transform opponent in opponents)
            {
                if (Vector3.Distance(transform.position, opponent.position) <= attackRadius)
                {
                    StartCoroutine(opponent.GetComponent<opponentAI>().PlayHitDamageAnimation(attackDamage));
                }
            }
        }
    }

    void PerformDodge()
    {
        animator.Play("DodgeFrontAnimation");
        Vector3 dodgeDirection = transform.forward * dodgeDistance;
        characterController.Move(dodgeDirection);

        // Haptic feedback for dodge
        OVRInput.SetControllerVibration(0.2f, 0.05f, OVRInput.Controller.LTouch);
    }

    // Your existing methods...
    public System.Collections.IEnumerator PlayHitDamageAnimation(int takeDamage)
    {
        yield return new WaitForSeconds(0.5f);
        // Your existing damage logic
    }

    // Effect triggers
    public void Attack1Effect() { /* Your effect logic */ }
    public void Attack2Effect() { /* Your effect logic */ }
    public void Attack3Effect() { /* Your effect logic */ }
    public void Attack4Effect() { /* Your effect logic */ }
}