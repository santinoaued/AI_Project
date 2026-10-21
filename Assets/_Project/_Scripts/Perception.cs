using UnityEngine;

public class Perception
{
    private readonly Transform _eye;
    private readonly float _viewDistance;
    private readonly float _halfAngle;
    private readonly LayerMask _obstacleMask;

    public Perception(Transform eye, float viewDistance, float viewAngle, LayerMask obstacleMask)
    {
        _eye = eye;
        _viewDistance = viewDistance;
        _halfAngle = viewAngle * 0.5f;
        _obstacleMask = obstacleMask;
    }

    public bool CanSee(Transform target)
    {
        Vector3 toTarget = target.position - _eye.position;
        Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);

        if (flatToTarget.sqrMagnitude > _viewDistance * _viewDistance)
            return false;

        if (Vector3.Angle(_eye.forward, flatToTarget) > _halfAngle)
            return false;

        return !Physics.Linecast(_eye.position, target.position, _obstacleMask);
    }
}
