using UnityEngine;

/// <summary>
/// Top-down player controller with WASD movement, bobbing, and sprite flipping.
/// Attach to the root “Player” object (with a Rigidbody2D).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Transform body;              // Child sprite/container that bobs & flips

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;        // Units per second

    [Header("Bobbing")]
    [SerializeField] private AnimationCurve bobCurve =
        AnimationCurve.EaseInOut(0, 0, 0.5f, 1);          // Simple 0→1→0 curve
    [SerializeField] private float bobFrequency = 6f;     // Cycles per second
    [SerializeField] private float bobAmplitude = 0.08f;  // Vertical offset (units)

    // ─────────────────────────────────────────────────────────────────────────────

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector3 bodyStartLocalPos;
    private Vector3 bodyStartLocalScale;
    private float bobTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (body != null)
        {
            bodyStartLocalPos   = body.localPosition;
            bodyStartLocalScale = body.localScale;
        }
    }

    private void Update()
    {
        // 1. Gather WASD / arrow-key input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput   = moveInput.normalized;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;

        // 2. Bob animation while moving
        if (isMoving && body != null)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float curveValue      = bobCurve.Evaluate(Mathf.Repeat(bobTimer, 1f));
            Vector3 bobOffset     = Vector3.up * (curveValue * bobAmplitude);
            body.localPosition    = bodyStartLocalPos + bobOffset;
        }
        else if (body != null)
        {
            bobTimer             = 0f;
            body.localPosition   = bodyStartLocalPos;
        }

        // 3. Flip body scale based on horizontal direction
        if (isMoving && Mathf.Abs(moveInput.x) > 0.01f && body != null)
        {
            float directionSign  = Mathf.Sign(moveInput.x);              // +1 (right) or −1 (left)
            Vector3 newScale     = bodyStartLocalScale;
            newScale.x          *= directionSign;                        // Flip X if moving left
            body.localScale      = newScale;
        }
    }

    private void FixedUpdate()
    {
        // 4. Rigidbody movement (FixedUpdate for physics consistency)
        Vector2 newPos = rb.position + moveInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
    }
}
