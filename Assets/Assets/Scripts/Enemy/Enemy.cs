using UnityEngine;
using System.Collections;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("References")]
    [SerializeField] private Transform spriteHolder;

    [Header("Stats")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private Transform target;

    [Header("Death Settings")]
    [SerializeField] private float deathDelay = 2f; // задержка перед уничтожением

    private int currentHealth;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    private Vector2 smoothDirection;
    private bool isDead;
    private bool isHit;

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
        if (isDead || isHit || target == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance < detectionRange)
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
            rb.velocity = Vector2.zero;
            animator.SetFloat("Speed", 0);
        }
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

        // Включаем анимацию удара
        animator.SetTrigger("Hit");

        // ждём, пока анимация проиграется (подгони под длину твоей анимации)
        yield return new WaitForSeconds(0.4f);

        isHit = false;
    }

    private IEnumerator DieRoutine()
    {
        isDead = true;
        rb.velocity = Vector2.zero;
        rb.simulated = false;
        if (col) col.enabled = false;

        // Запускаем анимацию смерти
        animator.SetTrigger("Die");

        // ждём, пока враг "лежит мёртвым"
        yield return new WaitForSeconds(deathDelay);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
