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

    private int currentHealth;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();

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
        if (target == null) return;

        float distance = Vector2.Distance(transform.position, target.position);
        if (distance < detectionRange)
        {
            Vector2 direction = (target.position - transform.position).normalized;
            rb.velocity = direction * moveSpeed;

            animator.SetFloat("MoveX", direction.x);
            animator.SetFloat("MoveY", direction.y);
            animator.SetFloat("Speed", rb.velocity.sqrMagnitude);

            if (Mathf.Abs(direction.x) > 0.01f)
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
        currentHealth -= amount;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("Hit");
            StartCoroutine(FlashRed());
        }
    }

    private IEnumerator FlashRed()
    {
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = Color.white;
    }

    private void Die()
    {
        rb.velocity = Vector2.zero;
        rb.simulated = false;

        animator.SetTrigger("Die");
        Destroy(gameObject, 0.6f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
