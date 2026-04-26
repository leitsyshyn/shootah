using System;
using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerConfig playerConfig;

    public int CurrentHp { get; private set; }
    public int MaxHp { get; private set; }
    public bool IsDead => CurrentHp <= 0;

    public event Action Changed;
    public event Action Died;

    private void Awake()
    {
        if (playerConfig == null)
        {
            Debug.LogError("PlayerHealth requires a PlayerConfig reference.", this);
            enabled = false;
            return;
        }

        MaxHp = playerConfig.BaseHp;
        CurrentHp = MaxHp;
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
    }
}
