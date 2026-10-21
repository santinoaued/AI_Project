using UnityEngine;

public class DeadState : HunterState
{
    private float _respawnTimer;

    protected override string Name => "DEAD";
    protected override Color StateColor => new Color(0.35f, 0.35f, 0.35f);

    public DeadState(Hunter hunter) : base(hunter) { }

    public override void Enter()
    {
        base.Enter();
        _respawnTimer = 0f;
        _hunter.SetBodyActive(false);
    }

    public override void Update()
    {
        _respawnTimer += Time.deltaTime;

        if (_respawnTimer >= _hunter.RespawnDelay)
            _hunter.Respawn();
    }

    public override void Exit()
    {
        base.Exit();
        _hunter.SetBodyActive(true);
    }
}
