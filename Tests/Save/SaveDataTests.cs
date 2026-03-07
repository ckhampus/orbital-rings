namespace OrbitalRings.Tests.Save;

using System.Collections.Generic;
using System.Text.Json;
using Chickensoft.GoDotTest;
using Godot;
using OrbitalRings.Autoloads;
using Shouldly;

public class SaveDataTests : TestClass
{
    public SaveDataTests(Node testScene) : base(testScene) { }

    private static SaveData CreateTestSaveData()
    {
        return new SaveData
        {
            Version = 3,
            Credits = 1500,
            Happiness = 0f,
            CrossedMilestoneCount = 3,
            LifetimeHappiness = 42,
            Mood = 0.65f,
            MoodBaseline = 0.10f,
            UnlockedRooms = new List<string> { "hydroponics", "solar_panel" },
            PlacedRooms = new List<SavedRoom>
            {
                new() { RoomId = "housing_basic", Row = 0, StartPos = 2, SegmentCount = 2, Cost = 129 },
                new() { RoomId = "solar_panel", Row = 1, StartPos = 5, SegmentCount = 1, Cost = 85 },
            },
            Citizens = new List<SavedCitizen>
            {
                new()
                {
                    Name = "Ada", BodyType = 1,
                    PrimaryR = 0.8f, PrimaryG = 0.3f, PrimaryB = 0.1f,
                    SecondaryR = 0.2f, SecondaryG = 0.5f, SecondaryB = 0.9f,
                    WalkwayAngle = 1.57f, Direction = -1f,
                    CurrentWishId = "wish_hydroponics",
                    HomeSegmentIndex = 2
                },
                new()
                {
                    Name = "Bob", BodyType = 0,
                    PrimaryR = 0.4f, PrimaryG = 0.6f, PrimaryB = 0.2f,
                    SecondaryR = 0.7f, SecondaryG = 0.1f, SecondaryB = 0.3f,
                    WalkwayAngle = 3.14f, Direction = 1f,
                    CurrentWishId = null,
                    HomeSegmentIndex = null
                },
            },
            ActiveWishes = new Dictionary<string, string> { { "Ada", "wish_hydroponics" } },
            PlacedRoomTypes = new Dictionary<string, int> { { "housing_basic", 1 }, { "solar_panel", 1 } },
        };
    }

    // --- SAVE-01: v3 Round-Trip ---

    [Test]
    public void V3RoundTripPreservesAllFields()
    {
        var original = CreateTestSaveData();
        var options = new JsonSerializerOptions { WriteIndented = true };

        string json = JsonSerializer.Serialize(original, options);
        var restored = JsonSerializer.Deserialize<SaveData>(json);

        // Top-level scalar fields
        restored.Version.ShouldBe(3);
        restored.Credits.ShouldBe(1500);
        restored.Happiness.ShouldBe(0f);
        restored.CrossedMilestoneCount.ShouldBe(3);
        restored.LifetimeHappiness.ShouldBe(42);
        restored.Mood.ShouldBe(0.65f);
        restored.MoodBaseline.ShouldBe(0.10f);

        // UnlockedRooms
        restored.UnlockedRooms.ShouldBe(new List<string> { "hydroponics", "solar_panel" });

        // PlacedRooms
        restored.PlacedRooms.Count.ShouldBe(2);

        restored.PlacedRooms[0].RoomId.ShouldBe("housing_basic");
        restored.PlacedRooms[0].Row.ShouldBe(0);
        restored.PlacedRooms[0].StartPos.ShouldBe(2);
        restored.PlacedRooms[0].SegmentCount.ShouldBe(2);
        restored.PlacedRooms[0].Cost.ShouldBe(129);

        restored.PlacedRooms[1].RoomId.ShouldBe("solar_panel");
        restored.PlacedRooms[1].Row.ShouldBe(1);
        restored.PlacedRooms[1].StartPos.ShouldBe(5);
        restored.PlacedRooms[1].SegmentCount.ShouldBe(1);
        restored.PlacedRooms[1].Cost.ShouldBe(85);

        // Citizens[0] -- with HomeSegmentIndex set
        restored.Citizens.Count.ShouldBe(2);

        restored.Citizens[0].Name.ShouldBe("Ada");
        restored.Citizens[0].BodyType.ShouldBe(1);
        restored.Citizens[0].PrimaryR.ShouldBe(0.8f);
        restored.Citizens[0].PrimaryG.ShouldBe(0.3f);
        restored.Citizens[0].PrimaryB.ShouldBe(0.1f);
        restored.Citizens[0].SecondaryR.ShouldBe(0.2f);
        restored.Citizens[0].SecondaryG.ShouldBe(0.5f);
        restored.Citizens[0].SecondaryB.ShouldBe(0.9f);
        restored.Citizens[0].WalkwayAngle.ShouldBe(1.57f);
        restored.Citizens[0].Direction.ShouldBe(-1f);
        restored.Citizens[0].CurrentWishId.ShouldBe("wish_hydroponics");
        restored.Citizens[0].HomeSegmentIndex.ShouldBe(2);

        // Citizens[1] -- with HomeSegmentIndex null
        restored.Citizens[1].Name.ShouldBe("Bob");
        restored.Citizens[1].BodyType.ShouldBe(0);
        restored.Citizens[1].PrimaryR.ShouldBe(0.4f);
        restored.Citizens[1].PrimaryG.ShouldBe(0.6f);
        restored.Citizens[1].PrimaryB.ShouldBe(0.2f);
        restored.Citizens[1].SecondaryR.ShouldBe(0.7f);
        restored.Citizens[1].SecondaryG.ShouldBe(0.1f);
        restored.Citizens[1].SecondaryB.ShouldBe(0.3f);
        restored.Citizens[1].WalkwayAngle.ShouldBe(3.14f);
        restored.Citizens[1].Direction.ShouldBe(1f);
        restored.Citizens[1].CurrentWishId.ShouldBeNull();
        restored.Citizens[1].HomeSegmentIndex.ShouldBeNull();

        // ActiveWishes
        restored.ActiveWishes.Count.ShouldBe(1);
        restored.ActiveWishes["Ada"].ShouldBe("wish_hydroponics");

        // PlacedRoomTypes
        restored.PlacedRoomTypes.Count.ShouldBe(2);
        restored.PlacedRoomTypes["housing_basic"].ShouldBe(1);
        restored.PlacedRoomTypes["solar_panel"].ShouldBe(1);
    }

    // --- SAVE-02: v1 Backward Compat ---

    [Test]
    public void V1JsonDeserializesWithCorrectDefaults()
    {
        var v1Json = """
            {
                "Version": 1,
                "Credits": 500,
                "Happiness": 0.35,
                "CrossedMilestoneCount": 1,
                "UnlockedRooms": ["hydroponics"],
                "PlacedRooms": [
                    {
                        "RoomId": "housing_basic",
                        "Row": 0,
                        "StartPos": 0,
                        "SegmentCount": 1,
                        "Cost": 70
                    }
                ],
                "Citizens": [
                    {
                        "Name": "Ada",
                        "BodyType": 1,
                        "PrimaryR": 0.8,
                        "PrimaryG": 0.3,
                        "PrimaryB": 0.1,
                        "SecondaryR": 0.2,
                        "SecondaryG": 0.5,
                        "SecondaryB": 0.9,
                        "WalkwayAngle": 1.57,
                        "Direction": -1.0,
                        "CurrentWishId": "wish_hydroponics"
                    }
                ],
                "ActiveWishes": { "Ada": "wish_hydroponics" },
                "PlacedRoomTypes": { "housing_basic": 1 }
            }
            """;

        var data = JsonSerializer.Deserialize<SaveData>(v1Json);

        // v1 fields present -- should deserialize correctly
        data.Version.ShouldBe(1);
        data.Credits.ShouldBe(500);
        data.Happiness.ShouldBe(0.35f);
        data.CrossedMilestoneCount.ShouldBe(1);

        data.UnlockedRooms.Count.ShouldBe(1);
        data.UnlockedRooms[0].ShouldBe("hydroponics");

        data.PlacedRooms.Count.ShouldBe(1);
        data.PlacedRooms[0].RoomId.ShouldBe("housing_basic");
        data.PlacedRooms[0].Row.ShouldBe(0);
        data.PlacedRooms[0].StartPos.ShouldBe(0);
        data.PlacedRooms[0].SegmentCount.ShouldBe(1);
        data.PlacedRooms[0].Cost.ShouldBe(70);

        data.Citizens.Count.ShouldBe(1);
        data.Citizens[0].Name.ShouldBe("Ada");
        data.Citizens[0].BodyType.ShouldBe(1);
        data.Citizens[0].PrimaryR.ShouldBe(0.8f);
        data.Citizens[0].PrimaryG.ShouldBe(0.3f);
        data.Citizens[0].PrimaryB.ShouldBe(0.1f);
        data.Citizens[0].SecondaryR.ShouldBe(0.2f);
        data.Citizens[0].SecondaryG.ShouldBe(0.5f);
        data.Citizens[0].SecondaryB.ShouldBe(0.9f);
        data.Citizens[0].WalkwayAngle.ShouldBe(1.57f);
        data.Citizens[0].Direction.ShouldBe(-1f);
        data.Citizens[0].CurrentWishId.ShouldBe("wish_hydroponics");

        data.ActiveWishes.Count.ShouldBe(1);
        data.ActiveWishes["Ada"].ShouldBe("wish_hydroponics");

        data.PlacedRoomTypes.Count.ShouldBe(1);
        data.PlacedRoomTypes["housing_basic"].ShouldBe(1);

        // v2 fields MISSING from v1 JSON -- should get C# defaults
        data.LifetimeHappiness.ShouldBe(0);
        data.Mood.ShouldBe(0f);
        data.MoodBaseline.ShouldBe(0f);

        // v3 field MISSING from v1 JSON -- should get null
        data.Citizens[0].HomeSegmentIndex.ShouldBeNull();
    }

    // --- SAVE-03: v2 Backward Compat ---

    [Test]
    public void V2JsonDeserializesWithCorrectDefaults()
    {
        var v2Json = """
            {
                "Version": 2,
                "Credits": 800,
                "Happiness": 0.50,
                "CrossedMilestoneCount": 2,
                "LifetimeHappiness": 25,
                "Mood": 0.40,
                "MoodBaseline": 0.05,
                "UnlockedRooms": ["hydroponics", "solar_panel"],
                "PlacedRooms": [
                    {
                        "RoomId": "hydroponics",
                        "Row": 0,
                        "StartPos": 3,
                        "SegmentCount": 2,
                        "Cost": 100
                    }
                ],
                "Citizens": [
                    {
                        "Name": "Charlie",
                        "BodyType": 2,
                        "PrimaryR": 0.6,
                        "PrimaryG": 0.4,
                        "PrimaryB": 0.5,
                        "SecondaryR": 0.3,
                        "SecondaryG": 0.7,
                        "SecondaryB": 0.2,
                        "WalkwayAngle": 2.10,
                        "Direction": 1.0,
                        "CurrentWishId": "wish_solar"
                    }
                ],
                "ActiveWishes": { "Charlie": "wish_solar" },
                "PlacedRoomTypes": { "hydroponics": 1 }
            }
            """;

        var data = JsonSerializer.Deserialize<SaveData>(v2Json);

        // v2 fields present -- should deserialize correctly
        data.Version.ShouldBe(2);
        data.Credits.ShouldBe(800);
        data.Happiness.ShouldBe(0.50f);
        data.CrossedMilestoneCount.ShouldBe(2);
        data.LifetimeHappiness.ShouldBe(25);
        data.Mood.ShouldBe(0.40f);
        data.MoodBaseline.ShouldBe(0.05f);

        data.UnlockedRooms.Count.ShouldBe(2);
        data.UnlockedRooms[0].ShouldBe("hydroponics");
        data.UnlockedRooms[1].ShouldBe("solar_panel");

        data.PlacedRooms.Count.ShouldBe(1);
        data.PlacedRooms[0].RoomId.ShouldBe("hydroponics");
        data.PlacedRooms[0].Row.ShouldBe(0);
        data.PlacedRooms[0].StartPos.ShouldBe(3);
        data.PlacedRooms[0].SegmentCount.ShouldBe(2);
        data.PlacedRooms[0].Cost.ShouldBe(100);

        data.Citizens.Count.ShouldBe(1);
        data.Citizens[0].Name.ShouldBe("Charlie");
        data.Citizens[0].BodyType.ShouldBe(2);
        data.Citizens[0].PrimaryR.ShouldBe(0.6f);
        data.Citizens[0].PrimaryG.ShouldBe(0.4f);
        data.Citizens[0].PrimaryB.ShouldBe(0.5f);
        data.Citizens[0].SecondaryR.ShouldBe(0.3f);
        data.Citizens[0].SecondaryG.ShouldBe(0.7f);
        data.Citizens[0].SecondaryB.ShouldBe(0.2f);
        data.Citizens[0].WalkwayAngle.ShouldBe(2.10f);
        data.Citizens[0].Direction.ShouldBe(1f);
        data.Citizens[0].CurrentWishId.ShouldBe("wish_solar");

        data.ActiveWishes.Count.ShouldBe(1);
        data.ActiveWishes["Charlie"].ShouldBe("wish_solar");

        data.PlacedRoomTypes.Count.ShouldBe(1);
        data.PlacedRoomTypes["hydroponics"].ShouldBe(1);

        // v3 field MISSING from v2 JSON -- should get null
        data.Citizens[0].HomeSegmentIndex.ShouldBeNull();
    }

    // --- Edge Cases ---

    [Test]
    public void EmptyCollectionsRoundTripAsNonNull()
    {
        var empty = new SaveData
        {
            Version = 3,
            Credits = 0,
            // All collections left at their initialized-empty defaults
        };
        var options = new JsonSerializerOptions { WriteIndented = true };

        string json = JsonSerializer.Serialize(empty, options);
        var restored = JsonSerializer.Deserialize<SaveData>(json);

        restored.PlacedRooms.ShouldNotBeNull();
        restored.PlacedRooms.ShouldBeEmpty();
        restored.Citizens.ShouldNotBeNull();
        restored.Citizens.ShouldBeEmpty();
        restored.ActiveWishes.ShouldNotBeNull();
        restored.ActiveWishes.ShouldBeEmpty();
        restored.PlacedRoomTypes.ShouldNotBeNull();
        restored.PlacedRoomTypes.ShouldBeEmpty();
        restored.UnlockedRooms.ShouldNotBeNull();
        restored.UnlockedRooms.ShouldBeEmpty();
    }
}
