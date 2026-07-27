using UnityEngine;

public class BatController : MonoBehaviour
{
    [Header("References")]
    public SensorNetworkReceiver sensorReceiver;
    public Transform ball;

    [Header("Motion Mapping")]
    public float rotationSmoothing = 0.15f;
    public float powerMultiplier = 2.5f;
    public float minPower = 5f;
    public float maxPower = 30f;

    [Header("Axis Calibration")]
    [Tooltip("Invert X axis (pitch)")]
    public bool invertX = true;

    [Tooltip("Invert Y axis (yaw)")]
    public bool invertY = false;

    [Tooltip("Invert Z axis (roll)")]
    public bool invertZ = true;

    [Tooltip("Swap X and Y axes")]
    public bool swapXY = false;

    [Header("Initial Rotation Offset")]
    public float initialYawOffset = -90f;

    [Header("Swing Detection")]
    public float swingThreshold = 2.5f;
    public float swingWindow = 0.25f;
    public float hitCooldown = 0.4f;
    public float maxHitDistance = 3.5f;

    [Header("Hit Settings")]
    public float liftFactor = 0.4f;

    [Header("Aim Target")]
    public Vector3 opponentBaselineCenter = new Vector3(0f, 1f, 15f);  // <--- THIS IS NEW

    // Private state
    private Quaternion currentOrient;
    private Vector3 currentAccel;
    private bool hasIMUData = false;

    private float swingPeak = 0f;
    private bool isSwinging = false;
    private float swingTimer = 0f;
    private float lastHitTime = 0f;
    private int hitCount = 0;

    private Rigidbody ballRb;
    private BallController ballController;
    private Vector3 fixedPosition;
    private Quaternion initialOffset;

    void Start()
    {
        fixedPosition = transform.position;
        initialOffset = Quaternion.Euler(0f, initialYawOffset, 0f);

        if (sensorReceiver == null)
            sensorReceiver = FindAnyObjectByType<SensorNetworkReceiver>();

        if (ball == null)
        {
            GameObject ballObj = GameObject.FindGameObjectWithTag("Ball");
            if (ballObj != null)
            {
                ball = ballObj.transform;
                ballRb = ballObj.GetComponent<Rigidbody>();
                ballController = ballObj.GetComponent<BallController>();
            }
        }

        transform.rotation = initialOffset;
        Debug.Log($"🏏 Bat ready at {fixedPosition}");
    }

    void Update()
    {
        if (sensorReceiver != null && sensorReceiver.HasData())
        {
            currentOrient = sensorReceiver.GetLatestOrientation();
            currentAccel = sensorReceiver.GetLatestAccel();
            hasIMUData = true;
        }

        if (!hasIMUData) return;

        transform.position = fixedPosition;
        ApplyRotation();
        DetectSwing();
    }

    void ApplyRotation()
    {
        Vector3 euler = currentOrient.eulerAngles;

        float x = euler.x;
        float y = euler.y;
        float z = euler.z;

        if (swapXY)
        {
            float temp = x;
            x = y;
            y = temp;
        }

        if (invertX) x = -x;
        if (invertY) y = -y;
        if (invertZ) z = -z;

        Quaternion calibratedRotation = Quaternion.Euler(x, y, z);
        Quaternion finalRotation = initialOffset * calibratedRotation;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            finalRotation,
            1f - rotationSmoothing
        );
    }

    void DetectSwing()
    {
        float accelMag = currentAccel.magnitude;

        if (!isSwinging && accelMag > swingThreshold && Time.time > lastHitTime + hitCooldown)
        {
            isSwinging = true;
            swingPeak = 0f;
            swingTimer = 0f;
        }

        if (isSwinging)
        {
            swingTimer += Time.deltaTime;
            if (accelMag > swingPeak)
                swingPeak = accelMag;

            if (swingTimer > swingWindow)
            {
                isSwinging = false;
                if (swingPeak > swingThreshold)
                    ExecuteHit();
            }
        }
    }

    void ExecuteHit()
    {
        if (ball == null || ballRb == null || ballController == null)
            return;

        float distance = Vector3.Distance(transform.position, ball.position);
        if (distance > maxHitDistance) return;

        // Use bat's yaw to steer left/right
        float batYaw = transform.eulerAngles.y;
        float maxOffset = 8f;
        float sensitivity = 0.5f;

        float normalizedYaw = (batYaw > 180) ? batYaw - 360 : batYaw;
        float horizontalOffset = Mathf.Clamp(normalizedYaw * sensitivity, -maxOffset, maxOffset);

        // Target point on opponent's baseline
        Vector3 targetPoint = opponentBaselineCenter;   // Now this works!
        targetPoint.x += horizontalOffset;

        // Calculate hit direction toward that target
        Vector3 hitDir = (targetPoint - transform.position).normalized;
        hitDir.y += liftFactor;
        hitDir.Normalize();

        // Power based on swing strength
        float power = Mathf.Lerp(minPower, maxPower,
                                 Mathf.InverseLerp(swingThreshold, 10f, swingPeak)) * powerMultiplier;

        ballRb.linearVelocity = hitDir * power + Vector3.up * 2f;
        ballRb.angularVelocity = Vector3.zero;

        ballController.HitBall();
        ballController.hitterPlayer = true;
        ballController.lastHitter = "Player";

        lastHitTime = Time.time;
        hitCount++;
    }

    public void ResetBat()
    {
        transform.rotation = initialOffset;
        transform.position = fixedPosition;
    }
}