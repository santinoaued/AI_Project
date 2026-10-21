using System.Collections.Generic;
using UnityEngine;

public class Pathfinder
{
    private const float CastHeight = 1f;
    private const int HeuristicNodeThreshold = 100;

    private readonly GraphNode[] _nodes;
    private readonly float _agentRadius;
    private readonly LayerMask _obstacleMask;

    private readonly PriorityQueue<GraphNode> _frontier = new PriorityQueue<GraphNode>();
    private readonly Dictionary<GraphNode, GraphNode> _cameFrom = new Dictionary<GraphNode, GraphNode>();
    private readonly Dictionary<GraphNode, float> _costSoFar = new Dictionary<GraphNode, float>();
    private readonly Dictionary<long, bool> _blockedEdges = new Dictionary<long, bool>();

    public Pathfinder(GraphNode[] nodes, float agentRadius, LayerMask obstacleMask)
    {
        _nodes = nodes;
        _agentRadius = agentRadius;
        _obstacleMask = obstacleMask;
    }

    public static float FlatSqrDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }

    public bool IsBlocked(Vector3 from, Vector3 to)
    {
        Vector3 origin = new Vector3(from.x, CastHeight, from.z);
        Vector3 offset = new Vector3(to.x - from.x, 0f, to.z - from.z);
        float distance = offset.magnitude;

        if (distance < Mathf.Epsilon)
            return false;

        return Physics.SphereCast(origin, _agentRadius, offset / distance, out _, distance, _obstacleMask);
    }

    public bool IsEdgeBlocked(GraphNode a, GraphNode b)
    {
        long key = EdgeKey(a, b);

        if (_blockedEdges.TryGetValue(key, out bool blocked))
            return blocked;

        blocked = IsBlocked(a.Position, b.Position);
        _blockedEdges[key] = blocked;
        _blockedEdges[EdgeKey(b, a)] = blocked;
        return blocked;
    }

    public bool TryFindPath(Vector3 from, Vector3 to, List<Vector3> path)
    {
        path.Clear();

        GraphNode start = NearestVisibleNode(from);
        GraphNode goal = NearestVisibleNode(to);

        if (start == null || goal == null)
            return false;

        _frontier.Clear();
        _cameFrom.Clear();
        _costSoFar.Clear();

        _frontier.Enqueue(start, 0f);
        _cameFrom[start] = null;
        _costSoFar[start] = 0f;

        while (_frontier.TryDequeue(out GraphNode current, out float priority))
        {
            float cost = _costSoFar[current];

            if (priority > cost + Estimate(current, goal))
                continue;

            if (current == goal)
                break;

            IReadOnlyList<GraphNode> neighbors = current.Neighbors;

            for (int i = 0; i < neighbors.Count; i++)
            {
                GraphNode neighbor = neighbors[i];

                if (IsEdgeBlocked(current, neighbor))
                    continue;

                float newCost = cost + Vector3.Distance(current.Position, neighbor.Position);

                if (_costSoFar.TryGetValue(neighbor, out float knownCost) && newCost >= knownCost)
                    continue;

                _costSoFar[neighbor] = newCost;
                _cameFrom[neighbor] = current;
                _frontier.Enqueue(neighbor, newCost + Estimate(neighbor, goal));
            }
        }

        if (!_cameFrom.ContainsKey(goal))
            return false;

        for (GraphNode node = goal; node != null; node = _cameFrom[node])
            path.Add(node.Position);

        path.Reverse();
        path.Add(to);
        return true;
    }

    private float Estimate(GraphNode from, GraphNode goal)
    {
        return _nodes.Length > HeuristicNodeThreshold ? Vector3.Distance(from.Position, goal.Position) : 0f;
    }

    private GraphNode NearestVisibleNode(Vector3 position)
    {
        GraphNode nearest = null;
        float nearestSqrDistance = float.MaxValue;

        foreach (GraphNode node in _nodes)
        {
            float sqrDistance = FlatSqrDistance(node.Position, position);

            if (sqrDistance >= nearestSqrDistance || IsBlocked(position, node.Position))
                continue;

            nearestSqrDistance = sqrDistance;
            nearest = node;
        }

        return nearest;
    }

    private static long EdgeKey(GraphNode a, GraphNode b)
    {
        return ((long)a.GetInstanceID() << 32) | (uint)b.GetInstanceID();
    }
}
