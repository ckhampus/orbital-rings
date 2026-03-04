using System;
using System.Collections.Generic;
using Godot;
using OrbitalRings.Data;

namespace OrbitalRings.Autoloads;

/// <summary>
/// Core economy engine for the orbital station. Owns the credit balance,
/// ticks passive income on a Timer, calculates room costs, and exposes
/// spend/earn/refund methods.
///
/// Registered as an Autoload in project.godot -- access via EconomyManager.Instance.
/// Loads EconomyConfig from the default .tres resource or falls back to code defaults.
///
/// Income ticks use a child Timer node (not _Process delta accumulation) per user decision:
/// periodic chunk ticks every 5.5 seconds, not smooth rolling.
/// </summary>
public partial class EconomyManager : Node
{
    /// <summary>
    /// Singleton instance, set in _Ready(). Guaranteed non-null after Autoloads initialize.
    /// </summary>
    public static EconomyManager Instance { get; private set; }

    /// <summary>
    /// When true, _Ready() skips setting credits to StartingCredits. Set by SaveManager
    /// before scene transition so loaded state is not overwritten by default initialization.
    /// </summary>
    public static bool StateLoaded { get; set; }

    /// <summary>
    /// Economy configuration loaded from .tres resource. All income rates, cost formulas,
    /// and multipliers are defined here for Inspector tuning.
    /// </summary>
    [Export] public EconomyConfig Config { get; set; }

    /// <summary>Current credit balance (integer, no floating point).</summary>
    public int Credits => _credits;

    private int _credits;
    private Timer _incomeTimer;
    private int _citizenCount;
    private int _workingCitizenCount;
    private readonly HashSet<string> _workingCitizens = new();
    private float _currentHappiness;

    public override void _Ready()
    {
        Instance = this;

        // Load config: prefer Inspector-assigned, then .tres file, then code defaults
        if (Config == null)
        {
            Config = ResourceLoader.Load<EconomyConfig>("res://Resources/Economy/default_economy.tres");
        }
        if (Config == null)
        {
            GD.PushWarning("EconomyManager: No EconomyConfig assigned or found at default path. Using code defaults.");
            Config = new EconomyConfig();
        }

        // Only set starting credits on fresh game (not when loading from save)
        if (!StateLoaded)
            _credits = Config.StartingCredits;

        // Create Timer as child node for periodic income ticks
        _incomeTimer = new Timer();
        _incomeTimer.WaitTime = Config.IncomeTickInterval;
        _incomeTimer.OneShot = false;
        AddChild(_incomeTimer);
        _incomeTimer.Timeout += OnIncomeTick;
        _incomeTimer.Start();

        // Timer pauses with the scene tree
        ProcessMode = ProcessModeEnum.Pausable;

        // Emit initial balance so HUD can display starting credits
        GameEvents.Instance?.EmitCreditsChanged(_credits);

        // Subscribe to citizen room visit events for work bonus tracking
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.CitizenEnteredRoom += OnCitizenEnteredRoom;
            GameEvents.Instance.CitizenExitedRoom += OnCitizenExitedRoom;
        }
    }

    public override void _ExitTree()
    {
        if (GameEvents.Instance != null)
        {
            GameEvents.Instance.CitizenEnteredRoom -= OnCitizenEnteredRoom;
            GameEvents.Instance.CitizenExitedRoom -= OnCitizenExitedRoom;
        }
    }

    // -------------------------------------------------------------------------
    // Income
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called by the Timer every IncomeTickInterval seconds.
    /// Calculates income and adds it to the balance.
    /// </summary>
    private void OnIncomeTick()
    {
        int income = CalculateTickIncome();
        if (income > 0)
        {
            _credits += income;
            GameEvents.Instance?.EmitIncomeTicked(income);
            GameEvents.Instance?.EmitCreditsChanged(_credits);
        }
    }

    /// <summary>
    /// Pure calculation of income per tick. Public for testing/display.
    ///
    /// Formula: (BaseStationIncome + PassiveIncomePerCitizen * sqrt(citizenCount) + workingCitizens * WorkBonusMultiplier)
    ///          * happinessMultiplier
    ///
    /// The sqrt scaling on citizen income prevents runaway feedback loops.
    /// </summary>
    public int CalculateTickIncome()
    {
        float baseIncome = Config.BaseStationIncome;
        float citizenIncome = Config.PassiveIncomePerCitizen * MathF.Sqrt(_citizenCount);
        float workBonus = _workingCitizenCount * Config.WorkBonusMultiplier;
        float happinessMult = 1.0f + (_currentHappiness * (Config.HappinessMultiplierCap - 1.0f));
        return Mathf.RoundToInt((baseIncome + citizenIncome + workBonus) * happinessMult);
    }

    // -------------------------------------------------------------------------
    // Credit Management
    // -------------------------------------------------------------------------

    /// <summary>
    /// Attempt to spend credits. Returns false and does not deduct if balance is insufficient.
    /// </summary>
    /// <param name="amount">Positive amount to spend.</param>
    /// <returns>True if spend succeeded, false if insufficient funds or invalid amount.</returns>
    public bool TrySpend(int amount)
    {
        if (amount <= 0 || _credits < amount)
            return false;

        _credits -= amount;
        GameEvents.Instance?.EmitCreditsSpent(amount);
        GameEvents.Instance?.EmitCreditsChanged(_credits);
        return true;
    }

    /// <summary>
    /// Add credits to the balance (for quest rewards, trade, etc.).
    /// </summary>
    /// <param name="amount">Positive amount to earn.</param>
    public void Earn(int amount)
    {
        if (amount <= 0) return;
        _credits += amount;
        GameEvents.Instance?.EmitCreditsChanged(_credits);
    }

    /// <summary>
    /// Refund credits to the balance (demolish refund, cancelled construction, etc.).
    /// Emits CreditsRefunded in addition to CreditsChanged for HUD floating text.
    /// </summary>
    /// <param name="amount">Positive amount to refund.</param>
    public void Refund(int amount)
    {
        if (amount <= 0) return;
        _credits += amount;
        GameEvents.Instance?.EmitCreditsRefunded(amount);
        GameEvents.Instance?.EmitCreditsChanged(_credits);
    }

    // -------------------------------------------------------------------------
    // Cost Calculation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Calculate the credit cost for placing a room.
    /// Pure function -- does not modify state.
    ///
    /// Formula: baseCost * categoryMultiplier * (segments * sizeDiscount^(segments-1)) * rowMultiplier
    /// </summary>
    /// <param name="room">Room definition with category and optional cost override.</param>
    /// <param name="segmentCount">Number of ring segments the room occupies.</param>
    /// <param name="isOuterRow">True if placed on the outer row (larger, costs more).</param>
    /// <returns>Final cost rounded to nearest integer.</returns>
    public int CalculateRoomCost(RoomDefinition room, int segmentCount, bool isOuterRow)
    {
        float baseCost = room.BaseCostOverride > 0 ? room.BaseCostOverride : Config.BaseRoomCost;
        float catMult = GetCategoryMultiplier(room.Category);
        float sizeFactor = segmentCount * MathF.Pow(Config.SizeDiscountFactor, segmentCount - 1);
        float rowMult = isOuterRow ? Config.OuterRowCostMultiplier : 1.0f;
        return Mathf.RoundToInt(baseCost * catMult * sizeFactor * rowMult);
    }

    /// <summary>
    /// Calculate the credit refund for demolishing a room.
    /// </summary>
    /// <param name="originalCost">The cost that was paid when the room was built.</param>
    /// <returns>Refund amount rounded to nearest integer.</returns>
    public int CalculateDemolishRefund(int originalCost)
    {
        return Mathf.RoundToInt(originalCost * Config.DemolishRefundRatio);
    }

    /// <summary>
    /// Get the cost multiplier for a room category from config.
    /// </summary>
    private float GetCategoryMultiplier(RoomDefinition.RoomCategory category)
    {
        return category switch
        {
            RoomDefinition.RoomCategory.Housing => Config.HousingCostMultiplier,
            RoomDefinition.RoomCategory.LifeSupport => Config.LifeSupportCostMultiplier,
            RoomDefinition.RoomCategory.Work => Config.WorkCostMultiplier,
            RoomDefinition.RoomCategory.Comfort => Config.ComfortCostMultiplier,
            RoomDefinition.RoomCategory.Utility => Config.UtilityCostMultiplier,
            _ => 1.0f
        };
    }

    // -------------------------------------------------------------------------
    // Income Breakdown (for HUD tooltip display)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the individual income components for tooltip/breakdown display.
    /// </summary>
    /// <returns>Tuple of (baseIncome, citizenIncome, workBonus, happinessMult, total).</returns>
    public (float baseIncome, float citizenIncome, float workBonus, float happinessMult, int total) GetIncomeBreakdown()
    {
        float baseIncome = Config.BaseStationIncome;
        float citizenIncome = Config.PassiveIncomePerCitizen * MathF.Sqrt(_citizenCount);
        float workBonus = _workingCitizenCount * Config.WorkBonusMultiplier;
        float happinessMult = 1.0f + (_currentHappiness * (Config.HappinessMultiplierCap - 1.0f));
        int total = Mathf.RoundToInt((baseIncome + citizenIncome + workBonus) * happinessMult);
        return (baseIncome, citizenIncome, workBonus, happinessMult, total);
    }

    // -------------------------------------------------------------------------
    // Working Citizen Tracking (Phase 9 -- bridges citizen visits to economy)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Called when a citizen enters a room. If the room is a Work category room,
    /// tracks the citizen in the working set and updates the count.
    /// </summary>
    private void OnCitizenEnteredRoom(string citizenName, int flatSegmentIndex)
    {
        if (IsWorkRoom(flatSegmentIndex))
        {
            _workingCitizens.Add(citizenName);
            _workingCitizenCount = _workingCitizens.Count;
        }
    }

    /// <summary>
    /// Called when a citizen exits a room. Removes from working set if present.
    /// Uses set membership (not room lookup) to handle demolished-room race condition:
    /// the room may no longer exist, but the citizen was counted as working.
    /// </summary>
    private void OnCitizenExitedRoom(string citizenName, int flatSegmentIndex)
    {
        if (_workingCitizens.Remove(citizenName))
        {
            _workingCitizenCount = _workingCitizens.Count;
        }
    }

    /// <summary>
    /// Checks whether the room at the given flat segment index is a Work category room.
    /// </summary>
    private bool IsWorkRoom(int flatSegmentIndex)
    {
        var room = Build.BuildManager.Instance?.GetPlacedRoom(flatSegmentIndex);
        return room?.Definition.Category == RoomDefinition.RoomCategory.Work;
    }

    // -------------------------------------------------------------------------
    // Phase 5/7 Integration Stubs
    // -------------------------------------------------------------------------

    /// <summary>
    /// Set the total citizen count. Called by CitizenManager (Phase 5) when citizens arrive/depart.
    /// </summary>
    public void SetCitizenCount(int count) { _citizenCount = Mathf.Max(0, count); }

    /// <summary>
    /// Set the number of citizens assigned to work rooms. Called by CitizenManager (Phase 5).
    /// </summary>
    public void SetWorkingCitizenCount(int count) { _workingCitizenCount = Mathf.Max(0, count); }

    /// <summary>
    /// Set the current station happiness (0.0 to 1.0). Called by HappinessManager (Phase 7).
    /// </summary>
    public void SetHappiness(float happiness) { _currentHappiness = Mathf.Clamp(happiness, 0f, 1f); }

    // -------------------------------------------------------------------------
    // Save/Load API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Restores the credit balance from save data and notifies UI.
    /// </summary>
    public void RestoreCredits(int credits)
    {
        _credits = credits;
        _workingCitizens.Clear();
        _workingCitizenCount = 0;
        GameEvents.Instance?.EmitCreditsChanged(_credits);
    }
}
