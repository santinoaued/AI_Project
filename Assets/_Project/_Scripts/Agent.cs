using UnityEngine;

public class Agent : MonoBehaviour
{
    [SerializeField] protected float _maxSpeed = 4f;
    [SerializeField] protected float _maxSteering = 2f;
    [SerializeField] protected float _slowingDistance = 3f;
    [SerializeField] protected float _minDistance = 0.5f;
    [SerializeField] protected float _bodyRadius = 0.4f;

    private const float ObstacleCastHeight = 1f;
    private const float SkinWidth = 0.02f;
    private const int MaxSlideSteps = 3;

    private static readonly Collider[] _obstacleBuffer = new Collider[8];
    private static int _obstacleMask = -1;

    private static int ObstacleMask
    {
        get
        {
            if (_obstacleMask < 0)
                _obstacleMask = LayerMask.GetMask("_obstacleLayer");

            return _obstacleMask;
        }
    }

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

    public bool HasLineOfSight(Vector3 target)
    {
        Vector3 end = new Vector3(target.x, ObstacleCastHeight, target.z);
        return !Physics.Linecast(CastOrigin(), end, ObstacleMask);
    }

    protected Navigator CreateNavigator(float nodeReachDistance, float repathDistance)
    {
        return NavigationGraph.Instance != null
            ? new Navigator(this, NavigationGraph.Instance.Pathfinder, nodeReachDistance, repathDistance)
            : null;
    }

    public bool IsObstructed(Vector3 direction, float distance)
    {
        return Physics.SphereCast(CastOrigin(), _bodyRadius, direction, out _, distance, ObstacleMask);
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

    protected Vector3 PredictPosition(Agent target)
    {
        Vector3 targetPosition = target.transform.position;
        float predictionTime = FlatDistance(targetPosition) / _maxSpeed;
        return targetPosition + target.Velocity * predictionTime;
    }

    public void Move(Vector3 steering)
    {
        _currentVelocity = Vector3.ClampMagnitude(Flat(_currentVelocity + steering), _maxSpeed);
        MoveAvoidingObstacles(_currentVelocity * Time.deltaTime);

        if (WrapsAroundBounds && Bounds.Instance != null)
            transform.position = Bounds.Instance.OutOfBounds(transform.position);

        if (_currentVelocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(_currentVelocity);
    }

    private Vector3 CastOrigin()
    {
        return new Vector3(transform.position.x, ObstacleCastHeight, transform.position.z);
    }

    private void MoveAvoidingObstacles(Vector3 displacement)
    {
        for (int i = 0; i < MaxSlideSteps && displacement.sqrMagnitude > 0.000001f; i++)
        {
            float distance = displacement.magnitude;
            Vector3 direction = displacement / distance;

            if (!Physics.SphereCast(CastOrigin(), _bodyRadius, direction, out RaycastHit hit, distance + SkinWidth, ObstacleMask))
            {
                transform.position += displacement;
                break;
            }

            float travel = Mathf.Max(0f, hit.distance - SkinWidth);
            transform.position += direction * travel;

            Vector3 normal = Flat(hit.normal).normalized;
            _currentVelocity = Vector3.ProjectOnPlane(_currentVelocity, normal);
            displacement = Vector3.ProjectOnPlane(displacement - direction * travel, normal);
        }

        PushOutOfObstacles();
    }

    private void PushOutOfObstacles()
    {
        Vector3 center = CastOrigin();
        int count = Physics.OverlapSphereNonAlloc(center, _bodyRadius, _obstacleBuffer, ObstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider obstacle = _obstacleBuffer[i];
            Vector3 offset = Flat(center - obstacle.ClosestPoint(center));

            Vector3 push = offset.sqrMagnitude > 0.000001f
                ? offset.normalized * (_bodyRadius - offset.magnitude)
                : ExitVector(obstacle, center);

            transform.position += push;
            center += push;
        }
    }

    private Vector3 ExitVector(Collider obstacle, Vector3 center)
    {
        var bounds = obstacle.bounds;
        float left = center.x - bounds.min.x;
        float right = bounds.max.x - center.x;
        float back = center.z - bounds.min.z;
        float forward = bounds.max.z - center.z;
        float smallest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(back, forward));
        float exitDistance = smallest + _bodyRadius;

        if (smallest == left)
            return Vector3.left * exitDistance;

        if (smallest == right)
            return Vector3.right * exitDistance;

        if (smallest == back)
            return Vector3.back * exitDistance;

        return Vector3.forward * exitDistance;
    }

    protected void Stop()
    {
        _currentVelocity = Vector3.zero;
    }
}
