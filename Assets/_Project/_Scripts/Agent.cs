using UnityEngine;

public class Agent : MonoBehaviour
{
    [SerializeField] protected float _maxSpeed = 4f;
    [SerializeField] protected float _maxSteering = 2f;
    [SerializeField] protected float _slowingDistance = 3f;
    [SerializeField] protected float _minDistance = 0.5f;

    protected Vector3 _currentVelocity;
    public Vector3 Velocity => _currentVelocity;

    protected virtual bool WrapsAroundBounds => true;

    protected static Vector3 Flat(Vector3 vector)
    {
        vector.y = 0f;
        return vector;
    }

    public float FlatDistance(Vector3 target)
    {
        return Flat(target - transform.position).magnitude;
    }

    protected Vector3 DesiredVector(Vector3 target, float speed)
    {
        return Flat(target - transform.position).normalized * speed;
    }

    protected Vector3 CalculateSteering(Vector3 desired)
    {
        Vector3 steering = Flat(desired) - _currentVelocity;
        return Vector3.ClampMagnitude(steering, _maxSteering * Time.deltaTime);
    }


    public Vector3 Seek(Vector3 target)
    {
        return CalculateSteering(DesiredVector(target, _maxSpeed));
    }

    public Vector3 Flee(Vector3 target)
    {
        return CalculateSteering(-DesiredVector(target, _maxSpeed));
    }

    public Vector3 Arrive(Vector3 target, float stopDistance = 0f)
    {
        float distance = FlatDistance(target) - stopDistance;

        if (distance <= _minDistance)
            return Brake();

        float desiredSpeed = Mathf.Min(_maxSpeed, _maxSpeed * (distance / _slowingDistance));
        return CalculateSteering(DesiredVector(target, desiredSpeed));
    }

    public Vector3 Pursuit(Agent target)
    {
        return Seek(PredictPosition(target));
    }

    public Vector3 Evade(Agent target)
    {
        return Flee(PredictPosition(target));
    }

    public Vector3 Brake()
    {
        return CalculateSteering(Vector3.zero);
    }

    private Vector3 PredictPosition(Agent target)
    {
        Vector3 targetPosition = target.transform.position;
        float predictionTime = FlatDistance(targetPosition) / _maxSpeed;
        return targetPosition + target.Velocity * predictionTime;
    }

    public void Move(Vector3 steering)
    {
        _currentVelocity = Vector3.ClampMagnitude(Flat(_currentVelocity + steering), _maxSpeed);
        transform.position += _currentVelocity * Time.deltaTime;

        if (WrapsAroundBounds && Bounds.Instance != null)
            transform.position = Bounds.Instance.OutOfBounds(transform.position);

        if (_currentVelocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(_currentVelocity);
    }

    protected void Stop()
    {
        _currentVelocity = Vector3.zero;
    }
}
