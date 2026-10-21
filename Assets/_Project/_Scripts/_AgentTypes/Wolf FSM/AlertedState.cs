using UnityEngine;

public class AlertedState : WolfState
{
    protected override string Name => "ALERTED";
    protected override Color StateColor => new Color(1f, 0.6f, 0.1f);

    public AlertedState(Wolf wolf) : base(wolf) { }

    public override void Update()
    {
        if (_wolf.CanSeeHunter)
        {
            TransitionTo(_wolf.Chase);
            return;
        }

        _wolf.ConsumeAlert();

        if (_wolf.FlatDistance(_wolf.LastKnownPosition) < _wolf.ArriveDistance)
        {
            TransitionTo(_wolf.Search);
            return;
        }

        _wolf.MoveTo(_wolf.LastKnownPosition);

        if (_wolf.Navigator.HasFailedPath)
            TransitionTo(_wolf.Search);
    }
}
