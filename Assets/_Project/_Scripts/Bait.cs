using UnityEngine;

public class Bait : MonoBehaviour
{
    [SerializeField] private float _health = 100f;

    public void TakeDamage(float damage)
    {
        _health -= damage;

        if (_health <= 0)
        {
            Destroy(gameObject);
        }
    }

}
