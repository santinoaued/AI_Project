using System;
using UnityEngine;

public class Wolf : Agent
{
    public static event Action<Wolf, Vector3> Howled;

    [Header("patrol")]
    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _waypointReachDistance = 1f;

    [Header("perception")]
    [SerializeField] private float _viewDistance = 12f;
    [SerializeField, Range(1f, 360f)] private float _viewAngle = 90f;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("navigation")]
    [SerializeField] private float _nodeReachDistance = 1.5f;
    [SerializeField] private float _repathDistance = 2f;

    [Header("search")]
    [SerializeField] private float _arriveDistance = 1.5f;
    [SerializeField] private float _searchDuration = 6f;
    [SerializeField] private float _searchRadius = 8f;

    [Header("alert")]
    [SerializeField] private float _loseSightDelay = 0.75f;

    [Header("combat")]
    [SerializeField] private float _biteDamage = 25f;
    [SerializeField] private float _biteRadius = 1.5f;
    [SerializeField] private float _biteCooldown = 1.5f;

    private Hunter _hunter;
    private Perception _perception;
    private StateFeedback _feedback;
    private WolfIndicators _indicators;
    private bool _hasPendingAlert;
    private float _nextBiteTime;

    public StateMachine StateMachine { get; } = new StateMachine();
    public WolfPatrolState Patrol { get; private set; }
    public ChaseState Chase { get; private set; }
    public AlertedState Alerted { get; private set; }
    public SearchState Search { get; private set; }

    public WaypointPath Path { get; private set; }
    public Navigator Navigator { get; private set; }
    public float ArriveDistance => _arriveDistance;
    public float SearchDuration => _searchDuration;
    public float LoseSightDelay => _loseSightDelay;
    public float ViewDistance => _viewDistance;
    public float ViewAngle => _viewAngle;
    public LayerMask ObstacleMask => _obstacleMask;

    public bool CanSeeHunter { get; private set; }
    public Vector3 LastKnownPosition { get; private set; }
    public Vector3 HunterPosition => _hunter.transform.position;
    public bool HasKnownPosition => StateMachine.CurrentState != null && StateMachine.CurrentState != Patrol;
    public bool CanBite => CanSeeHunter && Time.time >= _nextBiteTime && FlatDistance(HunterPosition) <= _biteRadius;

    private void Awake()
    {
        _feedback = gameObject.AddComponent<StateFeedback>();
        _indicators = gameObject.AddComponent<WolfIndicators>();
        _perception = new Perception(transform, _viewDistance, _viewAngle, _obstacleMask);
        Path = new WaypointPath(_waypoints, _waypointReachDistance);

        Patrol = new WolfPatrolState(this);
        Chase = new ChaseState(this);
        Alerted = new AlertedState(this);
        Search = new SearchState(this);
    }

    private void OnEnable()
    {
        Howled += OnHowled;
    }

    private void OnDisable()
    {
        Howled -= OnHowled;
    }

    private void Start()
    {
        _hunter = FindFirstObjectByType<Hunter>();

        if (_hunter == null)
            Debug.LogError("wolf: no hay Hunter en la escena", this);

        Navigator = CreateNavigator(_nodeReachDistance, _repathDistance);

        if (Navigator == null)
        {
            Debug.LogError("wolf: no hay NavigationGraph en la escena", this);
            return;
        }

        Navigator.PathComputed += OnPathComputed;

        StateMachine.ChangeState(Patrol);
    }

    private void Update()
    {
        if (Navigator == null)
            return;

        CanSeeHunter = _hunter != null && _hunter.IsAlive && _perception.CanSee(_hunter.transform);

        if (CanSeeHunter)
            LastKnownPosition = HunterPosition;

        StateMachine.Update();
    }

    public void MoveTo(Vector3 target) => Navigator.MoveTo(target);

    public void FollowPatrol() => Path.Follow(this, Navigator);

    public void PursueHunter() => Move(Pursuit(_hunter));

    public Vector3 RandomSearchPoint() => NavigationGraph.Instance.RandomPointNear(LastKnownPosition, _searchRadius);

    public void Bite()
    {
        _nextBiteTime = Time.time + _biteCooldown;
        _hunter.TakeDamage(_biteDamage);
        _feedback.SetEvent("MORDIDA", 1f);
    }

    public void Howl()
    {
        Debug.Log($"{name}: aullido, ultima posicion conocida {LastKnownPosition}");
        _feedback.SetEvent("AULLIDO!", 2f);
        _indicators.PlayHowl();
        Howled?.Invoke(this, LastKnownPosition);
    }

    public bool ConsumeAlert()
    {
        bool pending = _hasPendingAlert;
        _hasPendingAlert = false;
        return pending;
    }

    public void SetStateFeedback(string stateName, Color color) => _feedback.SetState(stateName, color);

    private void OnHowled(Wolf source, Vector3 position)
    {
        if (source == this || CanSeeHunter)
            return;

        LastKnownPosition = position;
        _hasPendingAlert = true;
        _feedback.SetEvent("ALERTA RECIBIDA", 2f);
        _indicators.PlayAlertReceived(source.transform.position);
        Debug.Log($"{name}: alerta recibida de {source.name}, posicion {position}");
    }

    private void OnPathComputed(Vector3 goal, bool found)
    {
        Debug.Log($"{name}: A* hacia {goal} {(found ? "encontrado" : "sin camino")}");

        if (found)
            _feedback.SetEvent("A*", 1f);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, -_viewAngle * 0.5f, 0f) * transform.forward * _viewDistance);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, _viewAngle * 0.5f, 0f) * transform.forward * _viewDistance);

        if (!Application.isPlaying || !HasKnownPosition)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(LastKnownPosition, 0.75f);
        Gizmos.DrawLine(transform.position, LastKnownPosition);
    }
}
