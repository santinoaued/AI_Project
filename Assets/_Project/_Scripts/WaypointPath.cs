using UnityEngine;

public class WaypointPath
{
    private readonly Transform[] _waypoints;
    private readonly float _reachDistance;
    private int _currentIndex;

    public Transform Current => _waypoints != null && _waypoints.Length > 0 ? _waypoints[_currentIndex] : null;

    public WaypointPath(Transform[] waypoints, float reachDistance)
    {
        _waypoints = waypoints;
        _reachDistance = reachDistance;
    }

    public void Follow(Agent agent, Navigator navigator)
    {
        Transform target = Current;

        if (target != null && navigator != null)
            navigator.MoveTo(target.position, false);
        else if (target != null)
            agent.Move(agent.Arrive(target.position));

        AdvanceIfReached(agent);
    }

    public void AdvanceIfReached(Agent agent)
    {
        if (_waypoints == null || _waypoints.Length == 0)
            return;

        Transform target = _waypoints[_currentIndex];

        if (target == null || agent.FlatDistance(target.position) < _reachDistance)
            _currentIndex = (_currentIndex + 1) % _waypoints.Length;
    }
}
