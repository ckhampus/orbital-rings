# Testing Patterns

**Analysis Date:** 2026-03-02

## Test Framework

**Status:** Testing infrastructure not yet implemented

**Recommended Setup:**
- Runner: NUnit 4.x or xUnit with Godot integration
- Assertion Library: Fluent Assertions (NuGet: FluentAssertions)
- Mocking: Moq (NuGet: Moq) or NSubstitute
- Integration: GodotTestDriver or GodotSharp.Tests

**Build/Deploy:**
- Project: Create `[ProjectName].Tests.csproj` as separate test project
- Reference main project: `<ProjectReference Include="../[MainProject].csproj" />`
- Target: net8.0 (matching main project)

**Run Commands:**
```bash
dotnet test                    # Run all tests
dotnet test --watch           # Watch mode (re-run on changes)
dotnet test --logger "console;verbosity=detailed"  # Verbose output
dotnet test /p:CollectCoverage=true  # Code coverage (requires coverlet)
dotnet test --filter "Category=Unit"  # Run specific category
```

**Code Coverage:**
- Tool: Coverlet (NuGet: coverlet.collector)
- Coverage report format: OpenCover or Cobertura
- Target: Minimum 80% coverage for critical systems (Ring, Citizens, Economy)
- View: Use `dotnet reportgenerator` or IDE integration

## Test File Organization

**Location:**
- Co-located with source in separate `.Tests` project
- Directory structure mirrors source structure

**Example Structure:**
```
/Orbital Rings.Tests/
├── Core/
│   ├── RingTests.cs
│   └── CitizenTests.cs
├── Systems/
│   ├── EconomySystemTests.cs
│   ├── HappinessSystemTests.cs
│   └── WishSystemTests.cs
├── Gameplay/
│   └── RoomPlacementTests.cs
├── Procedural/
│   └── RoomGeneratorTests.cs
└── TestFixtures/
    ├── CitizenFactory.cs
    ├── RoomFactory.cs
    └── GameStateBuilder.cs
```

**Naming:**
- Test class: `[ClassUnderTest]Tests` (e.g., `RingTests`, `EconomySystemTests`)
- Test method: `[MethodName]_[Scenario]_[ExpectedOutcome]` or `Should[ExpectedOutcome]When[Scenario]`

**Examples:**
```csharp
[TestClass]
public class RingTests
{
  [TestMethod]
  public void BuildRoom_WhenSegmentAvailable_PlacesRoom()
  {
    // Test implementation
  }

  [TestMethod]
  public void BuildRoom_WhenSegmentOccupied_ThrowsInvalidOperation()
  {
    // Test implementation
  }
}
```

## Test Structure

**Suite Organization:**
```csharp
[TestClass]
public class RingTests
{
  private Ring _ring;
  private Room _testRoom;

  [TestInitialize]  // Runs before each test
  public void Setup()
  {
    _ring = new Ring();
    _testRoom = new Room { Type = RoomType.Housing, Size = 1 };
  }

  [TestCleanup]  // Runs after each test
  public void Teardown()
  {
    _ring?.Dispose();
  }

  [TestMethod]
  public void Example_WhenCondition_ExpectedResult()
  {
    // Arrange
    var segment = 1;

    // Act
    _ring.BuildRoom(_testRoom, segment);

    // Assert
    Assert.IsTrue(_ring.IsSegmentOccupied(segment));
    Assert.AreEqual(_testRoom, _ring.GetRoomAt(segment));
  }
}
```

**Patterns:**
- **Arrange**: Set up test data, mocks, and system state
- **Act**: Execute the method being tested
- **Assert**: Verify the result matches expectations
- **Setup/Teardown**: Manage test lifecycle and resource cleanup

## Test Naming Convention

Use descriptive names that explain the behavior being tested:

**Pattern 1: Descriptive**
```csharp
[TestMethod]
public void BuildRoom_WithValidParameters_SuccessfullyBuildsRoom()
{
  // Clear intent about what succeeds
}
```

**Pattern 2: Should/When (Preferred)**
```csharp
[TestMethod]
public void ShouldPlaceRoomInAvailableSegment()
{
  // Simple, readable
}
```

**Pattern 3: Behavior-Driven**
```csharp
[TestMethod]
[DataRow(1)]
[DataRow(2)]
public void BuildRoom_WithVariousSegments_ReturnsSuccess(int segment)
{
  // Parameterized test example
}
```

## Mocking

**Framework:** Moq (primary recommendation)

**Patterns:**

1. **Creating Mocks:**
```csharp
[TestMethod]
public void BuildRoom_CallsWishSystemOnCompletion()
{
  // Arrange
  var mockWishSystem = new Mock<IWishSystem>();
  var ring = new Ring(mockWishSystem.Object);
  var room = new Room { Type = RoomType.Housing };

  // Act
  ring.BuildRoom(room, 1);

  // Assert
  mockWishSystem.Verify(
    x => x.OnRoomBuilt(room),
    Times.Once
  );
}
```

2. **Setting Up Return Values:**
```csharp
var mockCitizenFactory = new Mock<ICitizenFactory>();
mockCitizenFactory
  .Setup(f => f.CreateCitizen())
  .Returns(new Citizen { Name = "TestCitizen" });
```

3. **Verifying Calls:**
```csharp
mockRepository.Verify(
  r => r.Save(It.IsAny<Room>()),
  Times.AtLeastOnce,
  "Rooms should be saved"
);
```

**What to Mock:**
- External dependencies (databases, APIs, file systems)
- Time-dependent operations (use ISystemClock interface)
- Complex subsystems when testing isolation
- Input/output operations

**What NOT to Mock:**
- Value objects (Room, Citizen basic properties)
- Simple data structures
- The class being tested (test the real thing)
- Logic that IS the feature being tested

## Fixtures and Factories

**Test Data Builders:**
Location: `/Orbital Rings.Tests/TestFixtures/`

```csharp
// RoomFactory.cs
public static class RoomFactory
{
  public static Room CreateHousingRoom(int size = 1)
  {
    return new Room
    {
      Type = RoomType.Housing,
      Size = size,
      BuildCost = 100 * size
    };
  }

  public static Room CreateWorkRoom(int size = 1)
  {
    return new Room
    {
      Type = RoomType.Work,
      Size = size,
      BuildCost = 150 * size
    };
  }
}

// CitizenFactory.cs
public static class CitizenFactory
{
  public static Citizen CreateBasicCitizen()
  {
    return new Citizen
    {
      Name = "TestCitizen",
      Happiness = 50,
      Traits = new[] { CitizenTrait.Social }
    };
  }

  public static Citizen CreateWithTraits(params CitizenTrait[] traits)
  {
    return new Citizen
    {
      Name = "TraitedCitizen",
      Happiness = 50,
      Traits = traits
    };
  }
}

// GameStateBuilder.cs (Fluent Builder)
public class GameStateBuilder
{
  private GameState _state = new GameState();

  public GameStateBuilder WithCitizens(int count)
  {
    for (int i = 0; i < count; i++)
    {
      _state.AddCitizen(CitizenFactory.CreateBasicCitizen());
    }
    return this;
  }

  public GameStateBuilder WithCredits(int amount)
  {
    _state.Credits = amount;
    return this;
  }

  public GameState Build() => _state;
}
```

**Usage:**
```csharp
[TestMethod]
public void Economy_WithMultipleCitizens_GeneratesIncome()
{
  // Arrange
  var gameState = new GameStateBuilder()
    .WithCitizens(5)
    .WithCredits(1000)
    .Build();

  // Act
  var newCredits = gameState.CalculateIncomePerTick();

  // Assert
  Assert.IsTrue(newCredits > 0);
}
```

## Coverage

**Target Coverage:**
- Overall: 70% minimum
- Critical systems:
  - Ring/Segment logic: 85%+
  - Economy system: 80%+
  - Happiness/Wish system: 80%+
  - Procedural generation: 70%+

**View Coverage:**
```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
# View with reportgenerator
reportgenerator -reports:"**/coverage.opencover.xml" -targetdir:"coverage-report"
```

**Coverage Tools:**
- OpenCover (via Coverlet)
- IDE integration: Use Roslyn test explorer in Visual Studio/VS Code

**Gaps to Avoid:**
- Don't mock everything - lose real integration testing value
- Don't test trivial getters/setters
- Don't aim for 100% - diminishing returns on edge cases

## Test Types

**Unit Tests:**
- Scope: Single class or method in isolation
- Mocking: Mock all external dependencies
- Speed: Milliseconds (entire suite < 5 seconds)
- Location: `[ClassName]Tests.cs`

**Example:**
```csharp
[TestClass]
public class RingTests
{
  [TestMethod]
  public void IsSegmentOccupied_WhenEmpty_ReturnsFalse()
  {
    var ring = new Ring();
    Assert.IsFalse(ring.IsSegmentOccupied(1));
  }
}
```

**Integration Tests:**
- Scope: Multiple components working together
- Mocking: Minimal; test real interactions
- Speed: Slower (can be up to 1-2 seconds per test)
- Location: `[Feature].Integration/[ClassName]IntegrationTests.cs`

**Example:**
```csharp
[TestClass]
[TestCategory("Integration")]
public class RingAndEconomyIntegrationTests
{
  [TestMethod]
  public void BuildRoom_DeductsCreditsAndUpdatesRing()
  {
    // No mocking - test real Ring and Economy interaction
    var economy = new EconomySystem();
    var ring = new Ring();

    economy.SetCredits(500);
    ring.BuildRoom(RoomFactory.CreateHousingRoom(), 1);

    Assert.IsTrue(ring.IsSegmentOccupied(1));
    Assert.AreEqual(400, economy.Credits);
  }
}
```

**E2E Tests (If Added Later):**
- Tool: GodotTestDriver (for Godot-specific testing)
- Scope: Full gameplay flows (placement, citizens, happiness)
- Speed: Slower; typically run on CI only
- Not initially required; focus on unit + integration first

## Common Patterns

**Async Testing:**
```csharp
[TestMethod]
public async Task LoadCitizensAsync_WithValidData_ReturnsCitizens()
{
  // Arrange
  var repository = new Mock<ICitizenRepository>();
  repository
    .Setup(r => r.LoadAsync())
    .ReturnsAsync(new[] { new Citizen { Name = "Alice" } });

  // Act
  var result = await repository.Object.LoadAsync();

  // Assert
  Assert.AreEqual(1, result.Length);
  Assert.AreEqual("Alice", result[0].Name);
}
```

**Exception Testing:**
```csharp
[TestMethod]
[ExpectedException(typeof(ArgumentException))]
public void BuildRoom_WithNullRoom_ThrowsException()
{
  var ring = new Ring();
  ring.BuildRoom(null, 1);
}

// Or with FluentAssertions (preferred):
[TestMethod]
public void BuildRoom_WithNullRoom_ThrowsException()
{
  var ring = new Ring();

  ring.Invoking(r => r.BuildRoom(null, 1))
    .Should().Throw<ArgumentException>();
}
```

**Parameterized Tests:**
```csharp
[TestClass]
public class RoomSizeTests
{
  [DataTestMethod]
  [DataRow(1)]
  [DataRow(2)]
  [DataRow(3)]
  public void BuildRoom_WithValidSizes_Succeeds(int size)
  {
    var room = new Room { Size = size };
    var ring = new Ring();

    ring.BuildRoom(room, 1);

    Assert.IsTrue(ring.IsSegmentOccupied(1));
  }
}
```

**Collection Assertions:**
```csharp
[TestMethod]
public void GetAvailableSegments_WhenPartiallyOccupied_ReturnsFreeSegments()
{
  var ring = new Ring();
  ring.BuildRoom(RoomFactory.CreateHousingRoom(), 1);

  var available = ring.GetAvailableSegments();

  available.Should()
    .NotContain(1)
    .And.ContainInOrder(2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
}
```

## Test Execution & CI

**Local Testing:**
```bash
# Run all
dotnet test

# Run with category filter
dotnet test --filter "Category=Unit"

# Run specific test class
dotnet test --filter "FullyQualifiedName~RingTests"

# Verbose output
dotnet test -v d
```

**CI/CD Integration (Recommended):**
- Platform: GitHub Actions (already configured with gh CLI)
- Trigger: On push/PR to main
- Steps:
  1. Restore dependencies: `dotnet restore`
  2. Build: `dotnet build`
  3. Run tests: `dotnet test --logger "trx" --collect:"XPlat Code Coverage"`
  4. Report coverage to code quality tools
  5. Fail if coverage drops or tests fail

## Godot-Specific Testing Considerations

**Node Testing:**
- To test Godot `Node` subclasses, must call `AddChild()` to add to scene tree
- Use `GetTree()` to interact with scene tree
- Example:
```csharp
[TestMethod]
public void OnReady_InitializesChildren()
{
  var ring = new Ring();
  // Note: In headless tests, may need to mock GetTree()
  // ring.AddChild(childNode);
  // ring._Ready();
  // Assert children initialized
}
```

**Mocking Godot APIs:**
- Mock `IRandomNumberGenerator` instead of using Godot's `Rng` directly
- Mock `ISystemClock` instead of using `Time.GetTicksMsec()`
- Use dependency injection for Godot services

**Example Architecture for Testability:**
```csharp
public class Ring : Node3D
{
  private readonly IRandom _random;
  private readonly IGameEventDispatcher _events;

  public Ring(IRandom random = null, IGameEventDispatcher events = null)
  {
    _random = random ?? new GodotRandom();
    _events = events ?? new GodotGameEventDispatcher();
  }

  public void BuildRoom(Room room, int segment)
  {
    // Use injected dependencies, making tests possible
    if (_random.Next(100) > 50)
      _events.Emit(new RoomBuiltEvent(room));
  }
}
```

---

*Testing analysis: 2026-03-02*
