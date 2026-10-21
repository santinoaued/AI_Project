using UnityEngine;
using UnityEngine.Pool;

public class Hunter : Agent
{
    [Header("combat")]
    [SerializeField] private float _tba = 3f;
    [SerializeField] private float _rangeAttackRadius = 6f;
    [SerializeField] private float _meleeAttackRadius = 2f;
    [SerializeField] private float _meleeDamage = 50f;
    [SerializeField] private float _rangeDamage = 25f;
    [SerializeField] private Bullet _bulletPrefab;

    [Header("patrol")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _waypointReachDistance = 1f;

    [Header("perception")]
    [SerializeField] private LayerMask _boidLayer;
    [SerializeField] private float _perceptionRadius = 12f;

    [Header("bait")]
    [SerializeField] private Bait _baitPrefab;
    [SerializeField] private int _maxBaits = 5;
    [SerializeField] private float _baitSpawnInterval = 5f;
    [SerializeField] private float _placingBaitDuration = 1.5f;

    [Header("gather")]
    [SerializeField] private float _gatherDuration = 2f;

    [Header("health")]
    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _respawnDelay = 3f;

    [Header("escape")]
    [SerializeField] private LayerMask _wolfLayer;
    [SerializeField] private float _escapeRadius = 10f;
    [SerializeField] private float _escapeExitMargin = 3f;
    [SerializeField] private float _escapeLookAhead = 3f;
    [SerializeField] private float _escapeSpeedMultiplier = 1.4f;
    [SerializeField] private float _escapeSteeringMultiplier = 4f;

    [Header("feedback")]
    [SerializeField] private bool _showDebugPanel = true;

    private static readonly Collider[] _sensorBuffer = new Collider[64];

    private ObjectPool<Bullet> _bulletPool;
    private float _nextAttackTime;
    private StateFeedback _feedback;
    private Renderer[] _renderers;
    private Collider[] _colliders;
    private Vector3 _spawnPosition;
    private float _baseMaxSpeed;
    private float _baseMaxSteering;

    private static readonly float[] EscapeAngles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f };

    public StateMachine StateMachine { get; } = new StateMachine();
    public PatrolState Patrol { get; private set; }
    public AttackState Attack { get; private set; }
    public GatherState Gather { get; private set; }
    public EscapeState Escape { get; private set; }
    public DeadState Dead { get; private set; }

    public float Health { get; private set; }
    public bool IsAlive => Health > 0f;
    public float RespawnDelay => _respawnDelay;
    public Wolf ClosestWolf { get; private set; }

    public Transform[] Waypoints => _waypoints;
    public float WaypointReachDistance => _waypointReachDistance;
    public float MeleeAttackRadius => _meleeAttackRadius;
    public float RangeAttackRadius => _rangeAttackRadius;
    public float PerceptionRadius => _perceptionRadius;
    public float BaitSpawnInterval => _baitSpawnInterval;
    public float PlacingBaitDuration => _placingBaitDuration;
    public float GatherDuration => _gatherDuration;
    public bool CanSpawnBait => _baitPrefab != null && Bait.ActiveCount < _maxBaits;

    public Boid ClosestAliveBoid { get; private set; }
    public Boid ClosestDeadBoid { get; private set; }
    public int DetectedBoids { get; private set; }

    public Boid CurrentTarget { get; set; }
    public string CurrentStateName { get; private set; } = "-";
    public string LastAction { get; private set; } = "-";

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>();
        _spawnPosition = transform.position;
        _baseMaxSpeed = _maxSpeed;
        _baseMaxSteering = _maxSteering;
        Health = _maxHealth;

        _feedback = gameObject.AddComponent<StateFeedback>();

        if (_bulletPrefab == null)
            Debug.LogError("hunter: falta asignar prefab de bala, atacar instantaneo", this);
        if (_baitPrefab == null)
            Debug.LogError("hunter: falta asignar prefab del bait", this);

        _bulletPool = new ObjectPool<Bullet>(CreateBullet, OnGetBullet, OnReleaseBullet, OnDestroyBullet, true, 4);

        Patrol = new PatrolState(this);
        Attack = new AttackState(this);
        Gather = new GatherState(this);
        Escape = new EscapeState(this);
        Dead = new DeadState(this);
    }

    private void Start()
    {
        StateMachine.ChangeState(Patrol);
    }

    private void Update()
    {
        Sense();
        StateMachine.Update();
    }

    private void Sense()
    {
        ClosestAliveBoid = null;
        ClosestDeadBoid = null;
        DetectedBoids = 0;

        float closestAliveDistance = float.MaxValue;
        float closestDeadDistance = float.MaxValue;

        int count = Physics.OverlapSphereNonAlloc(transform.position, _perceptionRadius, _sensorBuffer, _boidLayer, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            if (!_sensorBuffer[i].TryGetComponent(out Boid boid))
                continue;

            float distance = FlatDistance(boid.transform.position);
            if (distance > _perceptionRadius || !HasLineOfSight(boid.transform.position))
                continue;

            if (boid.IsActive)
            {
                DetectedBoids++;

                if (distance < closestAliveDistance)
                {
                    closestAliveDistance = distance;
                    ClosestAliveBoid = boid;
                }
            }
            else if (boid.IsDead && distance < closestDeadDistance)
            {
                closestDeadDistance = distance;
                ClosestDeadBoid = boid;
            }
        }

        SenseWolves();
    }

    private void SenseWolves()
    {
        ClosestWolf = null;
        float radius = StateMachine.CurrentState == Escape ? _escapeRadius + _escapeExitMargin : _escapeRadius;
        float closestWolfDistance = radius;

        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _sensorBuffer, _wolfLayer, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            if (!_sensorBuffer[i].TryGetComponent(out Wolf wolf) || !HasLineOfSight(wolf.transform.position))
                continue;

            float distance = FlatDistance(wolf.transform.position);
            if (distance <= closestWolfDistance)
            {
                closestWolfDistance = distance;
                ClosestWolf = wolf;
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsAlive)
            return;

        Health = Mathf.Max(0f, Health - damage);
        SetAction($"Recibe {damage} de dano");

        if (!IsAlive)
            StateMachine.ChangeState(Dead);
    }

    public void Respawn()
    {
        Health = _maxHealth;
        transform.position = _spawnPosition;
        Stop();
        SetAction("Respawn");
        StateMachine.ChangeState(Patrol);
    }

    public void SetEscapeBoost(bool active)
    {
        _maxSpeed = active ? _baseMaxSpeed * _escapeSpeedMultiplier : _baseMaxSpeed;
        _maxSteering = active ? _baseMaxSteering * _escapeSteeringMultiplier : _baseMaxSteering;
    }

    public Vector3 EscapeSteering(Wolf wolf)
    {
        Vector3 away = Flat(transform.position - PredictPosition(wolf)).normalized;

        foreach (float angle in EscapeAngles)
        {
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * away;

            if (!IsObstructed(direction, _escapeLookAhead))
                return Seek(transform.position + direction * _escapeLookAhead);
        }

        return Seek(transform.position + away * _escapeLookAhead);
    }

    public void SetBodyActive(bool active)
    {
        foreach (Renderer bodyRenderer in _renderers)
            bodyRenderer.enabled = active;

        foreach (Collider bodyCollider in _colliders)
            bodyCollider.enabled = active;
    }

    public bool CanAttack() => Time.time >= _nextAttackTime;
    public void ResetAttackTimer() => _nextAttackTime = Time.time + _tba;

    public void MeleeAttack(Boid target)
    {
        target.TakeDamage(_meleeDamage);
        SetAction($"Melee a {target.name}");
    }

    public void RangeAttack(Boid target)
    {
        if (_bulletPrefab == null)
        {
            target.TakeDamage(_rangeDamage);
            SetAction($"Disparo instantaneo a {target.name}");
            return;
        }

        _bulletPool.Get().Launch(this, target, _rangeDamage);
        SetAction($"Disparo a {target.name}");
    }

    public void ReleaseBullet(Bullet bullet) => _bulletPool.Release(bullet);

    private Bullet CreateBullet()
    {
        Bullet bullet = Instantiate(_bulletPrefab);
        bullet.gameObject.SetActive(false);
        return bullet;
    }

    private void OnGetBullet(Bullet bullet)
    {
        bullet.transform.SetPositionAndRotation(transform.position, Quaternion.identity);
        bullet.gameObject.SetActive(true);
    }

    private static void OnReleaseBullet(Bullet bullet) => bullet.gameObject.SetActive(false);

    private static void OnDestroyBullet(Bullet bullet) => Destroy(bullet.gameObject);

    public void SpawnBait()
    {
        if (!CanSpawnBait)
            return;

        Instantiate(_baitPrefab, transform.position, Quaternion.identity);
        SetAction("Cebo generado");
    }

    public void SetStateFeedback(string stateName, Color color)
    {
        CurrentStateName = stateName;
        _feedback.SetState(stateName, color);
    }

    public void SetAction(string action)
    {
        if (action == LastAction)
            return;

        LastAction = action;
        Debug.Log($"Hunter: {action}");
    }

    private void OnGUI()
    {
        if (!_showDebugPanel || Event.current.type != EventType.Repaint)
            return;

        string target = CurrentTarget != null ? CurrentTarget.name : "-";
        float tba = Mathf.Max(0f, _nextAttackTime - Time.time);

        GUI.Box(new Rect(10, 10, 260, 150), "Hunter");
        GUI.Label(new Rect(20, 135, 240, 20), $"Vida: {Health:0}/{_maxHealth:0}");
        GUI.Label(new Rect(20, 35, 240, 20), $"Estado: {CurrentStateName}");
        GUI.Label(new Rect(20, 55, 240, 20), $"Objetivo: {target}");
        GUI.Label(new Rect(20, 75, 240, 20), $"Boids detectados: {DetectedBoids}");
        GUI.Label(new Rect(20, 95, 240, 20), $"TBA restante: {tba:0.0}s   Cebos: {Bait.ActiveCount}/{_maxBaits}");
        GUI.Label(new Rect(20, 115, 240, 20), $"Accion: {LastAction}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _perceptionRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _rangeAttackRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _meleeAttackRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _escapeRadius);

        if (CurrentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, CurrentTarget.transform.position);
        }
    }
}
