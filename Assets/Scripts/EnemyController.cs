using UnityEngine;

/// <summary>
/// Wandering top-down enemy: picks a random point inside the main camera bounds,
/// walks there with a bob animation, waits, then picks a new point.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    // ─────────────────── Inspector ────────────────────────────────────────────
    [Header("Links")]
    [SerializeField] private Transform body;                 // Child sprite that bobs & flips

    [Header("Movement")]
    [SerializeField] private float moveSpeed      = 3f;      // Units per second
    [SerializeField] private float reachThreshold = 0.05f;   // How close counts as “arrived”

    [Header("Idle Time After Arriving")]
    [SerializeField] private float pauseTimeMin   = 1f;      // Seconds
    [SerializeField] private float pauseTimeMax   = 3f;

    [Header("Bobbing (same as player)")]
    [SerializeField] private AnimationCurve bobCurve =
        AnimationCurve.EaseInOut(0f, 0f, 0.5f, 1f);          // 0→1→0 curve
    [SerializeField] private float bobFrequency   = 6f;      // Hz
    [SerializeField] private float bobAmplitude   = 0.08f;   // Units

    // ─────────────────── Internals ────────────────────────────────────────────
    private Rigidbody2D rb;
    private Vector2     targetPos;
    private float       waitTimer;                           // >0 while waiting
    private float       bobTimer;

    private Vector3 bodyStartLocalPos;
    private Vector3 bodyStartLocalScale;

    // ─────────────────── Unity Events ─────────────────────────────────────────
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (body != null)
        {
            bodyStartLocalPos   = body.localPosition;
            bodyStartLocalScale = body.localScale;
        }
    }

    private void Start()
    {
        ChooseNewTarget();       // Kick things off
    }

    private void Update()
    {
        bool isMoving = MoveTowardTarget();

        HandleBobbing(isMoving);
        HandleFlip(isMoving);

        if (isMoving)
        {
            // Reset idle timer while on the move
            waitTimer = 0f;
        }
        else
        {
            // We’re idle at the destination
            if (waitTimer <= 0f)
            {
                // Start a new idle countdown the first frame we arrive
                waitTimer = Random.Range(pauseTimeMin, pauseTimeMax);
            }
            else
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                    ChooseNewTarget();   // Pick the next wander point
            }
        }
    }

    // ─────────────────── Helper Methods ──────────────────────────────────────
    /// <summary>Select a random location within the camera frustum.</summary>
    private void ChooseNewTarget()
    {
        Vector3 camPos      = Camera.main.transform.position;
        float   zDist       = Mathf.Abs(camPos.z - transform.position.z); // Depth difference

        Vector3 bottomLeft  = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, zDist));
        Vector3 topRight    = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, zDist));

        targetPos = new Vector2(
            Random.Range(bottomLeft.x, topRight.x),
            Random.Range(bottomLeft.y, topRight.y)
        );
    }

    /// <summary>
    /// Moves toward the current target; returns true while still travelling.
    /// </summary>
    private bool MoveTowardTarget()
    {
        Vector2 current = rb.position;
        Vector2 delta   = targetPos - current;

        // Check arrival threshold
        if (delta.sqrMagnitude < reachThreshold * reachThreshold)
            return false;    // Arrived

        // Normalised direction and move step
        Vector2 dir = delta.normalized;
        rb.MovePosition(current + dir * moveSpeed * Time.deltaTime);
        return true;
    }

    private void HandleBobbing(bool isMoving)
    {
        if (body == null) return;

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float curveVal         = bobCurve.Evaluate(Mathf.Repeat(bobTimer, 1f));
            Vector3 offset         = Vector3.up * (curveVal * bobAmplitude);
            body.localPosition     = bodyStartLocalPos + offset;
        }
        else
        {
            bobTimer               = 0f;
            body.localPosition     = bodyStartLocalPos;
        }
    }

    private void HandleFlip(bool isMoving)
    {
        if (!isMoving || body == null) return;

        float xDir = Mathf.Sign(targetPos.x - rb.position.x);
        if (Mathf.Abs(xDir) < 0.01f) return; // Ignore near-vertical moves

        Vector3 scale = bodyStartLocalScale;
        scale.x       = Mathf.Abs(scale.x) * xDir;           // + right, − left
        body.localScale = scale;
    }
}
