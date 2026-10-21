using UnityEngine;

public class Perception
{
    private readonly Transform _eye;
    private readonly float _viewDistance;
    private readonly float _cosHalfAngle;
    private readonly LayerMask _obstacleMask;

    public Perception(Transform eye, float viewDistance, float viewAngle, LayerMask obstacleMask)
    {
        _eye = eye;
        _viewDistance = viewDistance;
        _cosHalfAngle = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);
        _obstacleMask = obstacleMask;
    }

    public bool CanSee(Transform target)
    {
        Vector3 toTarget = target.position - _eye.position;
        Vector3 flatToTarget = new Vector3(toTarget.x, 0f, toTarget.z);
        float sqrDistance = flatToTarget.sqrMagnitude;

        if (sqrDistance > _viewDistance * _viewDistance)
            return false;

        if (sqrDistance > 0.0001f && Vector3.Dot(_eye.forward, flatToTarget) < _cosHalfAngle * Mathf.Sqrt(sqrDistance))
            return false;

        return !Physics.Linecast(_eye.position, target.position, _obstacleMask);
    }
}
