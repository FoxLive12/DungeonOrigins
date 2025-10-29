using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NewBehaviourScript : MonoBehaviour, IDamageable
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float acceleration = 10f;
    public float deceleration = 10f;

    [Header("References")]
    [SerializeField] private Transform spriteHolder;

    [Header("Health Settings")]
    public int maxHealth = 5;
    private int currentHealth;
    private bool isInvincible;
    private bool Die;

    [SerializeField] private float invincibleDuration = 1f;
    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDecay = 8f; // чем больше — тем быстрее гаснет отбрасывание

    [Header("Attack Settings")]
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private float attackCooldown = 0.55f;
    [SerializeField, Range(0.05f, 0.95f)] private float hitMoment = 0.35f;
    [SerializeField] private float attackReach = 0.75f;
    [SerializeField] private LayerMask hittableLayers;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerInputActions inputActions;

    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private Vector2 lastMoveDirection = Vector2.down;

    private bool isAttacking;
    private float nextAttackTime;
    private bool isKnockbacked;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerInputActions();

        if (!spriteHolder)
        {
            Debug.LogError("SpriteHolder не назначен в инспекторе!");
            enabled = false;
            return;
        }

        animator = spriteHolder.GetComponent<Animator>();
        spriteRenderer = spriteHolder.GetComponent<SpriteRenderer>();

        if (!animator) Debug.LogError("На SpriteHolder нет Animator!");
        if (!spriteRenderer) Debug.LogError("На SpriteHolder нет SpriteRenderer!");
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Start()
    {
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (Die) return;

        ReadInput();
        UpdateAnimator();
        HandleSpriteFlip();
        HandleAttackInput();
    }

    private void FixedUpdate()
    {
        if (Die) return;

        if (isKnockbacked) // если идёт отбрасывание
        {
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.fixedDeltaTime * knockbackDecay);
            return;
        }

        if (isAttacking)
        {
            rb.velocity = Vector2.zero;
            currentVelocity = Vector2.zero;
            return;
        }

        Vector2 targetVelocity = moveInput.normalized * moveSpeed;
        float lerpSpeed = moveInput.magnitude > 0 ? acceleration : deceleration;
        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * lerpSpeed);
        rb.velocity = currentVelocity;
    }

    private void ReadInput()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        if (isAttacking)
            moveInput = Vector2.zero;
    }

    private void UpdateAnimator()
    {
        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
        animator.SetFloat("Speed", moveInput.sqrMagnitude);

        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = moveInput.normalized;
            animator.SetFloat("LastMoveX", lastMoveDirection.x);
            animator.SetFloat("LastMoveY", lastMoveDirection.y);
        }
    }

    private void HandleSpriteFlip()
    {
        if (!spriteRenderer) return;
        if (Mathf.Abs(lastMoveDirection.x) > 0.01f)
            spriteRenderer.flipX = lastMoveDirection.x < 0f;
    }

    private void HandleAttackInput()
    {
        if (isAttacking || isKnockbacked) return;

        if (inputActions.Player.Attack.triggered && Time.time >= nextAttackTime)
        {
            isAttacking = true;
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        animator.SetTrigger("Attack");
        isAttacking = true;

        yield return null;

        float hitTime = Mathf.Clamp01(hitMoment) * attackDuration;
        if (hitTime > 0f)
            yield return new WaitForSeconds(hitTime);

        DoHitDetection();

        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    private void DoHitDetection()
    {
        Vector2 center = (Vector2)transform.position + lastMoveDirection.normalized * (attackReach * 0.6f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackReach, hittableLayers);

        foreach (var h in hits)
            h.GetComponent<IDamageable>()?.TakeDamage(1);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Vector2 dir = Application.isPlaying ? lastMoveDirection.normalized : Vector2.right;
        Vector2 center = (Vector2)transform.position + dir * (attackReach * 0.6f);
        Gizmos.DrawWireSphere(center, attackReach);
    }

    // ==========================
    // === ПОЛУЧЕНИЕ УРОНА ===
    // ==========================

    public void TakeDamage(int amount)
    {
        if (Die || isInvincible) return;

        currentHealth -= amount;
        Debug.Log($"Игрок получил {amount} урона! Осталось HP: {currentHealth}");

        if (currentHealth > 0)
            StartCoroutine(DamageFeedback());
        else
            StartCoroutine(DieRoutine());
    }

    private IEnumerator DamageFeedback()
    {
        isInvincible = true;
        isKnockbacked = true;

        // 🔸 Отталкивание
        Vector2 knockDir = (Vector2)transform.position - GetNearestEnemyPosition();
        knockDir.Normalize();
        rb.velocity = knockDir * knockbackForce;

        // 🔸 Мигание (эффект урона)
        for (int i = 0; i < 5; i++)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(invincibleDuration - 0.5f);

        isInvincible = false;
        isKnockbacked = false;
    }

    private Vector2 GetNearestEnemyPosition()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0)
            return (Vector2)transform.position - lastMoveDirection;

        GameObject closest = enemies[0];
        float closestDist = Vector2.Distance(transform.position, closest.transform.position);

        foreach (var e in enemies)
        {
            float d = Vector2.Distance(transform.position, e.transform.position);
            if (d < closestDist)
            {
                closestDist = d;
                closest = e;
            }
        }

        return closest.transform.position;
    }

    private IEnumerator DieRoutine()
    {
        Die = true;
        rb.velocity = Vector2.zero;
        animator.SetTrigger("Die");
        Debug.Log("Игрок погиб!");

        yield return new WaitForSeconds(1.2f);
        // TODO: добавить перезапуск сцены или GameOver экран
    }
}

// === Интерфейс ===
public interface IDamageable
{
    void TakeDamage(int amount);
}
