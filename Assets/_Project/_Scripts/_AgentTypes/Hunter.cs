using UnityEngine;

public class Hunter : Agent
{
    [Header("combat")]
    [SerializeField] private float _tba;
    [SerializeField] private float _rangeAttackRadius;
    [SerializeField] private float _meleeAttackRadius;
    [SerializeField] private float _attackDamage = 10f;

    [Header("patrol")]
    [SerializeField] private Transform[] _waypoints;
    public Transform[] Waypoints => _waypoints;

    [Header("Percepción")]
    [SerializeField] private LayerMask _boidLayer;
    [SerializeField] private float _perceptionRadius;

    private IState _currentState;
    private float _attackTimer;

    public float MeleeAttackRadius => _meleeAttackRadius;
    public float RangeAttackRadius => _rangeAttackRadius;
    public float PerceptionRadius => _perceptionRadius;
    public float AttackDamage => _attackDamage;

    private void Start()
    {
        ChangeState(new PatrolState(this));
    }

    private void Update()
    {
        _attackTimer += Time.deltaTime;
        _currentState.Update();
    }

    // references so that the states can access Move(), Arrive(), Pursuit(), etc.
    public Vector3 GoToWaypoint(Vector3 target)
    {
        return Arrive(target);
    }

    public Vector3 PursuitTarget(Vector3 targetPosition, Vector3 targetVelocity)
    {
        return Pursuit(targetPosition, targetVelocity);
    }

    public void MoveHunter(Vector3 steering)
    {
        Move(steering);
    }

    public bool CanAttack() => _attackTimer >= _tba;
    public void ResetAttackTimer() => _attackTimer = 0f;

    public Boid DetectTarget()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _perceptionRadius, _boidLayer);

        foreach (Collider collider in colliders)
        {
            Boid boid = collider.GetComponent<Boid>();
            if (boid != null && boid.IsActive)
                return boid;
        }

        return null;
    }

    public Boid DetectInactiveBoid()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _perceptionRadius, _boidLayer);

        foreach (Collider collider in colliders)
        {
            Boid boid = collider.GetComponent<Boid>();
            if (boid != null && !boid.IsActive)
                return boid;
        }

        return null;
    }

    public void ChangeState(IState newState)
    {
        if (_currentState != null)
        {
            _currentState.Exit();
        }

        _currentState = newState;
        _currentState.Enter();
    }
}
