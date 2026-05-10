using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class SurvivalArenaGame : MonoBehaviour
{
    public enum RunState
    {
        Playing = 0,
        Victory = 1,
        Defeat = 2
    }

    [Header("Configuration")]
    [SerializeField] private RunConfig runConfig;
    [SerializeField] private RunUpgradeConfig runUpgradeConfig;

    [Header("Composition")]
    [SerializeField] private TopDownCameraFollow cameraFollow;
    [SerializeField] private ArenaGrid arenaGrid;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private PickupSpawner pickupSpawner;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private PrototypeHud hudPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform playerContainer;
    [SerializeField] private Transform projectileContainer;
    [SerializeField] private InputActionReference restartActionReference;

    [Header("Upgrade Popup")]
    [SerializeField] private RunUpgradeChoicePopup upgradePopupPrefab;

    private PlayerHealth playerHealth;
    private PlayerController2D playerController;
    private PlayerWeapon playerWeapon;
    private Collider2D playerCollider;
    private PrototypeHud hudInstance;
    private InputAction restartAction;

    private int upgradesEarned;
    private int cumulativeXpSpent;
    private bool isShowingPopup;
    private List<string> collectedUpgradeLabels = new();

    public RunState CurrentRunState { get; private set; } = RunState.Playing;
    public bool IsRunActive => CurrentRunState == RunState.Playing;
    public bool IsRunEnded => CurrentRunState != RunState.Playing;
    public bool HasWon => CurrentRunState == RunState.Victory;
    public bool HasLost => CurrentRunState == RunState.Defeat;
    public bool IsDefeated => HasLost;
    public int RunPoints { get; private set; }
    public int UpgradesEarned => upgradesEarned;
    public int CurrentUpgradeProgress => RunPoints - cumulativeXpSpent;
    public int NextUpgradePointThreshold => runUpgradeConfig.GetXpForLevel(upgradesEarned);
    public IReadOnlyList<string> CollectedUpgradeLabels => collectedUpgradeLabels;
    public float TargetSurvivalDuration => runConfig.TargetSurvivalDuration;
    public float ElapsedRunTime { get; private set; }
    public float RemainingRunTime => Mathf.Max(0f, TargetSurvivalDuration - ElapsedRunTime);
    public event Action RunStateChanged;
    public event Action RunPointsChanged;
    public event Action RunProgressionChanged;
    public event Action UpgradesCollectedChanged;
    public event Action TimerChanged;

    private void OnEnable()
    {
        restartAction = restartActionReference != null ? restartActionReference.action : null;
        if (restartAction == null)
        {
            Debug.LogError("SurvivalArenaGame requires a restart input action reference.", this);
            enabled = false;
            return;
        }

        restartAction.performed += OnRestartPerformed;
        restartAction.Enable();
    }

    private void Awake()
    {
        PersistentProgressionService.Warmup();
        ConfigureRuntimeTiming();
        Transform player = SpawnPlayer();
        ApplyPersistentProgression();
        BindSceneSystems(player);
        CreateHud();
    }

    private void OnDisable()
    {
        if (restartAction == null)
        {
            return;
        }

        restartAction.performed -= OnRestartPerformed;
        restartAction.Disable();
        restartAction = null;
    }

    private void Update()
    {
        HandleRunTimer();
    }

    private void ConfigureRuntimeTiming()
    {
        int configuredFrameRate = runConfig.TargetFrameRate;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = configuredFrameRate;
        Time.fixedDeltaTime = 1f / configuredFrameRate;
    }

    private Transform SpawnPlayer()
    {
        GameObject playerObject = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation, playerContainer);

        playerController = playerObject.GetComponent<PlayerController2D>();
        playerHealth = playerObject.GetComponent<PlayerHealth>();
        playerWeapon = playerObject.GetComponent<PlayerWeapon>();
        playerCollider = playerObject.GetComponent<Collider2D>();

        playerHealth.Died += EnterDefeatState;
        playerController.BindWorldCamera(cameraFollow.WorldCamera);
        playerWeapon.BindSession(this, projectileContainer);

        return playerObject.transform;
    }

    private void BindSceneSystems(Transform player)
    {
        cameraFollow.SetFollowTarget(player);
        arenaGrid.SetFollowTarget(player);
        pickupSpawner.BeginSession(this, playerHealth);
        enemySpawner.BeginSession(this, player, playerCollider, playerHealth, pickupSpawner);
    }

    private void ApplyPersistentProgression()
    {
        PersistentProgressionService progression = PersistentProgressionService.Instance;
        playerHealth?.SetPermanentMaxHpBonus(Mathf.RoundToInt(progression.GetPermanentStatBonus(PersistentProgressionStatType.MaxHealth)));
        playerController?.SetPermanentMoveSpeedBonus(progression.GetPermanentStatBonus(PersistentProgressionStatType.MoveSpeed));
        if (progression.Config != null && progression.Config.TryGetWeaponDefinition(progression.SelectedWeaponId, out PersistentProgressionConfig.WeaponUnlockDefinition definition))
        {
            playerWeapon?.SetWeaponConfig(definition.WeaponConfig);
        }
    }

    private void CreateHud()
    {
        hudInstance = Instantiate(hudPrefab);
        hudInstance.BindSession(playerHealth, playerWeapon, this);
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void AddRunPoints(int amount)
    {
        if (amount <= 0 || !IsRunActive)
        {
            return;
        }

        RunPoints += amount;
        RunPointsChanged?.Invoke();
        TryTriggerUpgradePopup();
        RunProgressionChanged?.Invoke();
    }

    private void TryTriggerUpgradePopup()
    {
        if (!IsRunActive) return;

        bool earned = false;
        while (RunPoints >= cumulativeXpSpent + NextUpgradePointThreshold)
        {
            cumulativeXpSpent += NextUpgradePointThreshold;
            upgradesEarned++;
            earned = true;
        }

        if (earned && !isShowingPopup)
        {
            ShowUpgradeChoicePopup();
        }
    }

    private void ShowUpgradeChoicePopup()
    {
        if (upgradePopupPrefab == null)
        {
            Debug.LogError("Upgrade popup prefab is not assigned!", this);
            return;
        }

        isShowingPopup = true;
        playerController?.DisableControls();
        playerWeapon?.CancelReload();
        Time.timeScale = 0f;

        RunUpgradeChoicePopup popup = Instantiate(upgradePopupPrefab);
        popup.Init(this, playerHealth, runUpgradeConfig, OnUpgradeChosen);
    }

    private void OnUpgradeChosen(RunUpgradeType type)
    {
        ApplyRunUpgrade(type);
        isShowingPopup = false;
        Time.timeScale = 1f;
        if (playerController != null && CurrentRunState == RunState.Playing)
        {
            playerController.EnableControls();
        }

        playerWeapon?.TryResumeReload();

        RunProgressionChanged?.Invoke();
        UpgradesCollectedChanged?.Invoke();

        TryTriggerUpgradePopup();
    }

    private void ApplyRunUpgrade(RunUpgradeType type)
    {
        RunUpgradeConfig.UpgradeDefinition? foundDef = null;
        var allUpgrades = runUpgradeConfig.Upgrades;
        for (int i = 0; i < allUpgrades.Count; i++)
        {
            if (allUpgrades[i].type == type)
            {
                foundDef = allUpgrades[i];
                break;
            }
        }

        if (foundDef == null)
        {
            collectedUpgradeLabels.Add("Unknown");
            return;
        }

        RunUpgradeConfig.UpgradeDefinition def = foundDef.Value;
        float val = def.value;
        collectedUpgradeLabels.Add(def.displayName);

        switch (type)
        {
            case RunUpgradeType.DamageUp:
                int dmg = Mathf.Max(1, Mathf.RoundToInt(val));
                playerWeapon?.AddProjectileDamageBonus(dmg);
                break;
            case RunUpgradeType.FireRateUp:
                playerWeapon?.AddFireRateBonus(val);
                break;
            case RunUpgradeType.MoveSpeedUp:
                playerController?.AddMoveSpeedBonus(val);
                break;
            case RunUpgradeType.MaxHpUp:
                int hpBonus = Mathf.RoundToInt(val);
                playerHealth?.AddRunMaxHpBonus(hpBonus);
                break;
            case RunUpgradeType.ReloadSpeedUp:
                playerWeapon?.AddReloadSpeedBonus(val);
                break;
            case RunUpgradeType.ProjectileSpeedUp:
                playerWeapon?.AddProjectileSpeedBonus(val);
                break;
        }
    }

    private void HandleRunTimer()
    {
        if (!IsRunActive)
        {
            return;
        }

        ElapsedRunTime = Mathf.Min(TargetSurvivalDuration, ElapsedRunTime + Time.deltaTime);
        TimerChanged?.Invoke();

        if (ElapsedRunTime >= TargetSurvivalDuration)
        {
            TryEndRun(RunState.Victory);
        }
    }

    private void HandleRestartRequested()
    {
        if (!IsRunEnded)
        {
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnRestartPerformed(InputAction.CallbackContext context)
    {
        HandleRestartRequested();
    }

    private void EnterDefeatState()
    {
        TryEndRun(RunState.Defeat);
    }

    private bool TryEndRun(RunState endState)
    {
        if (CurrentRunState != RunState.Playing)
        {
            return false;
        }

        CurrentRunState = endState;

        if (isShowingPopup)
        {
            isShowingPopup = false;
            Time.timeScale = 1f;
        }

        playerController?.DisableControls();
        playerWeapon?.CancelReload();
        enemySpawner?.StopSpawning();

        foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>())
        {
            enemy.StopMoving();
        }

        foreach (Projectile projectile in FindObjectsByType<Projectile>())
        {
            projectile.StopMoving();
        }

        foreach (EnemyProjectile enemyProjectile in FindObjectsByType<EnemyProjectile>())
        {
            enemyProjectile.StopMoving();
        }

        TimerChanged?.Invoke();
        RunStateChanged?.Invoke();
        return true;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Damaged -= OnPlayerDamaged;
            playerHealth.Died -= EnterDefeatState;
        }
    }

    private void OnPlayerDamaged()
    {
        GameEffects.Instance.DamageFlash();
    }

    private void Start()
    {
        playerHealth.Damaged += OnPlayerDamaged;
        GameEffects.Instance.Warmup();
    }
}
