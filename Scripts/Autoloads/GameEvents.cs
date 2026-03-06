using System;
using OrbitalRings.Data;
using Godot;

namespace OrbitalRings.Autoloads;

/// <summary>
/// Build mode states for the placement/demolish system.
/// Defined here (not in BuildManager) to avoid circular namespace dependency.
/// </summary>
public enum BuildMode { Normal, Placing, Demolish }

/// <summary>
/// Centralized signal bus for all cross-system communication.
/// Registered as an Autoload in project.godot -- access via GameEvents.Instance.
///
/// Uses pure C# event delegates (not Godot [Signal]) to avoid:
/// - Marshalling overhead crossing C#/engine boundary
/// - IsConnected bugs with custom C# signals (GitHub #76690)
/// - Disconnect errors on unconnected signals (GitHub #72994)
///
/// All events use null-safe Emit helpers via ?.Invoke() pattern.
/// </summary>
public partial class GameEvents : Node
{
    /// <summary>
    /// Singleton instance, set in _Ready(). Guaranteed non-null after scene loads
    /// because Autoloads initialize before any scene nodes enter the tree.
    /// </summary>
    public static GameEvents Instance { get; private set; }

    public override void _EnterTree()
    {
        Instance = this;
    }

    // ---------------------------------------------------------------------------
    // Camera Events
    // ---------------------------------------------------------------------------

    public event Action CameraOrbitStarted;
    public event Action CameraOrbitStopped;

    public void EmitCameraOrbitStarted() => CameraOrbitStarted?.Invoke();
    public void EmitCameraOrbitStopped() => CameraOrbitStopped?.Invoke();

    // ---------------------------------------------------------------------------
    // Segment Events (Phase 2)
    // ---------------------------------------------------------------------------

    /// <param name="index">Segment clock position (0-11).</param>
    /// <param name="isOuter">True if outer row, false if inner row.</param>
    public event Action<int, bool> SegmentHovered;
    public event Action SegmentUnhovered;

    /// <param name="index">Segment clock position (0-11).</param>
    /// <param name="isOuter">True if outer row, false if inner row.</param>
    public event Action<int, bool> SegmentSelected;
    public event Action SegmentDeselected;

    public void EmitSegmentHovered(int index, bool isOuter)
      => SegmentHovered?.Invoke(index, isOuter);

    public void EmitSegmentUnhovered()
      => SegmentUnhovered?.Invoke();

    public void EmitSegmentSelected(int index, bool isOuter)
      => SegmentSelected?.Invoke(index, isOuter);

    public void EmitSegmentDeselected()
      => SegmentDeselected?.Invoke();

    // ---------------------------------------------------------------------------
    // Room Events (Phase 4)
    // ---------------------------------------------------------------------------

    /// <param name="roomType">The RoomDefinition.RoomId of the placed room.</param>
    /// <param name="segmentIndex">Ring segment index where the room was placed.</param>
    public event Action<string, int> RoomPlaced;

    /// <param name="segmentIndex">Ring segment index of the demolished room.</param>
    public event Action<int> RoomDemolished;

    public void EmitRoomPlaced(string roomType, int segmentIndex)
      => RoomPlaced?.Invoke(roomType, segmentIndex);

    public void EmitRoomDemolished(int segmentIndex)
      => RoomDemolished?.Invoke(segmentIndex);

    // ---------------------------------------------------------------------------
    // Build Mode Events (Phase 4)
    // ---------------------------------------------------------------------------

    public event Action<BuildMode> BuildModeChanged;
    public event Action<int, int> PlacementPreviewUpdated;  // (startFlatIndex, segmentCount)
    public event Action PlacementPreviewCleared;

    public void EmitBuildModeChanged(BuildMode mode)
        => BuildModeChanged?.Invoke(mode);
    public void EmitPlacementPreviewUpdated(int startFlatIndex, int segmentCount)
        => PlacementPreviewUpdated?.Invoke(startFlatIndex, segmentCount);
    public void EmitPlacementPreviewCleared()
        => PlacementPreviewCleared?.Invoke();

    // ---------------------------------------------------------------------------
    // Placement Feedback Events (Phase 4 -- consumed by PlacementFeedback)
    // ---------------------------------------------------------------------------

    public event Action<MeshInstance3D, Vector3, Color> RoomPlacementConfirmed;
    public event Action<Vector3, Color> RoomDemolishConfirmed;
    public event Action<int> PlacementInvalid;

    public void EmitRoomPlacementConfirmed(MeshInstance3D mesh, Vector3 pos, Color color)
        => RoomPlacementConfirmed?.Invoke(mesh, pos, color);
    public void EmitRoomDemolishConfirmed(Vector3 pos, Color color)
        => RoomDemolishConfirmed?.Invoke(pos, color);
    public void EmitPlacementInvalid(int flatIndex)
        => PlacementInvalid?.Invoke(flatIndex);

    // ---------------------------------------------------------------------------
    // Demolish Hover Events (Phase 4 -- consumed by BuildPanel for refund preview)
    // ---------------------------------------------------------------------------

    public event Action<int, int> DemolishHoverUpdated;  // (flatIndex, refundAmount)
    public event Action DemolishHoverCleared;

    public void EmitDemolishHoverUpdated(int flatIndex, int refund)
        => DemolishHoverUpdated?.Invoke(flatIndex, refund);
    public void EmitDemolishHoverCleared()
        => DemolishHoverCleared?.Invoke();

    // ---------------------------------------------------------------------------
    // Citizen Events (Phase 5)
    // ---------------------------------------------------------------------------

    /// <param name="citizenName">Display name of the arriving citizen.</param>
    public event Action<string> CitizenArrived;

    /// <param name="citizenName">Display name of the clicked citizen.</param>
    public event Action<string> CitizenClicked;

    /// <param name="citizenName">Display name of the citizen entering a room.</param>
    /// <param name="flatSegmentIndex">Flat segment index of the room being entered.</param>
    public event Action<string, int> CitizenEnteredRoom;

    /// <param name="citizenName">Display name of the citizen exiting a room.</param>
    /// <param name="flatSegmentIndex">Flat segment index of the room being exited.</param>
    public event Action<string, int> CitizenExitedRoom;

    public void EmitCitizenArrived(string citizenName)
      => CitizenArrived?.Invoke(citizenName);

    public void EmitCitizenClicked(string citizenName)
      => CitizenClicked?.Invoke(citizenName);

    public void EmitCitizenEnteredRoom(string citizenName, int flatSegmentIndex)
      => CitizenEnteredRoom?.Invoke(citizenName, flatSegmentIndex);

    public void EmitCitizenExitedRoom(string citizenName, int flatSegmentIndex)
      => CitizenExitedRoom?.Invoke(citizenName, flatSegmentIndex);

    // ---------------------------------------------------------------------------
    // Wish Events (Phase 6)
    // ---------------------------------------------------------------------------

    /// <param name="citizenName">Name of the citizen who generated the wish.</param>
    /// <param name="wishType">The WishTemplate.WishId of the generated wish.</param>
    public event Action<string, string> WishGenerated;

    /// <param name="citizenName">Name of the citizen whose wish was fulfilled.</param>
    /// <param name="wishType">The WishTemplate.WishId of the fulfilled wish.</param>
    public event Action<string, string> WishFulfilled;

    public void EmitWishGenerated(string citizenName, string wishType)
      => WishGenerated?.Invoke(citizenName, wishType);

    public void EmitWishFulfilled(string citizenName, string wishType)
      => WishFulfilled?.Invoke(citizenName, wishType);

    // ---------------------------------------------------------------------------
    // Economy Events (Phase 3)
    // ---------------------------------------------------------------------------

    /// <param name="newBalance">Updated credit balance after the change.</param>
    public event Action<int> CreditsChanged;

    public void EmitCreditsChanged(int newBalance)
      => CreditsChanged?.Invoke(newBalance);

    /// <param name="amount">Credits earned from this income tick.</param>
    public event Action<int> IncomeTicked;

    /// <param name="amount">Credits spent (positive number).</param>
    public event Action<int> CreditsSpent;

    /// <param name="amount">Credits refunded (positive number).</param>
    public event Action<int> CreditsRefunded;

    public void EmitIncomeTicked(int amount) => IncomeTicked?.Invoke(amount);
    public void EmitCreditsSpent(int amount) => CreditsSpent?.Invoke(amount);
    public void EmitCreditsRefunded(int amount) => CreditsRefunded?.Invoke(amount);

    // ---------------------------------------------------------------------------
    // Progression Events (Phase 7)
    // ---------------------------------------------------------------------------

    /// <param name="roomType">The RoomDefinition.RoomId of the unlocked blueprint.</param>
    public event Action<string> BlueprintUnlocked;

    public void EmitBlueprintUnlocked(string roomType)
      => BlueprintUnlocked?.Invoke(roomType);

    // ---------------------------------------------------------------------------
    // Happiness v2 Events (Phase 10)
    // ---------------------------------------------------------------------------

    /// <param name="newTier">The new mood tier after the change.</param>
    /// <param name="previousTier">The tier before the change.</param>
    public event Action<MoodTier, MoodTier> MoodTierChanged;

    /// <param name="newCount">Updated lifetime wish count after the fulfilled wish.</param>
    public event Action<int> WishCountChanged;

    public void EmitMoodTierChanged(MoodTier newTier, MoodTier previousTier)
        => MoodTierChanged?.Invoke(newTier, previousTier);

    public void EmitWishCountChanged(int newCount)
        => WishCountChanged?.Invoke(newCount);

    // ---------------------------------------------------------------------------
    // Housing Events (Phase 14)
    // ---------------------------------------------------------------------------

    /// <param name="citizenName">Display name of the assigned citizen.</param>
    /// <param name="segmentIndex">Flat segment index of the home room.</param>
    public event Action<string, int> CitizenAssignedHome;

    /// <param name="citizenName">Display name of the now-unhoused citizen.</param>
    public event Action<string> CitizenUnhoused;

    public void EmitCitizenAssignedHome(string citizenName, int segmentIndex)
        => CitizenAssignedHome?.Invoke(citizenName, segmentIndex);

    public void EmitCitizenUnhoused(string citizenName)
        => CitizenUnhoused?.Invoke(citizenName);

    /// <summary>Fired after housing state is fully restored from save.</summary>
    public event Action HousingStateChanged;

    public void EmitHousingStateChanged()
        => HousingStateChanged?.Invoke();

    // ---------------------------------------------------------------------------
    // Home Visit Events (Phase 17)
    // ---------------------------------------------------------------------------

    /// <param name="citizenName">Display name of the citizen entering home.</param>
    /// <param name="segmentIndex">Flat segment index of the home room.</param>
    public event Action<string, int> CitizenEnteredHome;

    /// <param name="citizenName">Display name of the citizen exiting home.</param>
    /// <param name="segmentIndex">Flat segment index of the home room.</param>
    public event Action<string, int> CitizenExitedHome;

    public void EmitCitizenEnteredHome(string citizenName, int segmentIndex)
        => CitizenEnteredHome?.Invoke(citizenName, segmentIndex);

    public void EmitCitizenExitedHome(string citizenName, int segmentIndex)
        => CitizenExitedHome?.Invoke(citizenName, segmentIndex);
}
