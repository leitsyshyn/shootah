using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject aboutPanel;
    [SerializeField] private GameObject progressionPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button progressionButton;
    [SerializeField] private Button aboutButton;
    [SerializeField] private Button aboutBackButton;
    [SerializeField] private Button progressionBackButton;
    [SerializeField] private Button exitButton;

    [Header("Navigation")]
    [SerializeField] private string gameplaySceneName = "SurvivalArena";

    private void Awake()
    {
        BindButtons();
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void Play()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ShowAbout()
    {
        SetPanelState(MenuScreen.About);
    }

    public void ShowProgression()
    {
        SetPanelState(MenuScreen.Progression);
    }

    public void ShowMainMenu()
    {
        SetPanelState(MenuScreen.MainMenu);
    }

    public void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void BindButtons()
    {
        AddButtonListener(playButton, Play);
        AddButtonListener(progressionButton, ShowProgression);
        AddButtonListener(aboutButton, ShowAbout);
        AddButtonListener(aboutBackButton, ShowMainMenu);
        AddButtonListener(progressionBackButton, ShowMainMenu);
        AddButtonListener(exitButton, Exit);
    }

    private void UnbindButtons()
    {
        RemoveButtonListener(playButton, Play);
        RemoveButtonListener(progressionButton, ShowProgression);
        RemoveButtonListener(aboutButton, ShowAbout);
        RemoveButtonListener(aboutBackButton, ShowMainMenu);
        RemoveButtonListener(progressionBackButton, ShowMainMenu);
        RemoveButtonListener(exitButton, Exit);
    }

    private void SetPanelState(MenuScreen screen)
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(screen == MenuScreen.MainMenu);
        }

        if (aboutPanel != null)
        {
            aboutPanel.SetActive(screen == MenuScreen.About);
        }

        if (progressionPanel != null)
        {
            progressionPanel.SetActive(screen == MenuScreen.Progression);
        }
    }

    private enum MenuScreen
    {
        MainMenu = 0,
        About = 1,
        Progression = 2
    }

    private static void AddButtonListener(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(callback);
        button.onClick.AddListener(callback);
    }

    private static void RemoveButtonListener(Button button, UnityEngine.Events.UnityAction callback)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(callback);
    }
}
