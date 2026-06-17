using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    [Header("Arma encontrada")]
    [SerializeField] private GameObject foundWeaponPanel;
    [SerializeField] private TMP_Text foundWeaponText;
    [SerializeField] private Image fadeImage;
    [SerializeField] private string foundWeaponMessage = "Você encontrou a arma!";
    [SerializeField] private float foundMessageDuration = 2f;
    [SerializeField] private float fadeDuration = 1f;

    private bool isPaused = false;
    private bool gameEnded = false;
    private bool isChangingScene = false;

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
        SetFoundWeaponUI(false);
        SetFadeAlpha(0f);
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

    public void StartFoundWeaponSequence(string nextSceneName)
    {
        if (isChangingScene) return;

        StartCoroutine(FoundWeaponSequence(nextSceneName));
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

    private IEnumerator FoundWeaponSequence(string nextSceneName)
    {
        isChangingScene = true;
        isPaused = true;
        Time.timeScale = 0f;

        SetPauseButtons(false);
        SetFoundWeaponUI(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return new WaitForSecondsRealtime(foundMessageDuration);

        if (fadeImage != null)
        {
            float elapsedTime = 0f;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Clamp01(elapsedTime / fadeDuration));

                yield return null;
            }
        }

        isPaused = false;
        gameEnded = false;
        Time.timeScale = 1f;

        SceneManager.LoadScene(nextSceneName);
    }

    private void SetFoundWeaponUI(bool active)
    {
        if (foundWeaponPanel != null)
            foundWeaponPanel.SetActive(active);

        if (foundWeaponText != null)
            foundWeaponText.text = foundWeaponMessage;
    }

    private void SetFadeAlpha(float alpha)
    {
        if (fadeImage == null) return;

        Color color = fadeImage.color;
        color.a = alpha;
        fadeImage.color = color;

        fadeImage.gameObject.SetActive(alpha > 0f);
    }
}