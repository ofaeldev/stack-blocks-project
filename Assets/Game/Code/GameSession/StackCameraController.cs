using UnityEngine;

public class StackCameraController : MonoBehaviour
{
    [SerializeField] private float heightBeforeMoving = 2f;
    [SerializeField] private float verticalFollowMultiplier = 0.75f;
    [SerializeField] private float positionSmoothTime = 0.25f;
    [SerializeField] private float lookAtSmoothSpeed = 6f;
    [SerializeField] private float shakeRecoverySpeed = 10f;

    private Vector3 startPosition;
    private Vector3 positionVelocity;
    private Vector3 shakeOffset;
    private float targetStackHeight;
    private float currentLookHeight;

    private void Awake()
    {
        startPosition = transform.position;
        targetStackHeight = heightBeforeMoving;
        currentLookHeight = heightBeforeMoving;
    }

    private void LateUpdate()
    {
        float heightAboveThreshold = Mathf.Max(0f, targetStackHeight - heightBeforeMoving);
        Vector3 desiredPosition = startPosition + Vector3.up * heightAboveThreshold * verticalFollowMultiplier;
        shakeOffset = Vector3.Lerp(shakeOffset, Vector3.zero, shakeRecoverySpeed * Time.deltaTime);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition + shakeOffset,
            ref positionVelocity,
            positionSmoothTime
        );

        currentLookHeight = Mathf.Lerp(
            currentLookHeight,
            Mathf.Max(heightBeforeMoving, targetStackHeight),
            lookAtSmoothSpeed * Time.deltaTime
        );

        transform.LookAt(new Vector3(0f, currentLookHeight, 0f));
    }

    public void SetTargetHeight(float stackHeight)
    {
        targetStackHeight = stackHeight;
    }

    public void AddImpact(float strength)
    {
        shakeOffset += Random.insideUnitSphere * strength;
    }

    public void ResetCamera()
    {
        targetStackHeight = heightBeforeMoving;
        currentLookHeight = heightBeforeMoving;
        positionVelocity = Vector3.zero;
        shakeOffset = Vector3.zero;
        transform.position = startPosition;
        transform.LookAt(new Vector3(0f, currentLookHeight, 0f));
    }
}
