using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// One per level scene.
// Counts pigs, shows the HUD, decides win / lose and handles the buttons.
public class LevelManager : MonoBehaviour
{
    public const int TotalLevels = 10;

    public int levelNumber = 1;
    public Slingshot slingshot;

    [Header("UI")]
    public TMP_Text levelText;
    public TMP_Text birdsText;
    public GameObject levelCompletePanel;
    public GameObject gameOverPanel;
    public GameObject nextLevelButton;

    [Header("Stars")]
    public Image[] stars;   // the 3 stars on the Level Complete panel
    public Color emptyStarColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);

    [Header("Sounds")]
    public AudioClip levelCompleteSound;

    int pigsLeft;
    bool gameOver = false;

    void Start()
    {
        pigsLeft = FindObjectsByType<Pig>(FindObjectsSortMode.None).Length;

        levelCompletePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        levelText.text = "Level " + levelNumber;
    }

    void Update()
    {
        birdsText.text = "Birds: " + slingshot.birdsLeft;
    }

    public bool IsGameOver()
    {
        return gameOver;
    }

    // Called by a Pig when it dies
    public void PigDestroyed()
    {
        pigsLeft--;
        if (pigsLeft <= 0)
        {
            // Small delay so the player can see the last pig pop
            Invoke(nameof(WinLevel), 1f);
        }
    }

    // Called by the Slingshot when the last bird is finished
    public void OutOfBirds()
    {
        // Give falling blocks a moment to finish off pigs
        Invoke(nameof(CheckForLose), 2f);
    }

    void CheckForLose()
    {
        if (gameOver) return;

        if (pigsLeft > 0)
        {
            gameOver = true;
            gameOverPanel.SetActive(true);
        }
    }

    void WinLevel()
    {
        if (gameOver) return;
        gameOver = true;

        // Unlock the next level (saved with PlayerPrefs)
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
        if (levelNumber + 1 > unlockedLevel)
        {
            PlayerPrefs.SetInt("UnlockedLevel", levelNumber + 1);
            PlayerPrefs.Save();
        }

        // Stars: the more birds left over, the more stars
        int starsEarned = CalculateStars();
        string starsKey = "Stars_Level" + levelNumber;
        if (starsEarned > PlayerPrefs.GetInt(starsKey, 0))
        {
            PlayerPrefs.SetInt(starsKey, starsEarned);
            PlayerPrefs.Save();
        }

        levelCompletePanel.SetActive(true);
        // There is no next level after the last one
        nextLevelButton.SetActive(levelNumber < TotalLevels);
        StartCoroutine(ShowStars(starsEarned));

        if (levelCompleteSound != null)
        {
            AudioSource.PlayClipAtPoint(levelCompleteSound, Camera.main.transform.position);
        }
    }

    // 2 or more birds left = 3 stars, 1 left = 2 stars, won with the last bird = 1 star
    int CalculateStars()
    {
        if (slingshot.birdsLeft >= 2) return 3;
        if (slingshot.birdsLeft == 1) return 2;
        return 1;
    }

    // Shows the stars one by one with a little "pop"
    IEnumerator ShowStars(int starsEarned)
    {
        foreach (Image star in stars)
        {
            star.color = emptyStarColor;
        }

        for (int i = 0; i < starsEarned; i++)
        {
            yield return new WaitForSeconds(0.4f);

            // Light up the star and make it big for a moment
            stars[i].color = Color.white;
            stars[i].transform.localScale = new Vector3(1.4f, 1.4f, 1f);

            yield return new WaitForSeconds(0.15f);
            stars[i].transform.localScale = Vector3.one;
        }
    }

    // ----- Buttons -----

    public void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void NextLevel()
    {
        SceneManager.LoadScene("Level" + (levelNumber + 1));
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
