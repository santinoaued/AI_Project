using System;
using System.Collections.Generic;
using UnityEngine;

public class Navigator
{
    private readonly Agent _agent;
    private readonly Pathfinder _pathfinder;
    private readonly float _nodeReachDistance;
    private readonly float _repathDistance;
    private readonly List<Vector3> _path = new List<Vector3>();

    private Vector3 _goal;
    private bool _hasGoal;
    private bool _hasPath;
    private int _index;
    private int _lastMoveFrame;
    private bool _stopAtTarget;

    public event Action<Vector3, bool> PathComputed;

    public bool HasFailedPath => _hasGoal && !_hasPath;
    public bool IsUsingPathfinding => _hasPath && Time.frameCount - _lastMoveFrame <= 1;
    public IReadOnlyList<Vector3> Path => _path;
    public int Index => _index;

    public Navigator(Agent agent, Pathfinder pathfinder, float nodeReachDistance, float repathDistance)
    {
        _agent = agent;
        _pathfinder = pathfinder;
        _nodeReachDistance = nodeReachDistance;
        _repathDistance = repathDistance;
    }

    public void MoveTo(Vector3 target, bool stopAtTarget = true)
    {
        _stopAtTarget = stopAtTarget;

        if (Time.frameCount - _lastMoveFrame > 1)
            _hasGoal = false;

        _lastMoveFrame = Time.frameCount;

        if (!_pathfinder.IsBlocked(_agent.transform.position, target))
        {
            _hasGoal = false;
            _hasPath = false;
            _agent.Move(_stopAtTarget ? _agent.Arrive(target) : _agent.Seek(target));
            return;
        }

        if (!_hasGoal || Pathfinder.FlatSqrDistance(_goal, target) > _repathDistance * _repathDistance)
            ComputePath(target);

        if (!_hasPath)
        {
            _agent.Move(_agent.Brake());
            return;
        }

        Follow();
    }

    private void ComputePath(Vector3 target)
    {
        _hasPath = _pathfinder.TryFindPath(_agent.transform.position, target, _path);
        _hasGoal = true;
        _goal = target;
        _index = 0;
        PathComputed?.Invoke(target, _hasPath);
    }

    private void Follow()
    {
        Vector3 position = _agent.transform.position;
        int last = _path.Count - 1;

        while (_index < last && !_pathfinder.IsBlocked(position, _path[_index + 1]))
            _index++;

        if (_index < last && _agent.FlatDistance(_path[_index]) < _nodeReachDistance)
            _index++;

        Vector3 point = _path[_index];
        _agent.Move(_index == last && _stopAtTarget ? _agent.Arrive(point) : _agent.Seek(point));
    }
}
