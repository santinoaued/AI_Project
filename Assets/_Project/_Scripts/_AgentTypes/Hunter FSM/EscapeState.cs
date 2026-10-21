using UnityEngine;

public class EscapeState : HunterState
{
    protected override string Name => "ESCAPE";
    protected override Color StateColor => new Color(1f, 0.9f, 0.2f);

    public EscapeState(Hunter hunter) : base(hunter) { }

    public override void Enter()
    {
        base.Enter();
        _hunter.SetEscapeBoost(true);
    }

    public override void Update()
    {
        Wolf wolf = _hunter.ClosestWolf;

        if (wolf == null)
        {
            TransitionTo(_hunter.Patrol);
            return;
        }

        _hunter.Move(_hunter.EscapeSteering(wolf));
    }

    public override void Exit()
    {
        base.Exit();
        _hunter.SetEscapeBoost(false);
    }
}
