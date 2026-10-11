using UnityEngine;

// Put this on every wood / stone / ice block.
// The Rigidbody2D does the falling and toppling,
// this script only breaks the block after enough hard hits.
public class Block : MonoBehaviour
{
    public float health = 8f;            // ice is weak, stone is strong
    public float minImpactSpeed = 2f;    // softer hits than this do nothing

    public GameObject breakEffect;
    public AudioClip hitSound;
    public AudioClip breakSound;

    bool isBroken = false;

    void Update()
    {
        // Block fell or rolled out of the level
        if (transform.position.y < -10f || Mathf.Abs(transform.position.x) > 14f)
        {
            Destroy(gameObject);
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
        if (isBroken) return;

        health -= damage;

        if (health <= 0f)
        {
            Break();
        }
        else if (hitSound != null)
        {
            AudioSource.PlayClipAtPoint(hitSound, Camera.main.transform.position, 0.5f);
        }
    }

    void Break()
    {
        isBroken = true;
        WakeUpNearbyObjects();

        if (breakEffect != null)
        {
            GameObject effect = Instantiate(breakEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, Camera.main.transform.position);
        }
        Destroy(gameObject);
    }

    // Objects that stopped moving go to "sleep" and don't notice when the block
    // under them is gone, so they float. Wake up everything near this block.
    void WakeUpNearbyObjects()
    {
        // Half the size of the block, plus a little extra
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
