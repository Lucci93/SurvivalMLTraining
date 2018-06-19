using UnityEngine;
using System;

public class LivingEntity : MonoBehaviour, IDamageable {

    public float startingHealth;
    protected float health;
    protected bool dead;

    public event Action OnDeath;
    public event Action<Enemy> OnDeathEnemy;

    protected virtual void Start() {
        health = startingHealth;
    }

    public virtual void TakeHit(float damage, Vector3 hitPoint, Vector3 hitDirection) {
        TakeDamage(damage);
    }

    // take damage and decrease health
    public void TakeDamage(float damage) {
        health -= damage;

        if (health <= 0 && !dead) {
            Die();
        }
    }

    // remove the entity
    protected void Die() {
        dead = true;
        if (OnDeath != null) {
            OnDeath();
            health = startingHealth;
            dead = false;
        }
        if (OnDeathEnemy != null) {
            OnDeathEnemy(this.GetComponent<Enemy>());
            Destroy(gameObject);
        }
    }
}