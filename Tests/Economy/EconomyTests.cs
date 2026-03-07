namespace OrbitalRings.Tests.Economy;

using Chickensoft.GoDotTest;
using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Data;
using OrbitalRings.Tests.Infrastructure;
using Shouldly;

public class EconomyTests : GameTestClass
{
    public EconomyTests(Node testScene) : base(testScene) { }

    private static EconomyManager Econ => EconomyManager.Instance;

    private static RoomDefinition HousingRoom() =>
        new() { Category = RoomDefinition.RoomCategory.Housing };

    private static RoomDefinition LifeSupportRoom() =>
        new() { Category = RoomDefinition.RoomCategory.LifeSupport };

    private static RoomDefinition WorkRoom() =>
        new() { Category = RoomDefinition.RoomCategory.Work };

    private static RoomDefinition ComfortRoom() =>
        new() { Category = RoomDefinition.RoomCategory.Comfort };

    private static RoomDefinition UtilityRoom() =>
        new() { Category = RoomDefinition.RoomCategory.Utility };

    // --- Room Cost: Housing --- (ECON-01)

    [Test]
    public void RoomCostHousing1SegInner()
    {
        // 100 * 0.7 * 1.0 * 1.0 = 70
        Econ.CalculateRoomCost(HousingRoom(), 1, false).ShouldBe(70);
    }

    [Test]
    public void RoomCostHousing1SegOuter()
    {
        // 100 * 0.7 * 1.0 * 1.1 = 77
        Econ.CalculateRoomCost(HousingRoom(), 1, true).ShouldBe(77);
    }

    [Test]
    public void RoomCostHousing2SegInner()
    {
        // 100 * 0.7 * (2 * 0.92) * 1.0 = 128.8 -> 129
        Econ.CalculateRoomCost(HousingRoom(), 2, false).ShouldBe(129);
    }

    [Test]
    public void RoomCostHousing2SegOuter()
    {
        // 100 * 0.7 * (2 * 0.92) * 1.1 = 141.68 -> 142
        Econ.CalculateRoomCost(HousingRoom(), 2, true).ShouldBe(142);
    }

    [Test]
    public void RoomCostHousing3SegInner()
    {
        // 100 * 0.7 * (3 * 0.92^2) * 1.0 = 177.744 -> 178
        Econ.CalculateRoomCost(HousingRoom(), 3, false).ShouldBe(178);
    }

    [Test]
    public void RoomCostHousing3SegOuter()
    {
        // 100 * 0.7 * (3 * 0.92^2) * 1.1 = 195.5184 -> 196
        Econ.CalculateRoomCost(HousingRoom(), 3, true).ShouldBe(196);
    }

    // --- Room Cost: LifeSupport --- (ECON-01)

    [Test]
    public void RoomCostLifeSupport1SegInner()
    {
        // 100 * 0.85 * 1.0 * 1.0 = 85
        Econ.CalculateRoomCost(LifeSupportRoom(), 1, false).ShouldBe(85);
    }

    [Test]
    public void RoomCostLifeSupport1SegOuter()
    {
        // 100 * 0.85 * 1.0 * 1.1 = 93.5 -> 94
        Econ.CalculateRoomCost(LifeSupportRoom(), 1, true).ShouldBe(94);
    }

    [Test]
    public void RoomCostLifeSupport2SegInner()
    {
        // 100 * 0.85 * (2 * 0.92) * 1.0 = 156.4 -> 156
        Econ.CalculateRoomCost(LifeSupportRoom(), 2, false).ShouldBe(156);
    }

    [Test]
    public void RoomCostLifeSupport2SegOuter()
    {
        // 100 * 0.85 * (2 * 0.92) * 1.1 = 172.04 -> 172
        Econ.CalculateRoomCost(LifeSupportRoom(), 2, true).ShouldBe(172);
    }

    [Test]
    public void RoomCostLifeSupport3SegInner()
    {
        // 100 * 0.85 * (3 * 0.92^2) * 1.0 = 215.832 -> 216
        Econ.CalculateRoomCost(LifeSupportRoom(), 3, false).ShouldBe(216);
    }

    [Test]
    public void RoomCostLifeSupport3SegOuter()
    {
        // 100 * 0.85 * (3 * 0.92^2) * 1.1 = 237.4152 -> 237
        Econ.CalculateRoomCost(LifeSupportRoom(), 3, true).ShouldBe(237);
    }

    // --- Room Cost: Work --- (ECON-01)

    [Test]
    public void RoomCostWork1SegInner()
    {
        // 100 * 1.0 * 1.0 * 1.0 = 100
        Econ.CalculateRoomCost(WorkRoom(), 1, false).ShouldBe(100);
    }

    [Test]
    public void RoomCostWork1SegOuter()
    {
        // 100 * 1.0 * 1.0 * 1.1 = 110
        Econ.CalculateRoomCost(WorkRoom(), 1, true).ShouldBe(110);
    }

    [Test]
    public void RoomCostWork2SegInner()
    {
        // 100 * 1.0 * (2 * 0.92) * 1.0 = 184
        Econ.CalculateRoomCost(WorkRoom(), 2, false).ShouldBe(184);
    }

    [Test]
    public void RoomCostWork2SegOuter()
    {
        // 100 * 1.0 * (2 * 0.92) * 1.1 = 202.4 -> 202
        Econ.CalculateRoomCost(WorkRoom(), 2, true).ShouldBe(202);
    }

    [Test]
    public void RoomCostWork3SegInner()
    {
        // 100 * 1.0 * (3 * 0.92^2) * 1.0 = 253.92 -> 254
        Econ.CalculateRoomCost(WorkRoom(), 3, false).ShouldBe(254);
    }

    [Test]
    public void RoomCostWork3SegOuter()
    {
        // 100 * 1.0 * (3 * 0.92^2) * 1.1 = 279.312 -> 279
        Econ.CalculateRoomCost(WorkRoom(), 3, true).ShouldBe(279);
    }

    // --- Room Cost: Comfort --- (ECON-01)

    [Test]
    public void RoomCostComfort1SegInner()
    {
        // 100 * 1.15 * 1.0 * 1.0 = 115
        Econ.CalculateRoomCost(ComfortRoom(), 1, false).ShouldBe(115);
    }

    [Test]
    public void RoomCostComfort1SegOuter()
    {
        // 100 * 1.15 * 1.0 * 1.1 = 126.5 -> 126 (banker's rounding)
        Econ.CalculateRoomCost(ComfortRoom(), 1, true).ShouldBe(126);
    }

    [Test]
    public void RoomCostComfort2SegInner()
    {
        // 100 * 1.15 * (2 * 0.92) * 1.0 = 211.6 -> 212
        Econ.CalculateRoomCost(ComfortRoom(), 2, false).ShouldBe(212);
    }

    [Test]
    public void RoomCostComfort2SegOuter()
    {
        // 100 * 1.15 * (2 * 0.92) * 1.1 = 232.76 -> 233
        Econ.CalculateRoomCost(ComfortRoom(), 2, true).ShouldBe(233);
    }

    [Test]
    public void RoomCostComfort3SegInner()
    {
        // 100 * 1.15 * (3 * 0.92^2) * 1.0 = 292.008 -> 292
        Econ.CalculateRoomCost(ComfortRoom(), 3, false).ShouldBe(292);
    }

    [Test]
    public void RoomCostComfort3SegOuter()
    {
        // 100 * 1.15 * (3 * 0.92^2) * 1.1 = 321.2088 -> 321
        Econ.CalculateRoomCost(ComfortRoom(), 3, true).ShouldBe(321);
    }

    // --- Room Cost: Utility --- (ECON-01)

    [Test]
    public void RoomCostUtility1SegInner()
    {
        // 100 * 1.3 * 1.0 * 1.0 = 130
        Econ.CalculateRoomCost(UtilityRoom(), 1, false).ShouldBe(130);
    }

    [Test]
    public void RoomCostUtility1SegOuter()
    {
        // 100 * 1.3 * 1.0 * 1.1 = 143
        Econ.CalculateRoomCost(UtilityRoom(), 1, true).ShouldBe(143);
    }

    [Test]
    public void RoomCostUtility2SegInner()
    {
        // 100 * 1.3 * (2 * 0.92) * 1.0 = 239.2 -> 239
        Econ.CalculateRoomCost(UtilityRoom(), 2, false).ShouldBe(239);
    }

    [Test]
    public void RoomCostUtility2SegOuter()
    {
        // 100 * 1.3 * (2 * 0.92) * 1.1 = 263.12 -> 263
        Econ.CalculateRoomCost(UtilityRoom(), 2, true).ShouldBe(263);
    }

    [Test]
    public void RoomCostUtility3SegInner()
    {
        // 100 * 1.3 * (3 * 0.92^2) * 1.0 = 330.096 -> 330
        Econ.CalculateRoomCost(UtilityRoom(), 3, false).ShouldBe(330);
    }

    [Test]
    public void RoomCostUtility3SegOuter()
    {
        // 100 * 1.3 * (3 * 0.92^2) * 1.1 = 363.1056 -> 363
        Econ.CalculateRoomCost(UtilityRoom(), 3, true).ShouldBe(363);
    }

    // --- Room Cost: BaseCostOverride --- (ECON-01)

    [Test]
    public void RoomCostOverrideUsesCustomBase()
    {
        // 150 * 0.7 * 1.0 * 1.0 = 105
        var room = HousingRoom();
        room.BaseCostOverride = 150;
        Econ.CalculateRoomCost(room, 1, false).ShouldBe(105);
    }

    [Test]
    public void RoomCostOverrideMultiSegOuter()
    {
        // 200 * 1.0 * (2 * 0.92) * 1.1 = RoundToInt(404.8) = 405
        var room = WorkRoom();
        room.BaseCostOverride = 200;
        Econ.CalculateRoomCost(room, 2, true).ShouldBe(405);
    }

    // --- Tick Income --- (ECON-02)

    [Test]
    public void TickIncomeQuietTier()
    {
        // (1.0 + 2.0 * sqrt(10) + 3 * 1.25) * 1.0 = 11.0746 -> 11
        Econ.SetCitizenCount(10);
        Econ.SetWorkingCitizenCount(3);
        Econ.SetMoodTier(MoodTier.Quiet);
        Econ.CalculateTickIncome().ShouldBe(11);
    }

    [Test]
    public void TickIncomeCozyTier()
    {
        // (1.0 + 2.0 * sqrt(10) + 3 * 1.25) * 1.1 = 12.182 -> 12
        Econ.SetCitizenCount(10);
        Econ.SetWorkingCitizenCount(3);
        Econ.SetMoodTier(MoodTier.Cozy);
        Econ.CalculateTickIncome().ShouldBe(12);
    }

    [Test]
    public void TickIncomeLivelyTier()
    {
        // (1.0 + 2.0 * sqrt(10) + 3 * 1.25) * 1.2 = 13.2895 -> 13
        Econ.SetCitizenCount(10);
        Econ.SetWorkingCitizenCount(3);
        Econ.SetMoodTier(MoodTier.Lively);
        Econ.CalculateTickIncome().ShouldBe(13);
    }

    [Test]
    public void TickIncomeVibrantTier()
    {
        // (1.0 + 2.0 * sqrt(10) + 3 * 1.25) * 1.3 = 14.3969 -> 14
        Econ.SetCitizenCount(10);
        Econ.SetWorkingCitizenCount(3);
        Econ.SetMoodTier(MoodTier.Vibrant);
        Econ.CalculateTickIncome().ShouldBe(14);
    }

    [Test]
    public void TickIncomeRadiantTier()
    {
        // (1.0 + 2.0 * sqrt(10) + 3 * 1.25) * 1.4 = 15.5044 -> 16
        Econ.SetCitizenCount(10);
        Econ.SetWorkingCitizenCount(3);
        Econ.SetMoodTier(MoodTier.Radiant);
        Econ.CalculateTickIncome().ShouldBe(16);
    }

    // --- Tick Income: Edge Cases --- (ECON-02)

    [Test]
    public void TickIncomeZeroCitizensReturnsBase()
    {
        // (1.0 + 0 + 0) * 1.0 = 1
        Econ.SetCitizenCount(0);
        Econ.SetWorkingCitizenCount(0);
        Econ.SetMoodTier(MoodTier.Quiet);
        Econ.CalculateTickIncome().ShouldBe(1);
    }

    // --- Demolish Refund --- (ECON-03)

    [Test]
    public void DemolishRefundEvenCost()
    {
        // 100 * 0.5 = 50
        Econ.CalculateDemolishRefund(100).ShouldBe(50);
    }

    [Test]
    public void DemolishRefundOddCostEvenResult()
    {
        // 70 * 0.5 = 35
        Econ.CalculateDemolishRefund(70).ShouldBe(35);
    }

    [Test]
    public void DemolishRefundLargeCost()
    {
        // 184 * 0.5 = 92
        Econ.CalculateDemolishRefund(184).ShouldBe(92);
    }

    [Test]
    public void DemolishRefundBankersRounding()
    {
        // 77 * 0.5 = 38.5 -> 38 (banker's rounding: rounds to even)
        Econ.CalculateDemolishRefund(77).ShouldBe(38);
    }
}
