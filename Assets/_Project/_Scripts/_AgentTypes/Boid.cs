using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Boid : Agent
{
    [SerializeField] private float _health = 100;
    private bool _isActive = true;
    public bool IsActive => _isActive;

    [SerializeField] private float _respawnTime = 3f;
    [SerializeField] private float _spawnRange = 20f;

    // raidus
    [SerializeField] private float _separationRadius;
    [SerializeField] private float _neighborRadius;
    [SerializeField] private float _visionRadius;

    // layers
    [SerializeField] private LayerMask _boidLayer;
    [SerializeField] private LayerMask _hunterLayer;
    [SerializeField] private LayerMask _baitLayer;

    // weight
    [SerializeField] private float _separationWeight = 2f;
    [SerializeField] private float _alignmentWeight = 1f;
    [SerializeField] private float _cohesionWeight = 1f;

    // hunter
    private Hunter _detectedHunter;

    // bait
    private Bait _detectedBait;
    [SerializeField] private float _eatInterval = 1f;
    private float _eatTimer;
    [SerializeField] private float _damage = 25f;


    private List<Boid> GetNeighbors(float radius)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, radius, _boidLayer);
        List<Boid> neighbors = new List<Boid>();

        foreach (Collider collider in colliders)
        {
            Boid boid = collider.GetComponent<Boid>();

            if (boid != null && boid.gameObject != gameObject)
                neighbors.Add(boid);
        }

        return neighbors;
    }

    private Vector3 Separation() 
    {
        List<Boid> neighbors = GetNeighbors(_separationRadius);
        Vector3 separationForce = Vector3.zero;

        foreach (Boid neighbor in neighbors)
        {
            separationForce += transform.position - neighbor.transform.position;
        }

        if (neighbors.Count > 0) 
        {
            separationForce /= neighbors.Count;
        }

        return CalculateSteering(separationForce);
    }

    private Vector3 Alignment() 
    {
        List<Boid> neighbors = GetNeighbors(_neighborRadius);
        Vector3 alignmentForce = Vector3.zero;

        foreach (Boid neighbor in neighbors) 
        {
            alignmentForce += neighbor.Velocity;
        }

        if (neighbors.Count > 0)
        {
            alignmentForce /= neighbors.Count;
        }

        return CalculateSteering(alignmentForce);
    }

    private Vector3 Cohesion()
    {
        List<Boid> neighbors = GetNeighbors(_neighborRadius);
        Vector3 cohesionForce = Vector3.zero;

        foreach (Boid neighbor in neighbors)
        {
            cohesionForce += neighbor.transform.position;
        }

        if (neighbors.Count > 0)
        {
            cohesionForce /= neighbors.Count;
        }

        cohesionForce -= transform.position;

        return CalculateSteering(cohesionForce);
    }

    private Vector3 CalculateFlocking()
    {
        return (Separation() * _separationWeight) + (Alignment() * _alignmentWeight) + (Cohesion() * _cohesionWeight);
    }

    // DetectHunter no reusa GetNeighbors por claridad; se podría unificar si la prioridad fuera rendimiento
    private Hunter DetectHunter() 
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _visionRadius, _hunterLayer);

        foreach (Collider collider in colliders)
        {
            Hunter hunter = collider.GetComponent<Hunter>();
            if (hunter != null)
            {
                return hunter;
            }
        }

            return null;
    }

    private Bait DetectBait() 
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, _visionRadius, _baitLayer);

        foreach (Collider collider in colliders)
        {
            Bait bait = collider.GetComponent<Bait>();
            if (bait != null)
            {
                return bait;
            }
        }

        return null;
    }

    private void TryEat()
    {
        if (_detectedBait == null)
            return;

        Vector3 direction = _detectedBait.transform.position - transform.position;
        float distance = direction.magnitude;

        if (distance < _minDistance)
        {
            _eatTimer += Time.deltaTime;

            if (_eatTimer >= _eatInterval)
            {
                _detectedBait.TakeDamage(_damage);
                _eatTimer = 0f;
            }
        }
    }

    public void TakeDamage(float damage) 
    {
        _health -= damage;
        if (_health <= 0f) 
        {
            _isActive = false;
        }
    }

    private void Update()
    {
        if (!_isActive)
        {
            return;
        }

        Vector3 steering;

        _detectedHunter = DetectHunter();
        _detectedBait = DetectBait();

        if (_detectedHunter != null)
        {
            steering = Evade(_detectedHunter.transform.position, _detectedHunter.Velocity);
        }
        else if (_detectedBait != null)
        {
            steering = Arrive(_detectedBait.transform.position);
        }
        else
        {
            steering = CalculateFlocking();
        }

        Move(steering);
        TryEat();
    }

    public void Collect()
    {
        StartCoroutine(CollectRoutine());
    }

    private IEnumerator CollectRoutine()
    {
        gameObject.SetActive(false);
        yield return new WaitForSeconds(_respawnTime);

        transform.position = new Vector3(UnityEngine.Random.Range(-_spawnRange, _spawnRange), transform.position.y, UnityEngine.Random.Range(-_spawnRange, _spawnRange));
        _health = 100f;
        _isActive = true;
        gameObject.SetActive(true);
    }
}
