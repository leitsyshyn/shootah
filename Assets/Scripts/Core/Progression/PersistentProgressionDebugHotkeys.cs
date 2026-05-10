#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class PersistentProgressionDebugHotkeys : MonoBehaviour
{
    private ProgressionScreenController cachedProgressionScreen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureInstance()
    {
        if (FindAnyObjectByType<PersistentProgressionDebugHotkeys>() != null)
        {
            return;
        }

        GameObject debugObject = new GameObject(nameof(PersistentProgressionDebugHotkeys));
        DontDestroyOnLoad(debugObject);
        debugObject.AddComponent<PersistentProgressionDebugHotkeys>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        PersistentProgressionService progression = PersistentProgressionService.Instance;
        if (IsProgressionShopOpen() && Keyboard.current.cKey.wasPressedThisFrame)
        {
            int cheatAmount = progression.Config != null && progression.Config.ShopConfig != null
                ? progression.Config.ShopConfig.CheatCurrencyAmount
                : 25;
            progression.AddPersistentCurrency(cheatAmount);
            Debug.Log($"Persistent progression debug: granted {cheatAmount} currency. {progression.GetDebugSummary()}");
        }

        if (IsProgressionShopOpen() && Keyboard.current.mKey.wasPressedThisFrame)
        {
            int cheatAmount = progression.Config != null && progression.Config.ShopConfig != null
                ? progression.Config.ShopConfig.CheatCurrencyAmount
                : 25;
            progression.RemovePersistentCurrency(cheatAmount);
            Debug.Log($"Persistent progression debug: removed {cheatAmount} currency. {progression.GetDebugSummary()}");
        }

        if (IsProgressionShopOpen() && Keyboard.current.rKey.wasPressedThisFrame)
        {
            progression.ResetProfile();
            Debug.Log($"Persistent progression debug: reset profile. {progression.GetDebugSummary()}");
        }

        if (!IsGameplaySceneActive())
        {
            return;
        }

        if (Keyboard.current.f6Key.wasPressedThisFrame)
        {
            bool purchased = progression.TryPurchaseUpgrade(PersistentProgressionIds.MaxHealthUpgrade);
            Debug.Log($"Persistent progression debug: buy max health => {purchased}. {progression.GetDebugSummary()}");
        }

        if (Keyboard.current.f7Key.wasPressedThisFrame)
        {
            bool purchased = progression.TryPurchaseUpgrade(PersistentProgressionIds.MoveSpeedUpgrade);
            Debug.Log($"Persistent progression debug: buy move speed => {purchased}. {progression.GetDebugSummary()}");
        }

        if (Keyboard.current.f8Key.wasPressedThisFrame)
        {
            bool unlocked = progression.UnlockWeapon(PersistentProgressionIds.ShotgunWeapon);
            Debug.Log($"Persistent progression debug: unlock shotgun => {unlocked}. {progression.GetDebugSummary()}");
        }

        if (Keyboard.current.f9Key.wasPressedThisFrame)
        {
            bool unlocked = progression.UnlockWeapon(PersistentProgressionIds.SniperRifleWeapon);
            Debug.Log($"Persistent progression debug: unlock sniper rifle => {unlocked}. {progression.GetDebugSummary()}");
        }

        if (Keyboard.current.f10Key.wasPressedThisFrame)
        {
            bool unlocked = progression.UnlockWeapon(PersistentProgressionIds.GrenadeLauncherWeapon);
            Debug.Log($"Persistent progression debug: unlock grenade launcher => {unlocked}. {progression.GetDebugSummary()}");
        }

        if (Keyboard.current.f12Key.wasPressedThisFrame)
        {
            Debug.Log($"Persistent progression debug: {progression.GetDebugSummary()} | savePath={progression.SavePath}");
        }
    }

    private bool IsProgressionShopOpen()
    {
        if (cachedProgressionScreen == null)
        {
            cachedProgressionScreen = FindFirstObjectByType<ProgressionScreenController>(FindObjectsInactive.Include);
        }

        return cachedProgressionScreen != null && cachedProgressionScreen.isActiveAndEnabled && cachedProgressionScreen.gameObject.activeInHierarchy;
    }

    private static bool IsGameplaySceneActive()
    {
        return SceneManager.GetActiveScene().name == "SurvivalArena";
    }
}
#endif
