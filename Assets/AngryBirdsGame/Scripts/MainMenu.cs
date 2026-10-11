using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Main menu with a Play button and a level select screen.
// Levels are unlocked one by one (saved with PlayerPrefs).
public class MainMenu : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject levelSelectPanel;

    // One button per level (10 in total). Each button has a "Number", a "Lock" and a "Stars" child.
    public Button[] levelButtons;
    public Color emptyStarColor = new Color(0.15f, 0.15f, 0.15f, 0.6f);

    void Start()
    {
        ShowMainPanel();
    }

    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        levelSelectPanel.SetActive(false);
    }

    public void ShowLevelSelect()
    {
        mainPanel.SetActive(false);
        levelSelectPanel.SetActive(true);
        RefreshLevelButtons();
    }

    void RefreshLevelButtons()
    {
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);

        for (int i = 0; i < levelButtons.Length; i++)
        {
            int levelNumber = i + 1;
            bool isUnlocked = levelNumber <= unlockedLevel;

            levelButtons[i].interactable = isUnlocked;
            levelButtons[i].transform.Find("Number").gameObject.SetActive(isUnlocked);
            levelButtons[i].transform.Find("Lock").gameObject.SetActive(!isUnlocked);

            // Show the best stars for levels that have been beaten
            int bestStars = PlayerPrefs.GetInt("Stars_Level" + levelNumber, 0);
            Transform starsParent = levelButtons[i].transform.Find("Stars");
            starsParent.gameObject.SetActive(bestStars > 0);

            // The 3 stars are children of "Stars"
            for (int j = 0; j < 3; j++)
            {
                Image star = starsParent.GetChild(j).GetComponent<Image>();
                if (j < bestStars)
                {
                    star.color = Color.white;
                }
                else
                {
                    star.color = emptyStarColor;
                }
            }
        }
    }

    public void LoadLevel(int levelNumber)
    {
        SceneManager.LoadScene("Level" + levelNumber);
    }
}
