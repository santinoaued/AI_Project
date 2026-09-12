using UnityEngine;

public class GatherState : IState
{
    private Hunter _hunter;
    private Boid _target;
    private float _gatherTime = 2f;
    private float _gatherTimer;

    public GatherState(Hunter hunter, Boid target)
    {
        _hunter = hunter;
        _target = target;
    }

    public void Enter()
    {
        _gatherTimer = 0f;
    }

    public void Update()
    {
        if (_target == null || _target.IsActive)
        {
            _hunter.ChangeState(new PatrolState(_hunter));
            return;
        }

        float distance = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        if (distance > _hunter.MeleeAttackRadius)
        {
            Vector3 steering = _hunter.GoToWaypoint(_target.transform.position);
            _hunter.MoveHunter(steering);
            return;
        }

        _gatherTimer += Time.deltaTime;

        if (_gatherTimer >= _gatherTime)
        {
            _target.Collect();
            _hunter.ChangeState(new PatrolState(_hunter));
        }
    }

    public void Exit() { }
}