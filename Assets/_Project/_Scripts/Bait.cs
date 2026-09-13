using UnityEngine;

public class Bait : MonoBehaviour
{
    public static int ActiveCount { get; private set; }

    [SerializeField] private float _health = 100f;

    private void OnEnable() => ActiveCount++;
    private void OnDisable() => ActiveCount--;

    public void TakeDamage(float damage)
    {
        if (_health <= 0f)
            return;

        _health -= damage;

        if (_health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
