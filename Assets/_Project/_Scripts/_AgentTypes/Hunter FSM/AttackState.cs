using UnityEngine;

public class AttackState : HunterState
{
    private Boid _target;

    protected override string Name => "ATTACK";
    protected override Color StateColor => new Color(1f, 0.25f, 0.25f);

    public AttackState(Hunter hunter) : base(hunter) { }

    public AttackState SetTarget(Boid target)
    {
        _target = target;
        return this;
    }

    public override void Enter()
    {
        base.Enter();
        _hunter.CurrentTarget = _target;
    }

    public override void Update()
    {
        if (_target == null || !_target.IsActive)
        {
            TransitionTo(_hunter.Patrol);
            return;
        }

        float distance = _hunter.FlatDistance(_target.transform.position);

        if (distance > _hunter.PerceptionRadius)
        {
            _hunter.SetAction($"{_target.name} escapo");
            TransitionTo(_hunter.Patrol);
            return;
        }

        if (distance <= _hunter.MeleeAttackRadius)
        {
            _hunter.MeleeAttack(_target);
            FinishAttack();
        }
        else if (distance <= _hunter.RangeAttackRadius)
        {
            _hunter.RangeAttack(_target);
            FinishAttack();
        }
        else
        {
            _hunter.Move(_hunter.Pursuit(_target));
        }
    }

    private void FinishAttack()
    {
        _hunter.ResetAttackTimer();
        TransitionTo(_hunter.Patrol);
    }
}
