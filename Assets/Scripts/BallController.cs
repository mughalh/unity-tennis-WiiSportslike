using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Physics")]
    public float maxSpeed = 25f;
    public float bounceDamping = 0.5f;
    public float mass = 1.5f;

    [Header("Start Position")]
    public Vector3 startPosition = new Vector3(0f, 0.5f, 0f);

    [Header("Trail")]
    public float trailTime = 0.8f;
    public float minTrailSpeed = 1.5f;
    public Color trailColor = Color.cyan;

    public bool playing = true;
    public bool hitterPlayer = false;
    public string lastHitter = "";

    private Rigidbody rb;
    private TrailRenderer trail;
    private bool hasBounced = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = mass;
        rb.linearDamping = 0.3f;
        rb.angularDamping = 0.5f;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        trail = GetComponent<TrailRenderer>();

        if (trail == null)
            trail = gameObject.AddComponent<TrailRenderer>();

        trail.time = trailTime;
        trail.startWidth = 0.12f;
        trail.endWidth = 0.02f;
        trail.material = new Material(Shader.Find("Sprites/Default"));
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        trail.enabled = false;

        ResetBall();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBall();

            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
                gm.Serve();

            return;
        }

        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;

        if (trail != null)
            trail.enabled = playing && rb.linearVelocity.magnitude > minTrailSpeed;
    }

    public void LaunchBall(Vector3 velocity, string hitter)
    {
        rb.linearVelocity = velocity;
        rb.angularVelocity = Vector3.zero;

        playing = true;
        lastHitter = hitter;
        hitterPlayer = hitter == "Player";
        hasBounced = false;

        if (trail != null)
            trail.Clear();
    }

    public void HitBall()
    {
        playing = true;

        if (trail != null)
            trail.Clear();
    }

    public void HitBall(Vector3 velocity, string hitter)
    {
        LaunchBall(velocity, hitter);
    }

    public void ResetBall()
    {
        transform.position = startPosition;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        playing = true;
        hitterPlayer = false;
        lastHitter = "";
        hasBounced = false;

        if (trail != null)
            trail.Clear();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!playing)
            return;

        if (collision.transform.CompareTag("Ground"))
        {
            Vector3 normal = collision.contacts[0].normal;
            rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, normal) * bounceDamping;
            hasBounced = true;
        }

        if (collision.transform.CompareTag("Wall"))
        {
            if (!hasBounced)
            {
                GameManager gm = FindAnyObjectByType<GameManager>();

                if (gm != null)
                {
                    if (lastHitter == "Player")
                        gm.AIScored();
                    else if (lastHitter == "AI")
                        gm.PlayerScored();
                }

                playing = false;
            }
            else
            {
                Vector3 normal = collision.contacts[0].normal;
                rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, normal) * 0.7f;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!playing)
            return;

        if (other.CompareTag("Out"))
        {
            GameManager gm = FindAnyObjectByType<GameManager>();

            if (gm != null)
            {
                if (lastHitter == "Player")
                    gm.AIScored();
                else if (lastHitter == "AI")
                    gm.PlayerScored();
            }

            playing = false;
        }
    }

    void OnGUI()
    {
        GUI.Box(new Rect(10, Screen.height - 50, 180, 40), "Press R to Reset");
    }
}