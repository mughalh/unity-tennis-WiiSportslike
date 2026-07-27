using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBatController : MonoBehaviour
{
    [Header("References")]
    public Transform ball;
    public Transform aimTarget;
    public Transform batModel; // The actual bat mesh/visual that swings

    [Header("Movement")]
    public float moveSpeed = 8f;
    public float maxHitDistance = 3.5f;
    public float prepareDistance = 5f; // Distance at which enemy prepares swing

    [Header("Swing Settings")]
    public float swingSpeed = 20f;
    public float backswingAngle = 45f;
    public float followThroughAngle = 120f;

    [Header("Hit Settings")]
    public float hitPower = 15f;
    public float upForce = 2f;
    public float hitCooldown = 0.5f;

    [Header("AI Behavior")]
    [Range(0f, 1f)]
    public float accuracy = 0.8f;
    public float aimVariation = 3f;

    private Rigidbody ballRb;
    private BallController ballController;
    private Vector3 fixedPosition;
    private Vector3 aimTargetInitialPosition;
    private float lastHitTime = 0f;
    private int hitCount = 0;

    // Swing states
    private enum SwingState { Idle, Backswing, ForwardSwing, FollowThrough, Recovery }
    private SwingState swingState = SwingState.Idle;
    private float swingProgress = 0f;
    private Quaternion batRestRotation;

    void Start()
    {
        fixedPosition = transform.position;

        if (batModel != null)
            batRestRotation = batModel.localRotation;
        else
            batModel = transform; // Use self if no bat model assigned

        if (aimTarget != null)
            aimTargetInitialPosition = aimTarget.position;

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

        if (aimTarget == null)
        {
            GameObject target = new GameObject("EnemyAimTarget");
            target.transform.position = new Vector3(0f, 0.5f, -15f);
            aimTarget = target.transform;
            aimTargetInitialPosition = aimTarget.position;
        }

        Debug.Log($"🤖 Enemy bat ready at {fixedPosition}");
    }

    void Update()
    {
        if (ball == null) return;

        if (ballController != null && ballController.lastHitter == "Enemy")
        {
            ResetAimTarget();
            return;
        }

        // Move towards the ball on X axis
        Vector3 targetPos = new Vector3(ball.position.x, fixedPosition.y, fixedPosition.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        UpdateAimTarget();

        // Check distance and start swing
        float distanceToBall = Vector3.Distance(transform.position, ball.position);

        if (distanceToBall < prepareDistance && swingState == SwingState.Idle &&
            Time.time > lastHitTime + hitCooldown)
        {
            StartSwing();
        }

        UpdateSwing();
    }

    void StartSwing()
    {
        swingState = SwingState.Backswing;
        swingProgress = 0f;
    }

    void UpdateSwing()
    {
        if (swingState == SwingState.Idle) return;

        swingProgress += Time.deltaTime * swingSpeed;

        switch (swingState)
        {
            case SwingState.Backswing:
                // Rotate bat backwards
                float backswing = Mathf.Lerp(0f, -backswingAngle, swingProgress);
                batModel.localRotation = batRestRotation * Quaternion.Euler(backswing, 0f, 0f);

                if (swingProgress >= 1f)
                {
                    swingState = SwingState.ForwardSwing;
                    swingProgress = 0f;

                    // Check if ball is in range to hit
                    float distance = Vector3.Distance(transform.position, ball.position);
                    if (distance < maxHitDistance && Time.time > lastHitTime + hitCooldown)
                    {
                        ExecuteHit();
                    }
                }
                break;

            case SwingState.ForwardSwing:
                // Swing forward through the ball
                float forwardSwing = Mathf.Lerp(-backswingAngle, followThroughAngle, swingProgress);
                batModel.localRotation = batRestRotation * Quaternion.Euler(forwardSwing, 0f, 0f);

                if (swingProgress >= 1f)
                {
                    swingState = SwingState.Recovery;
                    swingProgress = 0f;
                }
                break;

            case SwingState.Recovery:
                // Return to rest position
                float recovery = Mathf.Lerp(followThroughAngle, 0f, swingProgress);
                batModel.localRotation = batRestRotation * Quaternion.Euler(recovery, 0f, 0f);

                if (swingProgress >= 1f)
                {
                    swingState = SwingState.Idle;
                    batModel.localRotation = batRestRotation;
                }
                break;
        }
    }

    void ExecuteHit()
    {
        if (ballRb == null) return;

        // Calculate hit direction
        Vector3 hitDirection;
        if (aimTarget != null)
        {
            hitDirection = (aimTarget.position - transform.position).normalized;
        }
        else
        {
            hitDirection = (Vector3.forward * -1 + Vector3.up * 0.3f +
                          new Vector3(Random.Range(-0.3f, 0.3f), 0, 0)).normalized;
        }

        hitDirection.y += upForce / hitPower;
        hitDirection.Normalize();

        ballRb.linearVelocity = hitDirection * hitPower;
        ballRb.angularVelocity = Vector3.zero;

        if (ballController != null)
        {
            ballController.HitBall();
            ballController.lastHitter = "Enemy";
            ballController.hitterPlayer = false;
        }

        lastHitTime = Time.time;
        hitCount++;
        ResetAimTarget();

        Debug.Log($"🏏 Enemy swung and hit #{hitCount} with power {hitPower}");
    }

    void UpdateAimTarget()
    {
        if (aimTarget == null) return;

        if (Random.value < 0.02f)
        {
            Vector3 randomAim = aimTargetInitialPosition;

            if (Random.value < accuracy)
            {
                randomAim.x += Random.Range(-aimVariation, aimVariation);
                randomAim.z += Random.Range(-aimVariation, aimVariation);
            }
            else
            {
                randomAim.x += Random.Range(-aimVariation * 3, aimVariation * 3);
                randomAim.z += Random.Range(-aimVariation * 3, aimVariation * 3);
            }

            aimTarget.position = randomAim;
        }
    }

    void ResetAimTarget()
    {
        if (aimTarget != null)
            aimTarget.position = aimTargetInitialPosition;
    }

    // Keep trigger for backup hit detection
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball")) return;
        if (Time.time < lastHitTime + hitCooldown) return;
        if (ballController != null && ballController.lastHitter == "Enemy") return;

        if (swingState == SwingState.Idle)
        {
            StartSwing();
        }
    }

    public void ResetBat()
    {
        transform.position = fixedPosition;
        swingState = SwingState.Idle;
        if (batModel != null)
            batModel.localRotation = batRestRotation;
        ResetAimTarget();
    }
}