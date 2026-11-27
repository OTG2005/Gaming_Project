using UnityEngine;

/// <summary>Anything Erik's axe can hit (enemies, the training dummy).</summary>
public interface IDamageable
{
    void TakeDamage(int amount, Vector2 fromPosition);
}
