using UnityEngine;

public abstract class WolfState : IState
{
    protected readonly Wolf _wolf;

    protected abstract string Name { get; }
    protected abstract Color StateColor { get; }

    protected WolfState(Wolf wolf)
    {
        _wolf = wolf;
    }

    public virtual void Enter()
    {
        _wolf.SetStateFeedback(Name, StateColor);
    }

    public abstract void Update();

    public virtual void Exit() { }

    protected void TransitionTo(IState state)
    {
        _wolf.StateMachine.ChangeState(state);
    }
}
