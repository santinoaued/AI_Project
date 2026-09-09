using UnityEngine;

public class Agent : MonoBehaviour
{
    [SerializeField] protected float _maxSpeed;
    [SerializeField] protected float _maxSteering;
    [SerializeField] protected float _slowingDistance;
    [SerializeField] protected float _minDistance;

    protected Vector3 _currentVelocity;
    public Vector3 Velocity => _currentVelocity;

    protected Vector3 DesiredVector(Vector3 target)
    {
        return (target - transform.position).normalized * _maxSpeed;
    }

    protected Vector3 CalculateSteering(Vector3 desired)
    {
        Vector3 steering = desired - _currentVelocity;
        steering = Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
        return steering;
    }

    protected Vector3 Seek(Vector3 target)
    {
        Vector3 desired = DesiredVector(target);
        return CalculateSteering(desired);
    }

    protected Vector3 Flee(Vector3 target)
    {
        Vector3 desired = -DesiredVector(target);
        return CalculateSteering(desired);
    }

    protected Vector3 Arrive(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        float distance = direction.magnitude;

        if (distance < _minDistance)
            return -_currentVelocity;

        float targetSpeed = _maxSpeed * (distance / _slowingDistance);
        float desiredSpeed = Mathf.Min(targetSpeed, _maxSpeed);

        Vector3 desired = direction.normalized * desiredSpeed;
        return CalculateSteering(desired);
    }

    protected Vector3 Pursuit(Vector3 targetPosition, Vector3 targetVelocity)
    {
        float distance = (targetPosition - transform.position).magnitude;
        float predictionTime = distance / _maxSpeed;
        Vector3 futurePosition = targetPosition + targetVelocity * predictionTime;
        return Seek(futurePosition);
    }

    protected Vector3 Evade(Vector3 targetPosition, Vector3 targetVelocity)
    {
        float distance = (targetPosition - transform.position).magnitude;
        float predictionTime = distance / _maxSpeed;
        Vector3 futurePosition = targetPosition + targetVelocity * predictionTime;
        return Flee(futurePosition);
    }

    protected void Move(Vector3 steering)
    {
        _currentVelocity += steering;
        transform.position += _currentVelocity * Time.deltaTime;
        transform.position = Bounds.Instance.OutOfBounds(transform.position);
    }
}
