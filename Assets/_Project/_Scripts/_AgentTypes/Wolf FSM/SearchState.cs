using UnityEngine;

public class SearchState : WolfState
{
    private float _elapsed;
    private Vector3 _searchPoint;

    protected override string Name => "SEARCH";
    protected override Color StateColor => new Color(0.9f, 0.9f, 0.2f);

    public SearchState(Wolf wolf) : base(wolf) { }

    public override void Enter()
    {
        base.Enter();
        _elapsed = 0f;
        PickSearchPoint();
    }

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

        _elapsed += Time.deltaTime;

        if (_elapsed >= _wolf.SearchDuration)
        {
            TransitionTo(_wolf.Patrol);
            return;
        }

        if (_wolf.FlatDistance(_searchPoint) < _wolf.ArriveDistance)
            PickSearchPoint();

        _wolf.MoveTo(_searchPoint);
    }

    private void PickSearchPoint()
    {
        _searchPoint = _wolf.RandomSearchPoint();
    }
}
