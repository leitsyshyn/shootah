using System;
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
    [SerializeField] private UpgradeTrackConfig upgradeTrackConfig;

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

    private PlayerHealth playerHealth;
    private PlayerController2D playerController;
    private PlayerWeapon playerWeapon;
    private Collider2D playerCollider;
    private PrototypeHud hudInstance;
    private InputAction restartAction;

    public RunState CurrentRunState { get; private set; } = RunState.Playing;
    public bool IsRunActive => CurrentRunState == RunState.Playing;
    public bool IsRunEnded => CurrentRunState != RunState.Playing;
    public bool HasWon => CurrentRunState == RunState.Victory;
    public bool HasLost => CurrentRunState == RunState.Defeat;
    public bool IsDefeated => HasLost;
    public int RunPoints { get; private set; }
    public int UpgradesEarned { get; private set; }
    public int PointsPerUpgrade => upgradeTrackConfig.PointsPerUpgrade;
    public int NextUpgradePointThreshold => (UpgradesEarned + 1) * PointsPerUpgrade;
    public int CurrentUpgradeProgress => RunPoints - (UpgradesEarned * PointsPerUpgrade);
    public string LastGrantedUpgradeLabel { get; private set; } = "None";
    public float TargetSurvivalDuration => runConfig.TargetSurvivalDuration;
    public float ElapsedRunTime { get; private set; }
    public float RemainingRunTime => Mathf.Max(0f, TargetSurvivalDuration - ElapsedRunTime);
    public event Action RunStateChanged;
    public event Action RunPointsChanged;
    public event Action RunProgressionChanged;
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
        ConfigureRuntimeTiming();
        Transform player = SpawnPlayer();
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
        HandleReturnToMainMenuRequested();
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

    private void CreateHud()
    {
        hudInstance = Instantiate(hudPrefab);
        hudInstance.BindSession(playerHealth, playerWeapon, this);
    }

public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

private void HandleReturnToMainMenuRequested()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReturnToMainMenu();
        }
    }



    public void AddRunPoints(int amount)
    {
        if (amount <= 0 || !IsRunActive)
        {
            return;
        }

        RunPoints += amount;
        RunPointsChanged?.Invoke();
        GrantPendingRunUpgrades();
        RunProgressionChanged?.Invoke();
    }

    private void GrantPendingRunUpgrades()
    {
        while (IsRunActive && RunPoints >= NextUpgradePointThreshold)
        {
            if (!upgradeTrackConfig.TryGetUpgrade(UpgradesEarned, out UpgradeTrackConfig.UpgradeEntry nextUpgrade))
            {
                break;
            }

            UpgradesEarned++;
            GrantRunUpgrade(nextUpgrade);
        }
    }

    private void GrantRunUpgrade(UpgradeTrackConfig.UpgradeEntry upgradeEntry)
    {
        switch (upgradeEntry.UpgradeType)
        {
            case RunUpgradeType.DamageUp:
                int damageUpgradeAmount = Mathf.Max(0, upgradeEntry.IntValue);
                playerWeapon?.AddProjectileDamageBonus(damageUpgradeAmount);
                LastGrantedUpgradeLabel = $"Damage +{damageUpgradeAmount}";
                break;
            case RunUpgradeType.FireRateUp:
                float fireRateUpgradeAmount = Mathf.Max(0f, upgradeEntry.Value);
                playerWeapon?.AddFireRateBonus(fireRateUpgradeAmount);
                LastGrantedUpgradeLabel = $"Fire Rate +{fireRateUpgradeAmount:0.###}s";
                break;
            case RunUpgradeType.MoveSpeedUp:
                float moveSpeedUpgradeAmount = Mathf.Max(0f, upgradeEntry.Value);
                playerController?.AddMoveSpeedBonus(moveSpeedUpgradeAmount);
                LastGrantedUpgradeLabel = $"Move Speed +{moveSpeedUpgradeAmount:0.##}";
                break;
            default:
                LastGrantedUpgradeLabel = "Unknown";
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
        playerController?.DisableControls();
        playerWeapon?.CancelReload();
        enemySpawner?.StopSpawning();

        foreach (Enemy enemy in FindObjectsByType<Enemy>())
        {
            enemy.StopMoving();
        }

        foreach (Projectile projectile in FindObjectsByType<Projectile>())
        {
            projectile.StopMoving();
        }

        TimerChanged?.Invoke();
        RunStateChanged?.Invoke();
        return true;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= EnterDefeatState;
        }
    }
}
