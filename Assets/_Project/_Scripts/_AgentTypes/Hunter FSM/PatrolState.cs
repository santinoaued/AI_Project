using UnityEngine;

public class PatrolState : HunterState
{
    private int _currentWaypointIndex;
    private float _baitSpawnTimer;

    private bool _isPlacingBait;
    private float _placingBaitTimer;

    protected override string Name => "PATROL";
    protected override Color StateColor => new Color(0.3f, 0.5f, 1f);

    public PatrolState(Hunter hunter) : base(hunter) { }

    public override void Update()
    {
        if (_hunter.ClosestDeadBoid != null)
        {
            TransitionTo(_hunter.Gather.SetTarget(_hunter.ClosestDeadBoid));
            return;
        }

        if (_hunter.CanAttack() && _hunter.ClosestAliveBoid != null)
        {
            TransitionTo(_hunter.Attack.SetTarget(_hunter.ClosestAliveBoid));
            return;
        }

        if (_isPlacingBait)
        {
            PlaceBait();
            return;
        }

        FollowWaypoints();
        UpdateBaitTimer();
    }

    public override void Exit()
    {
        base.Exit();
        _isPlacingBait = false;
    }

    private void FollowWaypoints()
    {
        Transform[] waypoints = _hunter.Waypoints;
        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform targetWaypoint = waypoints[_currentWaypointIndex];

        if (targetWaypoint != null)
            _hunter.Move(_hunter.Arrive(targetWaypoint.position));

        if (targetWaypoint == null || _hunter.FlatDistance(targetWaypoint.position) < _hunter.WaypointReachDistance)
            _currentWaypointIndex = (_currentWaypointIndex + 1) % waypoints.Length;
    }

    private void UpdateBaitTimer()
    {
        _baitSpawnTimer += Time.deltaTime;

        if (_baitSpawnTimer < _hunter.BaitSpawnInterval)
            return;

        _baitSpawnTimer = 0f;

        if (_hunter.CanSpawnBait)
        {
            _isPlacingBait = true;
            _placingBaitTimer = 0f;
            _hunter.SetAction("colocando cebo");
        }
    }

    private void PlaceBait()
    {
        _hunter.Move(_hunter.Brake());
        _placingBaitTimer += Time.deltaTime;

        if (_placingBaitTimer >= _hunter.PlacingBaitDuration)
        {
            _isPlacingBait = false;
            _hunter.SpawnBait();
        }
    }
}
