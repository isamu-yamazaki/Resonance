using System.Collections.Generic;
using System.Linq;
using PurrNet;
using Resonance.Assemblies.MatchStat;
using UnityEngine;
using Resonance.Helper;
using Resonance.Match;

public class PlayerViewModel : MonoBehaviour
{
    // -----------------
    // HEALTH
    // -----------------
    public ObservableValue<float> Health { get; private set; }
    public float MaxHealth { get; private set; } = 100f;

    // -----------------
    // AMMO
    // -----------------
    public ObservableValue<int> CurrentAmmo { get; private set; }
    public ObservableValue<int> MagazineSize { get; private set; }
    public ObservableValue<bool> IsReloading { get; private set; }
    public ObservableValue<float> ReloadProgress { get; private set; } // 0 → 1

    // -----------------
    // ELIMINATION POPUP
    // -----------------
    public ObservableValue<bool> GotKill { get; private set; }
    public ObservableValue<string> LastVictimName { get; private set; }
    private MatchStatNetworkAdapter matchStats;
    
    // -----------------
    // RATING
    // -----------------
    public ObservableValue<float> Rating { get; private set; }
    public ObservableValue<int> Rank { get; private set; }
    public ObservableValue<float> RatingDelta { get; private set; } // fires the pulse

    void Awake()
    {
        Health = new ObservableValue<float>(MaxHealth);

        CurrentAmmo = new ObservableValue<int>(0);
        MagazineSize = new ObservableValue<int>(0);
        IsReloading = new ObservableValue<bool>(false);
        ReloadProgress = new ObservableValue<float>(0f);
        
        GotKill = new ObservableValue<bool>(false);
        LastVictimName = new ObservableValue<string>("");

        matchStats = MatchLogicNetworkAdapter.Instance?.MatchStats;
        
        Rating = new ObservableValue<float>(0f);
        Rank = new ObservableValue<int>(0);
        RatingDelta = new ObservableValue<float>(0f);
    }
    
    private void Start()
    {
        matchStats = MatchLogicNetworkAdapter.Instance?.MatchStats;
    }

    public void InitializeHealth(float maxHealth)
    {
        MaxHealth = maxHealth;
        Health.Value = MaxHealth;
    }

    public void InitializeAmmo(int magazineSize)
    {
        MagazineSize.Value = magazineSize;
        CurrentAmmo.Value = magazineSize;
    }
    
    // Called by Shooter
    public void SetAmmo(int current, int max)
    {
        CurrentAmmo.Value = current;
        MagazineSize.Value = max;
    }

    public void SetReloadState(bool isReloading)
    {
        IsReloading.Value = isReloading;

        if (!isReloading)
            ReloadProgress.Value = 0f;
    }

    public void SetReloadProgress(float progress)
    {
        ReloadProgress.Value = progress;
    }

    public void TakeDamage(float amount)
    {
        Health.Value = Mathf.Max(Health.Value - amount, 0f);
    }

    public void Heal(float amount)
    {
        Health.Value = Mathf.Min(Health.Value + amount, MaxHealth);
    }
    
    public void NotifyKill(string victimName)
    {
        LastVictimName.Value = victimName;
        GotKill.Value = true;
        GotKill.Value = false;
    }

    private void OnEnable()
    {
        if (matchStats != null)
        {
            matchStats.OnPlayerKill += HandlePlayerKill;
            matchStats.OnAllStatsUpdate += HandleAllStatsUpdate;
        }
}

    private void OnDisable()
    {
        if (matchStats != null)
        {
            matchStats.OnPlayerKill -= HandlePlayerKill;
            matchStats.OnAllStatsUpdate -= HandleAllStatsUpdate;
        }
    }

    private void HandlePlayerKill(PlayerID killer, PlayerID victim)
    {
        if (killer == NetworkManager.main.localPlayer)
            NotifyKill(victim.ToString()); // or however you resolve a PlayerID to a display name
    }
    
    public void SetRating(float newRating)
    {
        float delta = newRating - Rating.Value;
        Rating.Value = newRating;
    
        if (Mathf.Abs(delta) > 0.01f)
        {
            RatingDelta.Value = delta;
            RatingDelta.Value = 0f;
        }
    }

    public void SetRank(int rank)
    {
        Rank.Value = rank;
    }
    
    private void HandleAllStatsUpdate(Dictionary<PlayerID, PlayerMatchStats> allStats)
    {
        // Only care about the local player
        PlayerID localId = NetworkManager.main.localPlayer;
        if (!allStats.TryGetValue(localId, out PlayerMatchStats newStats)) return;

        float prev = Rating.Value;
        float next = newStats.rating;
        SetRating(next);

        // Rank: sort all players by rating descending, find local player's index
        var sorted = allStats.OrderByDescending(kv => kv.Value.rating).ToList();
        int rank = sorted.FindIndex(kv => kv.Key == localId) + 1;
        SetRank(rank);
    }
}