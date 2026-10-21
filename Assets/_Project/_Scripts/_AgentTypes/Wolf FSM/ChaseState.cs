using UnityEngine;

public class ChaseState : WolfState
{
    private float _lostSightTime;

    protected override string Name => "CHASE";
    protected override Color StateColor => new Color(1f, 0.2f, 0.2f);

    public ChaseState(Wolf wolf) : base(wolf) { }

    public override void Enter()
    {
        base.Enter();
        _lostSightTime = 0f;
        _wolf.Howl();
    }

    public override void Update()
    {
        if (_wolf.CanSeeHunter)
        {
            _lostSightTime = 0f;

            if (_wolf.CanBite)
                _wolf.Bite();

            _wolf.PursueHunter();
            return;
        }

        _lostSightTime += Time.deltaTime;

        if (_lostSightTime >= _wolf.LoseSightDelay)
        {
            TransitionTo(_wolf.Alerted);
            return;
        }

        _wolf.MoveTo(_wolf.LastKnownPosition);
    }
}
