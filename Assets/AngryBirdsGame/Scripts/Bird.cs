using UnityEngine;
using UnityEngine.InputSystem;

// All the bird types we have sprites for
public enum BirdType { Red, BigRed, Blue, Yellow, Black, White, Green, Orange }

// Put this on every bird prefab.
// The Slingshot launches the bird. After that the bird removes itself
// when it stops moving, flies off the map, or after a timeout.
public class Bird : MonoBehaviour
{
    public BirdType birdType = BirdType.Red;

    [Header("When is the bird finished?")]
    public float stopSpeed = 0.3f;    // slower than this counts as "not moving"
    public float maxFlyTime = 8f;     // remove the bird after this many seconds anyway

    [Header("Ability settings")]
    public GameObject eggPrefab;      // White bird only
    public float explosionRadius = 1.8f;  // Black bird only
    public float explosionForce = 6f;
    public float explosionDamage = 12f;

    [Header("Effects")]
    public GameObject poofEffect;
    public AudioClip hitSound;

    [HideInInspector] public bool isLaunched = false;
    [HideInInspector] public bool abilityUsed = false;

    Rigidbody2D rb;
    bool hasHitSomething = false;
    float launchTime;
    float stillTimer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    // Called by the Slingshot (and by the Blue bird when it splits)
    public void Launch(Vector2 velocity)
    {
        isLaunched = true;
        launchTime = Time.time;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = velocity;
    }

    void Update()
    {
        if (!isLaunched) return;

        // Click while flying to use the special ability (only once, before hitting anything)
        if (!abilityUsed && !hasHitSomething && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            UseAbility();
        }

        // Count how long the bird has been lying still
        if (rb.linearVelocity.magnitude < stopSpeed)
            stillTimer += Time.deltaTime;
        else
            stillTimer = 0f;

        bool stoppedMoving = stillTimer > 1f;
        bool flewTooLong = Time.time - launchTime > maxFlyTime;
        bool leftTheMap = transform.position.y < -10f || Mathf.Abs(transform.position.x) > 14f;

        if (stoppedMoving || flewTooLong || leftTheMap)
        {
            RemoveBird();
        }
    }

    void UseAbility()
    {
        abilityUsed = true;

        if (birdType == BirdType.Yellow)
        {
            // Speed boost
            rb.linearVelocity = rb.linearVelocity * 2f;
        }
        else if (birdType == BirdType.Blue)
        {
            // Split into three birds
            SpawnCopy(10f);
            SpawnCopy(-10f);
        }
        else if (birdType == BirdType.Black)
        {
            Explode();
        }
        else if (birdType == BirdType.White)
        {
            // Drop an egg straight down and shoot upwards
            if (eggPrefab != null)
            {
                GameObject egg = Instantiate(eggPrefab, transform.position + Vector3.down * 0.4f, Quaternion.identity);
                egg.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(0f, -12f);
                Destroy(egg, 4f);
            }
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 1.5f, 8f);
        }
        else if (birdType == BirdType.Green)
        {
            // Boomerang: fly back the other way
            rb.linearVelocity = new Vector2(-rb.linearVelocity.x, rb.linearVelocity.y);
        }
        else if (birdType == BirdType.Orange)
        {
            // Inflate like a balloon
            transform.localScale = transform.localScale * 2.2f;
        }
        // Red and BigRed have no ability
    }

    void SpawnCopy(float angle)
    {
        // Spawn the copy a little above or below so the birds don't overlap
        Vector3 offset = new Vector3(0f, angle > 0f ? 0.35f : -0.35f, 0f);
        GameObject copyObject = Instantiate(gameObject, transform.position + offset, transform.rotation);
        Bird copy = copyObject.GetComponent<Bird>();
        copy.abilityUsed = true;
        // Rotate the velocity a little so the copies spread out
        Vector2 newVelocity = Quaternion.Euler(0f, 0f, angle) * rb.linearVelocity;
        copy.Launch(newVelocity);
    }

    void Explode()
    {
        // Push and damage everything close to the bird
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D otherBody = hit.attachedRigidbody;
            if (otherBody != null && otherBody != rb)
            {
                Vector2 direction = (otherBody.position - rb.position).normalized;
                otherBody.AddForce(direction * explosionForce, ForceMode2D.Impulse);
            }

            Pig pig = hit.GetComponent<Pig>();
            if (pig != null) pig.TakeDamage(explosionDamage);

            Block block = hit.GetComponent<Block>();
            if (block != null) block.TakeDamage(explosionDamage);
        }

        RemoveBird();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isLaunched) return;

        if (!hasHitSomething && hitSound != null && collision.relativeVelocity.magnitude > 2f)
        {
            AudioSource.PlayClipAtPoint(hitSound, Camera.main.transform.position);
        }
        hasHitSomething = true;

        // The Black bird explodes by itself a moment after it hits something
        if (birdType == BirdType.Black && !abilityUsed)
        {
            abilityUsed = true;
            Invoke(nameof(Explode), 1.5f);
        }
    }

    void RemoveBird()
    {
        if (poofEffect != null)
        {
            GameObject effect = Instantiate(poofEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }
        Destroy(gameObject);
    }
}
