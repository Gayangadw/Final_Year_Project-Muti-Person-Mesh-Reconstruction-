using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class FightingCharacter : MonoBehaviour
{
    [Header("Input System")]
    public PlayerInput playerInput;

    [Header("Player Movement")]
    public float movementSpeed = 1f;
    public float rotationSpeed = 10f;
    private CharacterController characterController;
    private Animator animator;

    [Header("Player Fight")]
    public float attackCooldown = 0.5f;
    public int attackDamage = 5;
    public string[] attackAnimations = { "Attack1Animation", "Attack2Animation", "Attack3Animation", "Attack4Animation" };
    public float dodgeDistance = 2f;
    public float attackRadius = 2.2f;
    public Transform[] opponents;
    private float lastAttackTime;

    [Header("Effects and Sounds")]
    public ParticleSystem attack1Effect;
    public ParticleSystem attack2Effect;
    public ParticleSystem attack3Effect;
    public ParticleSystem attack4Effect;
    private ParticleSystem[] attackEffects;
    public AudioClip[] hitSounds;

    [Header("Health")]
    public int maxHealth = 180;
    public int currentHealth;
    public HealthBar healthBar;

    private Vector2 movementInput;

    void Awake()
    {
        currentHealth = maxHealth;
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        attackEffects = new ParticleSystem[] { attack1Effect, attack2Effect, attack3Effect, attack4Effect };

        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (healthBar != null)
        {
            healthBar.GiveFullHealth(currentHealth);
        }
    }

    void Update()
    {
        PerformMovement();
        HandleKeyboardInput();
    }

    // INPUT SYSTEM METHODS - Use CallbackContext
    public void OnMove(InputAction.CallbackContext context)
    {
        movementInput = context.ReadValue<Vector2>();
    }

    public void OnDodge(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PerformDodgeFront();
        }
    }

    public void OnAttack1(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PerformAttack(0);
        }
    }

    public void OnAttack2(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PerformAttack(1);
        }
    }

    public void OnAttack3(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PerformAttack(2);
        }
    }

    public void OnAttack4(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PerformAttack(3);
        }
    }

    void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            PerformDodgeFront();
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) PerformAttack(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2)) PerformAttack(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3)) PerformAttack(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4)) PerformAttack(3);
    }

    void PerformMovement()
    {
        Vector3 movement;

        if (playerInput != null && movementInput.magnitude > 0.1f)
        {
            movement = new Vector3(movementInput.x, 0f, movementInput.y);
        }
        else
        {
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            movement = new Vector3(horizontalInput, 0f, verticalInput);
        }

        if (movement != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(movement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            animator.SetBool("Walking", true);
        }
        else
        {
            animator.SetBool("Walking", false);
        }

        characterController.Move(movement * movementSpeed * Time.deltaTime);
    }

    void PerformAttack(int attackIndex)
    {
        if (Time.time - lastAttackTime > attackCooldown)
        {
            animator.Play(attackAnimations[attackIndex]);
            Debug.Log($"Performed attack {attackIndex + 1}, dealing {attackDamage} damage");
            lastAttackTime = Time.time;

            foreach (Transform opponent in opponents)
            {
                if (Vector3.Distance(transform.position, opponent.position) <= attackRadius)
                {
                    StartCoroutine(opponent.GetComponent<opponentAI>().PlayHitDamageAnimation(attackDamage));
                }
            }
        }
        else
        {
            Debug.Log("Cannot perform attack yet. Cooldown time remaining.");
        }
    }

    void PerformDodgeFront()
    {
        animator.Play("DodgeFrontAnimation");
        Vector3 dodgeDirection = transform.forward * dodgeDistance;
        characterController.Move(dodgeDirection);
    }

    public IEnumerator PlayHitDamageAnimation(int takeDamage)
    {
        yield return new WaitForSeconds(0.5f);

        if (hitSounds != null && hitSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, hitSounds.Length);
            AudioSource.PlayClipAtPoint(hitSounds[randomIndex], transform.position);
        }

        currentHealth -= takeDamage;

        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }

        animator.Play("HitDamageAnimation");
    }

    void Die()
    {
        Debug.Log("Player died.");
    }

    // Effect methods
    public void Attack1Effect()
    {
        if (attack1Effect != null) attack1Effect.Play();
    }

    public void Attack2Effect()
    {
        if (attack2Effect != null) attack2Effect.Play();
    }

    public void Attack3Effect()
    {
        if (attack3Effect != null) attack3Effect.Play();
    }

    public void Attack4Effect()
    {
        if (attack4Effect != null) attack4Effect.Play();
    }
}