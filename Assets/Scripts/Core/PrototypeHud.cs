using UnityEngine;
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
    [SerializeField] private Button mainMenuButton;

    private PlayerHealth playerHealth;
    private PlayerWeapon playerWeapon;
    private SurvivalArenaGame game;

    public void BindSession(PlayerHealth health, PlayerWeapon weapon, SurvivalArenaGame owner)
    {
        Unsubscribe();
        UnbindButtons();

        playerHealth = health;
        playerWeapon = weapon;
        game = owner;

        BindButtons();
        Subscribe();
        Refresh();
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
        }
    }

    private void BindButtons()
    {
        if (mainMenuButton == null)
        {
            return;
        }

        mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
        mainMenuButton.onClick.AddListener(HandleMainMenuClicked);
    }

    private void UnbindButtons()
    {
        if (mainMenuButton == null)
        {
            return;
        }

        mainMenuButton.onClick.RemoveListener(HandleMainMenuClicked);
    }

    private void OnDestroy()
    {
        UnbindButtons();
        Unsubscribe();
    }

    private void Refresh()
    {
        hpText.text = $"HP: {playerHealth.CurrentHp}/{playerHealth.MaxHp}";
        ammoText.text = $"{playerWeapon.WeaponDisplayName}: {playerWeapon.Ammo}/{playerWeapon.MagazineSize}";
        reloadText.text = playerWeapon.IsReloading ? "Reload: Reloading" : "Reload: Ready";
        pointsText.text = $"Points: {game.RunPoints}";
        progressionText.text = $"Next Upgrade: {game.CurrentUpgradeProgress}/{game.PointsPerUpgrade}";
        timerText.text = $"Time: {FormatTime(game.RemainingRunTime)}";
        upgradeText.text = $"Last Upgrade: {game.LastGrantedUpgradeLabel}";

        if (game.HasWon)
        {
            stateText.text = "VICTORY\nPress R to Restart or use Main Menu";
            stateText.color = new Color(0.35f, 1f, 0.45f);
        }
        else if (game.HasLost)
        {
            stateText.text = "DEFEATED\nPress R to Restart or use Main Menu";
            stateText.color = new Color(1f, 0.25f, 0.2f);
        }
        else
        {
            stateText.text = string.Empty;
            stateText.color = Color.white;
        }
    }

    private void HandleMainMenuClicked()
    {
        if (game == null)
        {
            return;
        }

        game.ReturnToMainMenu();
    }

    private static string FormatTime(float timeSeconds)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, timeSeconds));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}
