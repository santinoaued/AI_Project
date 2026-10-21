using UnityEngine;

public class PatrolState : HunterState
{
    private readonly WaypointPath _path;
    private float _baitSpawnTimer;

    private bool _isPlacingBait;
    private float _placingBaitTimer;

    protected override string Name => "PATROL";
    protected override Color StateColor => new Color(0.3f, 0.5f, 1f);

    public PatrolState(Hunter hunter) : base(hunter)
    {
        _path = new WaypointPath(hunter.Waypoints, hunter.WaypointReachDistance);
    }

    public override void Update()
    {
        if (_hunter.ClosestWolf != null)
        {
            TransitionTo(_hunter.Escape);
            return;
        }

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

        _path.Follow(_hunter, _hunter.Navigator);
        UpdateBaitTimer();
    }

    public override void Exit()
    {
        base.Exit();
        _isPlacingBait = false;
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
