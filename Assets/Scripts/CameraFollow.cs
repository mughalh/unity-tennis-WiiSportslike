using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform player;
    public Transform ball;

    [Header("Zoom Settings")]
    [Tooltip("1 = normal, 0.5 = 2x zoom, 0.33 = 3x zoom")]
    public float zoomMultiplier = 0.5f;

    [Header("Camera Position")]
    public Vector3 baseOffset = new Vector3(19.5f, 15f, -1.62f);
    public float smoothSpeed = 0.125f;
    public float lookAhead = 2f;

    private Vector3 velocity = Vector3.zero;
    private float fixedXPosition;

    void Start()
    {
        fixedXPosition = transform.position.x;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        if (ball == null)
        {
            GameObject ballObj = GameObject.Find("tennis_ball");
            if (ballObj != null)
                ball = ballObj.transform;
        }
    }

    void LateUpdate()
    {
        Transform target = player != null ? player : ball;
        if (target == null) return;

        Vector3 lookAheadOffset = Vector3.zero;
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb != null && rb.linearVelocity.magnitude > 0.5f)
        {
            lookAheadOffset = rb.linearVelocity.normalized * lookAhead;
        }

        Vector3 targetPosition = target.position + lookAheadOffset + (baseOffset * zoomMultiplier);

        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothSpeed
        );
        smoothedPosition.x = fixedXPosition;

        transform.position = smoothedPosition;
        transform.LookAt(target.position + lookAheadOffset + Vector3.up * 1f);
    }
}