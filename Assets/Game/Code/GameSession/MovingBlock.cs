using UnityEngine;

public class MovingBlock : MonoBehaviour
{
    private Vector3 movementAxis = Vector3.right;
    private Vector3 movementCenter;
    private float speed;
    private float travelDistance;
    private int direction = 1;
    private bool isMoving;

    public void Initialize(Vector3 axis, Vector3 centerPosition, float moveSpeed, float maxDistance)
    {
        movementAxis = axis.normalized;
        movementCenter = centerPosition;
        speed = moveSpeed;
        travelDistance = maxDistance;
        direction = 1;
        isMoving = true;
    }

    private void Update()
    {
        if (!isMoving)
        {
            return;
        }

        transform.position += movementAxis * direction * speed * Time.deltaTime;

        Vector3 offsetFromCenter = transform.position - movementCenter;
        float distanceOnAxis = Vector3.Dot(offsetFromCenter, movementAxis);

        if (Mathf.Abs(distanceOnAxis) >= travelDistance)
        {
            direction *= -1;
        }
    }

    public void Stop()
    {
        isMoving = false;
    }
}
