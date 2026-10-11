using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Holds the birds for this level, lets the player drag and launch them,
// and loads the next bird when the current one is finished.
public class Slingshot : MonoBehaviour
{
    [Header("Birds for this level (in order)")]
    public Bird[] birds;

    [Header("Slingshot parts")]
    public Transform restPoint;        // where the bird sits before launch
    public Transform pouch;            // the leather pouch sprite
    public LineRenderer frontBand;
    public LineRenderer backBand;
    public Transform frontBandStart;
    public Transform backBandStart;
    public LineRenderer aimLine;

    [Header("Launch settings")]
    public float maxDragDistance = 1.5f;
    public float launchPower = 10f;    // launch speed = drag distance * launchPower
    public float grabDistance = 1.5f;  // how close to the bird you must click

    [Header("Sounds")]
    public AudioClip stretchSound;
    public AudioClip launchSound;

    public LevelManager levelManager;

    [HideInInspector] public int birdsLeft;   // birds that have not been launched yet

    int nextBirdIndex = 0;
    Bird currentBird;
    bool birdIsReady = false;    // a bird is sitting in the slingshot
    bool isDragging = false;
    bool waitingForBird = false; // a launched bird is still flying around
    Camera cam;

    void Start()
    {
        cam = Camera.main;
        birdsLeft = birds.Length;
        aimLine.positionCount = 0;
        LoadNextBird();
    }

    void Update()
    {
        // Wait until the launched bird has removed itself
        if (waitingForBird && currentBird == null)
        {
            waitingForBird = false;
            if (nextBirdIndex < birds.Length)
            {
                LoadNextBird();
            }
            else
            {
                levelManager.OutOfBirds();
            }
        }

        if (birdIsReady && !levelManager.IsGameOver())
        {
            HandleMouse();
        }

        UpdateBands();
    }

    void LoadNextBird()
    {
        if (levelManager.IsGameOver()) return;

        currentBird = Instantiate(birds[nextBirdIndex], restPoint.position, Quaternion.identity);
        nextBirdIndex++;

        // The bird should not fall until we launch it
        Rigidbody2D rb = currentBird.GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        birdIsReady = true;
    }

    void HandleMouse()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePosition = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Vector2 restPosition = restPoint.position;

        // Start dragging when the player clicks near the bird (but not on a UI button)
        if (mouse.leftButton.wasPressedThisFrame && !isDragging)
        {
            bool clickedOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (!clickedOnUI && Vector2.Distance(mousePosition, restPosition) < grabDistance)
            {
                isDragging = true;
                PlaySound(stretchSound);
            }
        }

        if (!isDragging) return;

        // Move the bird with the mouse, but not further than maxDragDistance
        Vector2 dragOffset = Vector2.ClampMagnitude(mousePosition - restPosition, maxDragDistance);
        currentBird.transform.position = restPosition + dragOffset;

        // Launch in the opposite direction of the drag
        Vector2 launchVelocity = -dragOffset * launchPower;
        DrawAimLine(currentBird.transform.position, launchVelocity);

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
            aimLine.positionCount = 0;

            if (dragOffset.magnitude < 0.2f)
            {
                // Tiny drag: just put the bird back
                currentBird.transform.position = restPosition;
            }
            else
            {
                LaunchCurrentBird(launchVelocity);
            }
        }
    }

    // Shoots the bird that is sitting in the slingshot
    public void LaunchCurrentBird(Vector2 velocity)
    {
        if (!birdIsReady) return;

        currentBird.Launch(velocity);
        birdIsReady = false;
        waitingForBird = true;
        birdsLeft--;
        PlaySound(launchSound);
    }

    // Simple aiming line: a few points of the flight path (start + velocity * t + gravity * t * t / 2)
    void DrawAimLine(Vector2 start, Vector2 velocity)
    {
        int pointCount = 15;
        aimLine.positionCount = pointCount;
        for (int i = 0; i < pointCount; i++)
        {
            float t = i * 0.06f;
            Vector2 point = start + velocity * t + 0.5f * Physics2D.gravity * t * t;
            aimLine.SetPosition(i, point);
        }
    }

    // Stretch the rubber bands to the bird (or to the rest point when empty)
    void UpdateBands()
    {
        Vector3 bandEnd = restPoint.position;
        if (birdIsReady && currentBird != null)
        {
            bandEnd = currentBird.transform.position;
        }

        frontBand.SetPosition(0, frontBandStart.position);
        frontBand.SetPosition(1, bandEnd);
        backBand.SetPosition(0, backBandStart.position);
        backBand.SetPosition(1, bandEnd);
        pouch.position = bandEnd;
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, cam.transform.position);
        }
    }
}
