using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Boid : Agent
{
    [SerializeField] private float _separationRadius;
    [SerializeField] private float _neighborRadius;
    [SerializeField] private LayerMask _boidLayer;

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
        return Vector3.zero;
    }
    void Start()
    {
        
    }
    void Update()
    {
        
    }
}
