using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class PrototypeHud : MonoBehaviour
{
    [SerializeField] private Text hpText;
    [SerializeField] private Text ammoText;
    [SerializeField] private Text reloadText;
    [SerializeField] private Text pointsText;
    [SerializeField] private Text progressionText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text upgradeText;
    [SerializeField] private Text stateText;
    [SerializeField] private Button pauseButton;
    [SerializeField] private GameObject pauseOverlay;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseMainMenuButton;

    private PlayerHealth playerHealth;
    private PlayerWeapon playerWeapon;
    private SurvivalArenaGame game;
    private PlayerController2D playerController;
    private bool isPaused;

    public void BindSession(PlayerHealth health, PlayerWeapon weapon, SurvivalArenaGame owner)
    {
        Unsubscribe();
        UnbindButtons();

        playerHealth = health;
        playerWeapon = weapon;
        game = owner;
        playerController = playerHealth != null ? playerHealth.GetComponent<PlayerController2D>() : null;

        BindButtons();
        Subscribe();
        Refresh();
        SetPaused(false);
    }

    private void Subscribe()
    {
        if (playerHealth != null)
        {
            playerHealth.Changed += Refresh;
        }

        if (playerWeapon != null)
        {
            playerWeapon.Changed += Refresh;
        }

        if (game != null)
        {
            game.RunPointsChanged += Refresh;
            game.RunProgressionChanged += Refresh;
            game.RunStateChanged += Refresh;
            game.TimerChanged += Refresh;
            game.UpgradesCollectedChanged += Refresh;
        }
    }

    private void Unsubscribe()
    {
        if (playerHealth != null)
        {
            playerHealth.Changed -= Refresh;
        }

        if (playerWeapon != null)
        {
            playerWeapon.Changed -= Refresh;
        }

        if (game != null)
        {
            game.RunPointsChanged -= Refresh;
            game.RunProgressionChanged -= Refresh;
            game.RunStateChanged -= Refresh;
            game.TimerChanged -= Refresh;
            game.UpgradesCollectedChanged -= Refresh;
        }
    }

    private void BindButtons()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(HandlePauseClicked);
            pauseButton.onClick.AddListener(HandlePauseClicked);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(HandleResumeClicked);
            resumeButton.onClick.AddListener(HandleResumeClicked);
        }

        if (pauseMainMenuButton != null)
        {
            pauseMainMenuButton.onClick.RemoveListener(HandlePauseMainMenuClicked);
            pauseMainMenuButton.onClick.AddListener(HandlePauseMainMenuClicked);
        }
    }

    private void UnbindButtons()
    {
        if (pauseButton != null)
        {
            pauseButton.onClick.RemoveListener(HandlePauseClicked);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(HandleResumeClicked);
        }

        if (pauseMainMenuButton != null)
        {
            pauseMainMenuButton.onClick.RemoveListener(HandlePauseMainMenuClicked);
        }
    }

    private void OnDestroy()
    {
        UnbindButtons();
        Unsubscribe();
        SetPaused(false);
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    private void Refresh()
    {
        hpText.text = $"HP: {playerHealth.CurrentHp}/{playerHealth.MaxHp}";
        ammoText.text = $"{playerWeapon.WeaponDisplayName}: {playerWeapon.Ammo}/{playerWeapon.MagazineSize}";
        reloadText.text = playerWeapon.IsReloading ? "Reload: Reloading" : "Reload: Ready";
        pointsText.text = $"Points: {game.RunPoints}";
        progressionText.text = $"Next: {game.CurrentUpgradeProgress}/{game.NextUpgradePointThreshold}";
        timerText.text = $"Time: {FormatTime(game.RemainingRunTime)}";
        RefreshUpgradeList();

        if (game.HasWon)
        {
            stateText.text = "VICTORY\nPress R to Restart or use Pause Menu";
            stateText.color = new Color(0.35f, 1f, 0.45f);
        }
        else if (game.HasLost)
        {
            stateText.text = "DEFEATED\nPress R to Restart or use Pause Menu";
            stateText.color = new Color(1f, 0.25f, 0.2f);
        }
        else
        {
            stateText.text = string.Empty;
            stateText.color = Color.white;
        }
    }

    private void RefreshUpgradeList()
    {
        var labels = game.CollectedUpgradeLabels;
        if (labels == null || labels.Count == 0)
        {
            upgradeText.text = "Upgrades: None";
            return;
        }

        upgradeText.text = "Upgrades: " + string.Join(", ", labels);
    }

    private void HandlePauseClicked()
    {
        TogglePause();
    }

    private void HandleResumeClicked()
    {
        SetPaused(false);
    }

    private void HandlePauseMainMenuClicked()
    {
        if (game == null)
        {
            return;
        }

        SetPaused(false);
        game.ReturnToMainMenu();
    }

    private void TogglePause()
    {
        SetPaused(!isPaused);
    }

    private void SetPaused(bool paused)
    {
        isPaused = paused;

        if (pauseOverlay != null)
        {
            pauseOverlay.SetActive(paused);
        }

        if (playerController != null)
        {
            if (paused)
            {
                playerController.DisableControls();
            }
            else
            {
                playerController.EnableControls();
            }
        }

        Time.timeScale = paused ? 0f : 1f;
    }

    private static string FormatTime(float timeSeconds)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, timeSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
