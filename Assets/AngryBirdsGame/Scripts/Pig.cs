using UnityEngine;

// Put this on every pig.
// Pigs lose health when something hits them hard enough.
public class Pig : MonoBehaviour
{
    public float health = 5f;
    public float minImpactSpeed = 2.5f;   // softer hits than this do nothing

    public GameObject deathEffect;
    public AudioClip hitSound;
    public AudioClip deathSound;

    LevelManager levelManager;
    bool isDead = false;

    void Start()
    {
        levelManager = FindFirstObjectByType<LevelManager>();
    }

    void Update()
    {
        // Pig fell or rolled out of the level
        if (transform.position.y < -10f || Mathf.Abs(transform.position.x) > 14f)
        {
            Die();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Ignore small bumps while the level is settling at the start
        if (Time.timeSinceLevelLoad < 1f) return;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactSpeed) return;

        TakeDamage(impactSpeed);
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        health -= damage;

        if (health <= 0f)
        {
            Die();
        }
        else if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, Camera.main.transform.position);
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;
        WakeUpNearbyObjects();

        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, Camera.main.transform.position);
        }

        if (levelManager != null)
        {
            levelManager.PigDestroyed();
        }
        Destroy(gameObject);
    }

    // Same as in Block: wake up anything resting on this pig so it falls
    void WakeUpNearbyObjects()
    {
        float radius = GetComponent<Collider2D>().bounds.extents.magnitude + 0.5f;

        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, radius);
        foreach (Collider2D col in nearby)
        {
            if (col.attachedRigidbody != null)
            {
                col.attachedRigidbody.WakeUp();
            }
        }
    }
}
