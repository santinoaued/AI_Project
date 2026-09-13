using UnityEngine;

public abstract class HunterState : IState
{
    protected readonly Hunter _hunter;

    protected abstract string Name { get; }
    protected abstract Color StateColor { get; }

    protected HunterState(Hunter hunter)
    {
        _hunter = hunter;
    }

    public virtual void Enter()
    {
        _hunter.SetStateFeedback(Name, StateColor);
    }

    public abstract void Update();

    public virtual void Exit()
    {
        _hunter.CurrentTarget = null;
    }

    protected void TransitionTo(IState state)
    {
        _hunter.StateMachine.ChangeState(state);
    }
}
