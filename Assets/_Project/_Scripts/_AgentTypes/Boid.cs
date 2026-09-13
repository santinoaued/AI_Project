using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boid : Agent
{
    private enum BoidState { Flocking, Evading, GoingToBait, Dead }

    [SerializeField] private float _maxHealth = 100f;
    [SerializeField] private float _respawnTime = 3f;

    [Header("radius")]
    [SerializeField] private float _separationRadius = 2f;
    [SerializeField] private float _neighborRadius = 5f;
    [SerializeField] private float _visionRadius = 8f;

    [Header("layers")]
    [SerializeField] private LayerMask _boidLayer;
    [SerializeField] private LayerMask _hunterLayer;
    [SerializeField] private LayerMask _baitLayer;

    [Header("weights")]
    [SerializeField] private float _separationWeight = 2f;
    [SerializeField] private float _alignmentWeight = 1f;
    [SerializeField] private float _cohesionWeight = 1f;
    [SerializeField] private float _evadeWeight = 3f;
    [SerializeField] private float _arriveWeight = 1.5f;

    [Header("bait")]
    [SerializeField] private float _eatDistance = 1.2f;
    [SerializeField] private float _eatInterval = 1f;
    [SerializeField] private float _damage = 25f;

    [Header("feedback")]
    [SerializeField] private Color _evadeColor = new Color(1f, 0.3f, 0.3f);
    [SerializeField] private Color _baitColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color _deadColor = new Color(0.25f, 0.25f, 0.25f);

    private static readonly Collider[] _sensorBuffer = new Collider[64];

    private readonly List<Boid> _neighbors = new List<Boid>();
    private Hunter _detectedHunter;
    private Bait _detectedBait;
    private int _sensorMask;

    private float _health;
    private float _eatTimer;
    private bool _isActive = true;
    private bool _isCollected;
    private BoidState _state = BoidState.Flocking;

    private Renderer[] _renderers;
    private Collider[] _colliders;
    private Material _material;
    private Color _baseColor = Color.white;
    private WaitForSeconds _respawnWait;

    public bool IsActive => _isActive && !_isCollected;
    public bool IsDead => !_isActive && !_isCollected;
    public bool IsCollected => _isCollected;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>();
        _sensorMask = _boidLayer | _hunterLayer | _baitLayer;
        _respawnWait = new WaitForSeconds(_respawnTime);

        if (_renderers.Length > 0)
        {
            _material = _renderers[0].material;
            _baseColor = _material.color;
        }
    }

    private void Start()
    {
        _health = _maxHealth;
        RandomizeVelocity();
    }

    private void Update()
    {
        if (!IsActive)
            return;

        Sense();

        Vector3 steering = Separation() * _separationWeight;

        if (_detectedHunter != null)
        {
            steering += Evade(_detectedHunter) * _evadeWeight;
            SetState(BoidState.Evading);
        }
        else if (_detectedBait != null)
        {
            steering += Arrive(_detectedBait.transform.position, _eatDistance) * _arriveWeight;
            SetState(BoidState.GoingToBait);
            TryEat();
        }
        else
        {
            steering += Alignment() * _alignmentWeight
                      + Cohesion() * _cohesionWeight
                      + Cruise();
            SetState(BoidState.Flocking);
        }

        Move(steering);
    }

    private void Sense()
    {
        _neighbors.Clear();
        _detectedHunter = null;

        Bait closestBait = null;
        float closestBaitDistance = float.MaxValue;

        float radius = Mathf.Max(_neighborRadius, _visionRadius);
        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _sensorBuffer, _sensorMask, QueryTriggerInteraction.Collide);

        for (int i = 0; i < count; i++)
        {
            Collider sensed = _sensorBuffer[i];
            float distance = FlatDistance(sensed.transform.position);

            if (sensed.TryGetComponent(out Boid boid))
            {
                if (boid != this && !boid.IsCollected && distance <= _neighborRadius)
                    _neighbors.Add(boid);
            }
            else if (distance > _visionRadius)
            {
                continue;
            }
            else if (_detectedHunter == null && sensed.TryGetComponent(out Hunter hunter))
            {
                _detectedHunter = hunter;
            }
            else if (distance < closestBaitDistance && sensed.TryGetComponent(out Bait bait))
            {
                closestBaitDistance = distance;
                closestBait = bait;
            }
        }

        if (closestBait != _detectedBait)
            _eatTimer = 0f;

        _detectedBait = closestBait;
    }

    private Vector3 Separation()
    {
        Vector3 away = Vector3.zero;

        foreach (Boid neighbor in _neighbors)
        {
            Vector3 offset = Flat(transform.position - neighbor.transform.position);
            float sqrDistance = offset.sqrMagnitude;

            if (sqrDistance > _separationRadius * _separationRadius)
                continue;

            away += offset / Mathf.Max(sqrDistance, 0.0001f);
        }

        return away == Vector3.zero ? Vector3.zero : Seek(transform.position + away);
    }

    private Vector3 Alignment()
    {
        Vector3 velocitySum = Vector3.zero;

        foreach (Boid neighbor in _neighbors)
        {
            if (neighbor.IsActive)
                velocitySum += neighbor.Velocity;
        }

        return velocitySum.sqrMagnitude < 0.0001f ? Vector3.zero : Seek(transform.position + velocitySum);
    }

    private Vector3 Cohesion()
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (Boid neighbor in _neighbors)
        {
            if (!neighbor.IsActive)
                continue;

            center += neighbor.transform.position;
            count++;
        }

        return count == 0 ? Vector3.zero : Seek(center / count);
    }

    private Vector3 Cruise()
    {
        if (_currentVelocity.sqrMagnitude < 0.0001f)
            RandomizeVelocity();

        return Seek(transform.position + _currentVelocity);
    }

    private void RandomizeVelocity()
    {
        Vector2 random = Random.insideUnitCircle.normalized;
        _currentVelocity = new Vector3(random.x, 0f, random.y) * _maxSpeed;
    }

    private void TryEat()
    {
        if (FlatDistance(_detectedBait.transform.position) > _eatDistance + 0.5f)
        {
            _eatTimer = 0f;
            return;
        }

        _eatTimer += Time.deltaTime;

        if (_eatTimer >= _eatInterval)
        {
            _detectedBait.TakeDamage(_damage);
            _eatTimer = 0f;
        }
    }

    public void TakeDamage(float damage)
    {
        if (!IsActive)
            return;

        _health -= damage;

        if (_health <= 0f)
        {
            _health = 0f;
            _isActive = false;
            _detectedBait = null;
            Stop();
            SetState(BoidState.Dead);
            Debug.Log($"{name}: eliminado");
        }
    }

    public void Collect()
    {
        if (IsDead)
            StartCoroutine(CollectRoutine());
    }

    private IEnumerator CollectRoutine()
    {
        _isCollected = true;
        SetVisible(false);

        yield return _respawnWait;

        Vector3 randomPoint = Bounds.Instance.RandomPoint();
        transform.position = new Vector3(randomPoint.x, transform.position.y, randomPoint.z);
        _health = _maxHealth;
        _eatTimer = 0f;
        RandomizeVelocity();
        SetState(BoidState.Flocking);

        _isActive = true;
        _isCollected = false;
        SetVisible(true);
    }

    private void SetState(BoidState state)
    {
        if (_state == state || _material == null)
        {
            _state = state;
            return;
        }

        _state = state;
        _material.color = state switch
        {
            BoidState.Evading => _evadeColor,
            BoidState.GoingToBait => _baitColor,
            BoidState.Dead => _deadColor,
            _ => _baseColor
        };
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer renderer in _renderers)
            renderer.enabled = visible;

        foreach (Collider collider in _colliders)
            collider.enabled = visible;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !IsActive)
            return;

        if (_detectedHunter != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _detectedHunter.transform.position);
        }
        else if (_detectedBait != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _detectedBait.transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _separationRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _neighborRadius);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _visionRadius);
    }
}
