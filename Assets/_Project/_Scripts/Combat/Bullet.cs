using UnityEngine;

public class Bullet : Agent
{
    [SerializeField] private float _lifeTime = 3f;
    [SerializeField] private float _hitDistance = 0.8f;

    private Hunter _owner;
    private Boid _target;
    private float _damage;
    private float _lifeTimer;

    protected override bool WrapsAroundBounds => false;

    public void Launch(Hunter owner, Boid target, float damage)
    {
        _owner = owner;
        _target = target;
        _damage = damage;
        _lifeTimer = 0f;
        _currentVelocity = DesiredVector(target.transform.position, _maxSpeed);
    }

    private void Update()
    {
        _lifeTimer += Time.deltaTime;

        if (_lifeTimer >= _lifeTime)
        {
            Release();
            return;
        }

        if (_target == null || !_target.IsActive)
        {
            Move(Vector3.zero);
            return;
        }

        Move(Pursuit(_target));

        if (FlatDistance(_target.transform.position) <= _hitDistance)
        {
            _target.TakeDamage(_damage);
            Release();
        }
    }

    private void Release()
    {
        _target = null;

        if (_owner != null)
            _owner.ReleaseBullet(this);
        else
            Destroy(gameObject);
    }
}
