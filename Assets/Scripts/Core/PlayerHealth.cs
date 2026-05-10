using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerConfig playerConfig;

    private int baseMaxHp;
    private int permanentMaxHpBonus;
    private int runMaxHpBonus;

    public int CurrentHp { get; private set; }
    public int MaxHp { get; private set; }
    public bool IsDead => CurrentHp <= 0;

    public event Action Changed;
    public event Action Died;
    public event Action Damaged;
    public event Action Healed;

    private void Awake()
    {
        if (playerConfig == null)
        {
            Debug.LogError("PlayerHealth requires a PlayerConfig reference.", this);
            enabled = false;
            return;
        }

        baseMaxHp = playerConfig.BaseHp;
        permanentMaxHpBonus = 0;
        runMaxHpBonus = 0;
        MaxHp = baseMaxHp;
        CurrentHp = MaxHp;
        Changed?.Invoke();
    }

    public void SetPermanentMaxHpBonus(int amount)
    {
        permanentMaxHpBonus = Mathf.Max(0, amount);
        RecalculateMaxHp(true);
    }

    public void AddRunMaxHpBonus(int amount)
    {
        if (amount <= 0) return;
        runMaxHpBonus += amount;
        RecalculateMaxHp(true);
    }

    private void RecalculateMaxHp(bool heal)
    {
        MaxHp = Mathf.Max(1, baseMaxHp + permanentMaxHpBonus + runMaxHpBonus);
        if (heal) CurrentHp = MaxHp;
        Changed?.Invoke();
    }

    public void TakeDamage(int damage)
    {
        if (IsDead)
        {
            return;
        }

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        Changed?.Invoke();
        Damaged?.Invoke();

        if (CurrentHp <= 0)
        {
            Died?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
        {
            return;
        }

        int nextHp = Mathf.Clamp(CurrentHp + amount, 0, MaxHp);
        if (nextHp == CurrentHp)
        {
            return;
        }

        CurrentHp = nextHp;
        Changed?.Invoke();
        Healed?.Invoke();
    }
}
