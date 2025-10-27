using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class NewBehaviourScript : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float acceleration = 10f;
    public float deceleration = 10f;

    [Header("References")]
    [SerializeField] private Transform spriteHolder;

    [Header("Attack Settings")]
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private float attackCooldown = 0.55f;
    [SerializeField, Range(0.05f, 0.95f)]
    private float hitMoment = 0.35f;
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteHolder == null)
        {
            Debug.LogError("SpriteHolder не назначен в инспекторе!");
            enabled = false;
            return;
        }

        animator = spriteHolder.GetComponent<Animator>();
        spriteRenderer = spriteHolder.GetComponent<SpriteRenderer>();

        if (animator == null) Debug.LogError("На SpriteHolder нет Animator!");
        if (spriteRenderer == null) Debug.LogError("На SpriteHolder нет SpriteRenderer!");

        inputActions = new PlayerInputActions();
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
        if (isAttacking) moveInput = Vector2.zero;


        animator?.SetFloat("MoveX", moveInput.x);
        animator?.SetFloat("MoveY", moveInput.y);
        animator?.SetFloat("Speed", moveInput.sqrMagnitude);

        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = moveInput.normalized;
            animator?.SetFloat("LastMoveX", lastMoveDirection.x);
            animator?.SetFloat("LastMoveY", lastMoveDirection.y);
        }

        HandleSpriteFlip();
        HandleAttackInput();
    }

    private void FixedUpdate()
    {

        if (isAttacking)
        {
            rb.velocity = Vector2.zero;
            currentVelocity = Vector2.zero;
            return;
        }

        Vector2 targetVelocity = moveInput.normalized * moveSpeed;
        float lerpSpeed = (moveInput.magnitude > 0f) ? acceleration : deceleration;
        currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, Time.fixedDeltaTime * lerpSpeed);
        rb.velocity = currentVelocity;
    }

    private void HandleSpriteFlip()
    {
        if (spriteRenderer == null) return;

        float x = Mathf.Abs(moveInput.x) > 0.01f ? moveInput.x : lastMoveDirection.x;
        if (x < -0.01f) spriteRenderer.flipX = true;
        else if (x > 0.01f) spriteRenderer.flipX = false;
    }

    private void HandleAttackInput()
    {
        if (isAttacking) return;

        if (inputActions.Player.Attack.triggered && Time.time >= nextAttackTime)
        {
            isAttacking = true;
            StartCoroutine(PerformAttack());
        }
    }

    private IEnumerator PerformAttack()
    {
        animator?.SetBool("IsAttacking", true);

        float hitTime = Mathf.Clamp01(hitMoment) * attackDuration;
        yield return new WaitForSeconds(hitTime);

        DoHitDetection();

        float remaining = Mathf.Max(0f, attackDuration - hitTime);
        yield return new WaitForSeconds(remaining);

        animator?.SetBool("IsAttacking", false);

        isAttacking = false;
        nextAttackTime = Time.time + attackCooldown;
    }

    private void DoHitDetection()
    {
        Vector2 center = (Vector2)transform.position + lastMoveDirection.normalized * (attackReach * 0.6f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, attackReach, hittableLayers);

        foreach (var h in hits)
        {
            var dmg = h.GetComponent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(1);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Vector2 dir = Application.isPlaying ? lastMoveDirection.normalized : Vector2.right;
        Vector2 center = (Vector2)transform.position + dir * (attackReach * 0.6f);
        Gizmos.DrawWireSphere(center, attackReach);
    }
}

public interface IDamageable
{
    void TakeDamage(int amount);
}
