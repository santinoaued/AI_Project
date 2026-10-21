using System.Collections.Generic;
using UnityEngine;

public class NavigationGraph : MonoBehaviour
{
    public static NavigationGraph Instance { get; private set; }

    [SerializeField] private float _connectionRadius = 10f;
    [SerializeField] private float _agentRadius = 0.4f;
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private bool _drawGizmos = true;

    private readonly List<GraphNode> _nearBuffer = new List<GraphNode>();
    private GraphNode[] _nodes;

    public Pathfinder Pathfinder { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        _nodes = GetComponentsInChildren<GraphNode>();
        Pathfinder = new Pathfinder(_nodes, _agentRadius, _obstacleMask);
    }

    private void Start()
    {
        ConnectNodes();
    }

    public Vector3 RandomPointNear(Vector3 position, float radius)
    {
        _nearBuffer.Clear();

        foreach (GraphNode node in _nodes)
        {
            if (Pathfinder.FlatSqrDistance(node.Position, position) <= radius * radius)
                _nearBuffer.Add(node);
        }

        return _nearBuffer.Count == 0 ? position : _nearBuffer[Random.Range(0, _nearBuffer.Count)].Position;
    }

    private void ConnectNodes()
    {
        float sqrRadius = _connectionRadius * _connectionRadius;

        for (int i = 0; i < _nodes.Length; i++)
        {
            for (int j = i + 1; j < _nodes.Length; j++)
            {
                if (Pathfinder.FlatSqrDistance(_nodes[i].Position, _nodes[j].Position) > sqrRadius)
                    continue;

                if (Pathfinder.IsEdgeBlocked(_nodes[i], _nodes[j]))
                    continue;

                _nodes[i].Connect(_nodes[j]);
                _nodes[j].Connect(_nodes[i]);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!_drawGizmos)
            return;

        GraphNode[] nodes = _nodes ?? GetComponentsInChildren<GraphNode>();

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        foreach (GraphNode node in nodes)
            Gizmos.DrawSphere(node.Position, 0.3f);

        if (Pathfinder == null)
            return;

        Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
        foreach (GraphNode node in nodes)
        {
            foreach (GraphNode neighbor in node.Neighbors)
            {
                if (node.GetInstanceID() < neighbor.GetInstanceID() && !Pathfinder.IsEdgeBlocked(node, neighbor))
                    Gizmos.DrawLine(node.Position, neighbor.Position);
            }
        }
    }
}
