using UnityEngine;

public class WolfPatrolState : WolfState
{
    protected override string Name => "PATROL";
    protected override Color StateColor => new Color(0.7f, 0.7f, 0.7f);

    public WolfPatrolState(Wolf wolf) : base(wolf) { }

    public override void Update()
    {
        if (_wolf.CanSeeHunter)
        {
            TransitionTo(_wolf.Chase);
            return;
        }

        if (_wolf.ConsumeAlert())
        {
            TransitionTo(_wolf.Alerted);
            return;
        }

        _wolf.FollowPatrol();
    }
}
