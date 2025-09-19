using UnityEngine;

public interface IDamageable
{
    void TakeDamage(int damage);
    bool IsDead();
    GameObject GetGameObject(); // Useful for getting the GameObject reference
}
