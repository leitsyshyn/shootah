using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject aboutPanel;

    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button aboutButton;
    [SerializeField] private Button backButton;
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
        SetPanelState(showMainMenu: false);
    }

    public void ShowMainMenu()
    {
        SetPanelState(showMainMenu: true);
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
        AddButtonListener(aboutButton, ShowAbout);
        AddButtonListener(backButton, ShowMainMenu);
        AddButtonListener(exitButton, Exit);
    }

    private void UnbindButtons()
    {
        RemoveButtonListener(playButton, Play);
        RemoveButtonListener(aboutButton, ShowAbout);
        RemoveButtonListener(backButton, ShowMainMenu);
        RemoveButtonListener(exitButton, Exit);
    }

    private void SetPanelState(bool showMainMenu)
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(showMainMenu);
        }

        if (aboutPanel != null)
        {
            aboutPanel.SetActive(!showMainMenu);
        }
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
