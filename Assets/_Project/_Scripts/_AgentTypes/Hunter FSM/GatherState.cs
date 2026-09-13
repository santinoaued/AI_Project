using UnityEngine;

public class GatherState : HunterState
{
    private Boid _target;
    private float _gatherTimer;

    protected override string Name => "GATHER";
    protected override Color StateColor => new Color(0.3f, 1f, 0.4f);

    public GatherState(Hunter hunter) : base(hunter) { }

    public GatherState SetTarget(Boid target)
    {
        _target = target;
        return this;
    }

    public override void Enter()
    {
        base.Enter();
        _gatherTimer = 0f;
        _hunter.CurrentTarget = _target;
    }

    public override void Update()
    {
        if (_target == null || !_target.IsDead)
        {
            _hunter.SetAction("recoleccion cancelada");
            TransitionTo(_hunter.Patrol);
            return;
        }

        Vector3 targetPosition = _target.transform.position;
        float gatherDistance = _hunter.MeleeAttackRadius;

        _hunter.Move(_hunter.Arrive(targetPosition, gatherDistance * 0.7f));

        if (_hunter.FlatDistance(targetPosition) > gatherDistance)
        {
            _gatherTimer = 0f;
            return;
        }

        _gatherTimer += Time.deltaTime;
        _hunter.SetAction($"Recolectando {_target.name}");

        if (_gatherTimer >= _hunter.GatherDuration)
        {
            _target.Collect();
            _hunter.SetAction($"{_target.name} recolectado");
            TransitionTo(_hunter.Patrol);
        }
    }
}
