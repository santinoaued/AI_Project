using UnityEngine;

public class PatrolState : IState
{
    private Hunter _hunter;
    private int _currentWaypointIndex = 0;
    private float _minDistance = 0.5f;

    public PatrolState(Hunter hunter)
    {
        _hunter = hunter;
    }

    public void Enter()
    {
    }

    public void Update()
    {
        Boid inactiveBoid = _hunter.DetectInactiveBoid();
        if (inactiveBoid != null)
        {
            _hunter.ChangeState(new GatherState(_hunter, inactiveBoid));
            return;
        }

        Boid target = _hunter.DetectTarget();
        if (target != null && _hunter.CanAttack())
        {
            _hunter.ChangeState(new AttackState(_hunter, target));
            return;
        }

        Transform targetWaypoint = _hunter.Waypoints[_currentWaypointIndex];
        Vector3 steering = _hunter.GoToWaypoint(targetWaypoint.position);
        _hunter.MoveHunter(steering);

        float distance = Vector3.Distance(_hunter.transform.position, targetWaypoint.position);

        if (distance < _minDistance)
        {
            _currentWaypointIndex++;

            if (_currentWaypointIndex >= _hunter.Waypoints.Length)
            {
                _currentWaypointIndex = 0;
            }
        }
    }

    public void Exit()
    {
    }
}
