using System.Collections.Generic;
using UnityEngine;

public class GraphNode : MonoBehaviour
{
    private readonly List<GraphNode> _neighbors = new List<GraphNode>();

    public IReadOnlyList<GraphNode> Neighbors => _neighbors;
    public Vector3 Position => transform.position;

    public void Connect(GraphNode other)
    {
        if (other != this && !_neighbors.Contains(other))
            _neighbors.Add(other);
    }
}
