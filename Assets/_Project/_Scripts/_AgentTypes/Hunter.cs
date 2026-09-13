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

    [Header("feedback")]
    [SerializeField] private bool _showDebugPanel = true;

    private static readonly Collider[] _sensorBuffer = new Collider[64];

    private ObjectPool<Bullet> _bulletPool;
    private float _nextAttackTime;
    private Material _bodyMaterial;
    private TextMesh _stateLabel;
    private Transform _cameraTransform;

    public StateMachine StateMachine { get; } = new StateMachine();
    public PatrolState Patrol { get; private set; }
    public AttackState Attack { get; private set; }
    public GatherState Gather { get; private set; }

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
        Renderer bodyRenderer = GetComponentInChildren<Renderer>();
        if (bodyRenderer != null)
            _bodyMaterial = bodyRenderer.material;

        if (_bulletPrefab == null)
            Debug.LogError("hunter: falta asignar prefab de bala, atacar instantaneo", this);
        if (_baitPrefab == null)
            Debug.LogError("hunter: falta asignar prefab del bait", this);

        _bulletPool = new ObjectPool<Bullet>(CreateBullet, OnGetBullet, OnReleaseBullet, OnDestroyBullet, true, 4);

        Patrol = new PatrolState(this);
        Attack = new AttackState(this);
        Gather = new GatherState(this);

        CreateStateLabel();
    }

    private void Start()
    {
        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;

        StateMachine.ChangeState(Patrol);
    }

    private void Update()
    {
        Sense();
        StateMachine.Update();
    }

    private void LateUpdate()
    {
        _stateLabel.transform.position = transform.position + Vector3.up * 2f;

        if (_cameraTransform != null)
            _stateLabel.transform.rotation = _cameraTransform.rotation;
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
            if (distance > _perceptionRadius)
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
        Debug.Log($"Hunter -> {stateName}");

        if (_bodyMaterial != null)
            _bodyMaterial.color = color;

        _stateLabel.text = stateName;
        _stateLabel.color = color;
    }

    public void SetAction(string action)
    {
        if (action == LastAction)
            return;

        LastAction = action;
        Debug.Log($"Hunter: {action}");
    }

    private void CreateStateLabel()
    {
        GameObject labelObj = new GameObject("StateLabel");
        _stateLabel = labelObj.AddComponent<TextMesh>();
        _stateLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelObj.GetComponent<MeshRenderer>().sharedMaterial = _stateLabel.font.material;
        _stateLabel.fontSize = 48;
        _stateLabel.characterSize = 0.1f;
        _stateLabel.anchor = TextAnchor.MiddleCenter;
        _stateLabel.alignment = TextAlignment.Center;
    }

    private void OnGUI()
    {
        if (!_showDebugPanel || Event.current.type != EventType.Repaint)
            return;

        string target = CurrentTarget != null ? CurrentTarget.name : "-";
        float tba = Mathf.Max(0f, _nextAttackTime - Time.time);

        GUI.Box(new Rect(10, 10, 260, 130), "Hunter");
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

        if (CurrentTarget != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, CurrentTarget.transform.position);
        }
    }
}
