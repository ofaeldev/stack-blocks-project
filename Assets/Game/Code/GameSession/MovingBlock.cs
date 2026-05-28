using UnityEngine;

public class MovingBlock : MonoBehaviour
{
    private Vector3 movementAxis = Vector3.right;
    private Vector3 startPosition;
    private float speed;
    private float travelDistance;
    private int direction = 1;
    private bool isMoving;

    public void Initialize(Vector3 axis, float moveSpeed, float maxDistance)
    {
        movementAxis = axis.normalized;
        startPosition = transform.position;
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

        Vector3 offsetFromStart = transform.position - startPosition;
        float distanceOnAxis = Vector3.Dot(offsetFromStart, movementAxis);

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
