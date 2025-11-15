using UnityEngine;
using System.Collections;

public class opponentAI : MonoBehaviour
{
    [Header("Player Movement")]
    public float movementSpeed = 1f;
    public float rotationSpeed = 10f;
    public CharacterController characterController;
    public Animator animator;

    [Header("Opponent Movement")]
    public float attackCooldown = 0.5f;
    public int attackDamage = 5;
    public string[] attackAnimations = { "Attack1Animation", "Attack2Animation", "Attack3Animation", "Attack4Animation" };
    public float dodgeDistance = 2f;
    public int attackCount = 0;
    public int randomNumber;
    public float attackRadius = 2f;
    public MonoBehaviour[] fightingController;
    public Transform[] players;
    public bool isTakingDamage;
    private float lastAttackTime;

    [Header("Effects and Sounds")]
    public ParticleSystem attack1Effect;
    public ParticleSystem attack2Effect;
    public ParticleSystem attack3Effect;
    public ParticleSystem attack4Effect;

    public AudioClip[] hitSounds;

    [Header("Health")]
    public int maxHealth = 180;
    public int currentHealth;
    public HealthBar healthBar;

    void Awake()
    {
        /*******************************************************/
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        /*******************************************************/
        currentHealth = maxHealth;
        
        if (healthBar != null)
        {
            healthBar.GiveFullHealth(currentHealth);
        }
        
        createRandomNumber();
    }

    void Update()
    {
        for (int i = 0; i < fightingController.Length; i++)
        {
            if (players[i].gameObject.activeSelf && Vector3.Distance(transform.position, players[i].position) <= attackRadius)
            {
                animator.SetBool("Walking", false);
                if (Time.time - lastAttackTime > attackCooldown)
                {
                    int randomAttackIndex = Random.Range(0, attackAnimations.Length);
                    if (!isTakingDamage)
                    {
                        PerformAttack(randomAttackIndex);
                        StartCoroutine(((FightingCharacter)fightingController[i]).PlayHitDamageAnimation(attackDamage));
                    }
                }
            }
            else
            {
                if (players[i].gameObject.activeSelf)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, players[i].position);

                    if (distanceToPlayer <= attackRadius)
                    {
                        if (Time.time >= lastAttackTime + attackCooldown)
                        {
                            int randomAttack = Random.Range(0, attackAnimations.Length);
                            PerformAttack(randomAttack);
                        }
                        animator.SetBool("Walking", false);
                    }
                    else
                    {
                        Vector3 direction = (players[i].position - transform.position).normalized;
                        characterController.Move(direction * movementSpeed * Time.deltaTime);
                        Quaternion targetRotation = Quaternion.LookRotation(direction);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                        animator.SetBool("Walking", true);
                    }
                }
            }
        }
    }

    void PerformAttack(int attackIndex)
    {
        animator.Play(attackAnimations[attackIndex]);
        Debug.Log($"Performed attack {attackIndex + 1}, dealing {attackDamage} damage");
        lastAttackTime = Time.time;
    }

    void PerformDodgeFront()
    {
        animator.Play("DodgeFrontAnimation");
        Vector3 dodgeDirection = -transform.forward * dodgeDistance;
        characterController.SimpleMove(dodgeDirection);
    }

    void createRandomNumber()
    {
        randomNumber = Random.Range(1, 5);
    }

    public IEnumerator PlayHitDamageAnimation(int takeDamage)
    {
        yield return new WaitForSeconds(0.5f);
        
        // Play random hurt sounds
        if (hitSounds != null && hitSounds.Length > 0)
        {
            int randomIndex = Random.Range(0, hitSounds.Length);
            AudioSource.PlayClipAtPoint(hitSounds[randomIndex], transform.position);
        }
        
        // Decrease health
        currentHealth -= takeDamage;
        
        // Update health bar - FIXED: using healthBar instance instead of HealthBar class
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
        Debug.Log("Opponent died.");
        // Add death logic here
    }

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