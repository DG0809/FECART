using UnityEngine;
using UnityEngine.SceneManagement;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;

    [Header("Cenas")]
    [SerializeField] private string menuSceneName = "Menu";
    [SerializeField] private string victorySceneName = "Victory";
    [SerializeField] private string defeatSceneName = "Defeat";

    [Header("Botões do Pause")]
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject menuButton;

    private bool isPaused = false;
    private bool gameEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetPauseButtons(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (gameEnded) return;

        isPaused = !isPaused;

        Time.timeScale = isPaused ? 0f : 1f;
        SetPauseButtons(isPaused);

        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        SetPauseButtons(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToMenu()
    {
        isPaused = false;
        gameEnded = false;
        Time.timeScale = 1f;

        SetPauseButtons(false);

        SceneManager.LoadScene(menuSceneName);
    }

    public void GoToScene(string sceneName)
    {
        isPaused = false;
        gameEnded = false;
        Time.timeScale = 1f;

        SetPauseButtons(false);

        SceneManager.LoadScene(sceneName);
    }

    public void GoToVictory()
    {
        if (gameEnded) return;

        gameEnded = true;
        Time.timeScale = 1f;

        SetPauseButtons(false);

        SceneManager.LoadScene(victorySceneName);
    }

    public void GoToDefeat()
    {
        if (gameEnded) return;

        gameEnded = true;
        Time.timeScale = 1f;

        SetPauseButtons(false);

        SceneManager.LoadScene(defeatSceneName);
    }

    public void RestartGame()
    {
        isPaused = false;
        gameEnded = false;
        Time.timeScale = 1f;

        SetPauseButtons(false);

        SceneManager.LoadScene(0);
    }

    public bool IsPaused()
    {
        return isPaused;
    }

    private void SetPauseButtons(bool active)
    {
        if (continueButton != null)
            continueButton.SetActive(active);

        if (menuButton != null)
            menuButton.SetActive(active);
    }
}