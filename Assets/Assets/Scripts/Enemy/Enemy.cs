using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private Transform spriteHolder;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRange = 3f;
    [SerializeField] private Transform target;

    [Header("Attack Settings")]
    [SerializeField] private int contactDamage = 1;          // урон при атаке
    [SerializeField] private float attackRange = 1.0f;       // расстояние для начала атаки
    [SerializeField] private float attackWindup = 0.4f;      // пауза перед ударом (замах)
    [SerializeField] private float attackRecovery = 0.6f;    // пауза после удара
    [SerializeField] private float attackCooldown = 1.2f;    // общее время между атаками

    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 2f;

    private int currentHealth;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private Vector2 smoothDirection;
    private bool isDead;
    private bool isHit;
    private bool isAttacking;
    private float nextAttackTime;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (!spriteHolder)
        {
            Debug.LogError("SpriteHolder не назначен!", this);
            enabled = false;
            return;
        }

        animator = spriteHolder.GetComponent<Animator>();
        spriteRenderer = spriteHolder.GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (isDead || isHit || isAttacking || target == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);

        if (distance < detectionRange)
        {
            // если игрок близко — двигаемся к нему
            if (distance > attackRange)
            {
                Vector2 direction = (target.position - transform.position).normalized;
                rb.velocity = direction * moveSpeed;

                smoothDirection = Vector2.Lerp(smoothDirection, direction, 0.15f);
                animator.SetFloat("MoveX", smoothDirection.x);
                animator.SetFloat("MoveY", smoothDirection.y);
                animator.SetFloat("Speed", rb.velocity.magnitude);

                if (Mathf.Abs(direction.x) > 0.05f)
                    spriteRenderer.flipX = direction.x < 0f;
            }
            else
            {
                // если достаточно близко — готовимся к атаке
                rb.velocity = Vector2.zero;
                animator.SetFloat("Speed", 0);

                if (Time.time >= nextAttackTime)
                    StartCoroutine(AttackRoutine());
            }
        }
        else
        {
            rb.velocity = Vector2.zero;
            animator.SetFloat("Speed", 0);
        }
    }

    // === АТАКА ===
    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        rb.velocity = Vector2.zero;

        // Пауза перед ударом (замах)
        yield return new WaitForSeconds(attackWindup);

        // Проверяем, жив ли игрок и в радиусе
        if (target != null && !isDead)
        {
            float dist = Vector2.Distance(transform.position, target.position);
            if (dist <= attackRange + 0.3f) // небольшая погрешность
            {
                // атака — наносим урон, если игрок реализует IDamageable
                var damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(contactDamage);
            }
        }

        // Пауза после удара
        yield return new WaitForSeconds(attackRecovery);

        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    public void TakeDamage(int amount)
    {
        if (isDead || isHit) return;

        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            StartCoroutine(DieRoutine());
        }
        else
        {
            StartCoroutine(HitRoutine());
        }
    }

    private IEnumerator HitRoutine()
    {
        isHit = true;
        rb.velocity = Vector2.zero;
        animator.SetTrigger("Hit");

        yield return new WaitForSeconds(0.4f);

        isHit = false;
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        rb.velocity = Vector2.zero;
        rb.simulated = false;
        if (col) col.enabled = false;

        animator.SetTrigger("Die");

        yield return new WaitForSeconds(deathDelay);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        // радиус обнаружения и атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
