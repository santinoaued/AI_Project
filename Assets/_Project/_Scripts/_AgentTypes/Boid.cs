using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class Boid : Agent
{
    [SerializeField] private float _separationRadius;
    [SerializeField] private float _neighborRadius;
    [SerializeField] private LayerMask _boidLayer;

    // weight
    [SerializeField] private float _separationWeight = 2f;
    [SerializeField] private float _alignmentWeight = 1f;
    [SerializeField] private float _cohesionWeight = 1f;

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

    void Update()
    {
        Vector3 steering = CalculateFlocking();
        Move(steering);
    }
}
