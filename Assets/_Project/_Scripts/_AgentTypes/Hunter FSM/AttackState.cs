using UnityEngine;
public class AttackState : IState
{
    private Hunter _hunter;
    private Boid _target;

    public AttackState(Hunter hunter, Boid target)
    {
        _hunter = hunter;
        _target = target;
    }

    public void Enter() { }

    public void Update()
    {
        if (_target == null)
        {
            _hunter.ChangeState(new PatrolState(_hunter));
            return;
        }

        float distance = Vector3.Distance(_hunter.transform.position, _target.transform.position);

        if (distance > _hunter.PerceptionRadius)
        {
            _hunter.ChangeState(new PatrolState(_hunter));
            return;
        }

        if (distance <= _hunter.MeleeAttackRadius || distance <= _hunter.RangeAttackRadius)
        {
            _target.TakeDamage(_hunter.AttackDamage);
            _hunter.ResetAttackTimer();
            _hunter.ChangeState(new PatrolState(_hunter));
        }
        else
        {
            Vector3 steering = _hunter.PursuitTarget(_target.transform.position, _target.Velocity);
            _hunter.MoveHunter(steering);
        }
    }

    public void Exit() { }
}
