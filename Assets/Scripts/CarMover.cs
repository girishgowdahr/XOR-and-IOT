using UnityEngine;

public class CarMover : MonoBehaviour
{
public GateController gateController;


public Transform turnPoint;
public Transform destination;

public float speed = 5f;
public float turnSpeed = 2f;

private int stage = 0;

void Update()
{
    if (gateController == null || !gateController.IsOpen)
        return;

    if (stage == 0)
    {
        MoveTo(turnPoint.position);

        if (Vector3.Distance(transform.position, turnPoint.position) < 1f)
            stage = 1;
    }
    else if (stage == 1)
    {
        MoveTo(destination.position);

        if (Vector3.Distance(transform.position, destination.position) < 1f)
            enabled = false;
    }
}

void MoveTo(Vector3 target)
{
    target.y = transform.position.y;

    Vector3 direction = (target - transform.position).normalized;

    if (direction != Vector3.zero)
    {
        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
    }

    transform.position = Vector3.MoveTowards(
        transform.position,
        target,
        speed * Time.deltaTime);
}


}
