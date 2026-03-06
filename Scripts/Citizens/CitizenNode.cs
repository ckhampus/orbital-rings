using Godot;
using OrbitalRings.Autoloads;
using OrbitalRings.Build;
using OrbitalRings.Data;
using OrbitalRings.Ring;

namespace OrbitalRings.Citizens;

/// <summary>
/// A walking citizen on the circular walkway. Uses angle-based polar coordinate
/// movement along the walkway centerline (radius 4.5, midpoint of inner/outer row edges).
///
/// Room visit behavior: periodically drifts toward a nearby occupied segment,
/// fades out (entering room), waits inside, fades back in, and resumes walking.
///
/// Extends Node3D (not SafeNode) because SafeNode extends Node, not Node3D.
/// CitizenNode needs Node3D for Position/Rotation. Implements the same
/// subscribe/unsubscribe lifecycle pattern manually via _EnterTree/_ExitTree.
///
/// Navigation is deliberately simple: the walkway is a 1D circular path,
/// so navigation is literally angle += speed * delta. No NavigationAgent3D needed.
/// </summary>
public partial class CitizenNode : Node3D
{
    // -------------------------------------------------------------------------
    // Constants
    // -------------------------------------------------------------------------

    /// <summary>Walkway centerline radius (midpoint of InnerRowOuter=4.0 and OuterRowInner=5.0).</summary>
    public const float WalkwayRadius = 4.5f;

    /// <summary>Base walking speed in radians/sec (~42s for a full loop).</summary>
    private const float BaseSpeed = 0.15f;

    /// <summary>Speed variation range (+/-15% of base speed).</summary>
    private const float SpeedVariation = 0.15f;

    /// <summary>Vertical bob amplitude (subtle walking rhythm).</summary>
    private const float BobAmplitude = 0.015f;

    /// <summary>Vertical bob frequency in rad/s (~1.3 Hz natural walking rhythm).</summary>
    private const float BobFrequency = 8.0f;

    // ---- Visit constants ----

    /// <summary>Minimum seconds between visit attempts.</summary>
    private const float VisitTimerMin = 20.0f;

    /// <summary>Maximum seconds between visit attempts.</summary>
    private const float VisitTimerMax = 40.0f;

    /// <summary>Minimum seconds spent inside a room.</summary>
    private const float VisitDurationMin = 4.0f;

    /// <summary>Maximum seconds spent inside a room.</summary>
    private const float VisitDurationMax = 8.0f;

    /// <summary>Seconds for radial drift from walkway to room edge.</summary>
    private const float DriftDuration = 0.5f;

    /// <summary>Seconds for fade out/in animation.</summary>
    private const float FadeDuration = 0.3f;

    /// <summary>Drift target radius for inner row rooms.</summary>
    private const float InnerDriftRadius = 3.5f;

    /// <summary>Drift target radius for outer row rooms.</summary>
    private const float OuterDriftRadius = 5.5f;

    /// <summary>
    /// Citizen visits room within this many segment arcs of angular distance.
    /// ~1.5 segment widths ensures citizens only visit nearby rooms.
    /// </summary>
    private const float VisitProximityThreshold = 1.5f;

    // ---- Wish timing constants ----

    /// <summary>Minimum seconds before first/next wish generation (30s).</summary>
    private const float WishTimerMin = 30.0f;

    /// <summary>Maximum seconds before first/next wish generation (60s).</summary>
    private const float WishTimerMax = 60.0f;

    /// <summary>Minimum cooldown seconds after wish fulfillment (30s).</summary>
    private const float WishCooldownMin = 30.0f;

    /// <summary>Maximum cooldown seconds after wish fulfillment (90s).</summary>
    private const float WishCooldownMax = 90.0f;

    /// <summary>Visit timer reset delay on room-build nudge (~5-10s range).</summary>
    private const float NudgeDelay = 7.0f;

    /// <summary>Distance reduction for rooms matching active wish (70% closer in weighting).</summary>
    private const float WishMatchDistanceMultiplier = 0.3f;

    // ---- Badge display constants ----

    /// <summary>Badge vertical offset above citizen head (capsule top + margin).</summary>
    private const float BadgeVerticalOffset = 0.45f;

    /// <summary>Badge pixel size (64px * 0.005 = 0.32 world units).</summary>
    private const float BadgePixelSize = 0.005f;

    // -------------------------------------------------------------------------
    // Fields
    // -------------------------------------------------------------------------

    private CitizenData _data;
    private float _currentAngle;    // radians, 0 to Tau
    private float _direction;       // +1.0 CCW or -1.0 CW
    private float _speed;           // radians/sec
    private float _bobPhase;        // randomized per citizen to avoid sync (pitfall #5)
    private bool _isVisiting;       // true during room visit sequence
    private Node3D _meshContainer;  // holds capsule meshes from CitizenAppearance
    private Tween _activeTween;     // for visit animation, kill-before-create pattern (pitfall #7)
    private Timer _visitTimer;      // periodic check for nearby occupied rooms
    private SegmentGrid _grid;      // reference to ring occupancy data

    // ---- Wish fields ----
    private WishTemplate _currentWish;       // Active wish (null = no wish)
    private Sprite3D _wishBadge;             // Badge icon above citizen head
    private Timer _wishTimer;                // Wish generation/cooldown timer
    private Tween _badgeTween;               // Badge pop animation (separate from visit tween)
    private int _visitTargetSegment = -1;    // Segment index of current visit target (for fulfillment check)

    // ---- Home return fields ----
    private bool _isAtHome;                  // true during home rest sequence
    private bool _walkingToHome;             // true only during walk-to-home phase (for abort detection)
    private Timer _homeTimer;                // periodic home return scheduling
    private Label3D _zzzLabel;               // "Zzz" indicator above room during rest
    private Tween _zzzBobTween;              // bob animation on Zzz label
    private HousingConfig _housingConfig;    // timing constants

    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    /// <summary>Citizen identity and appearance data.</summary>
    public CitizenData Data => _data;

    /// <summary>Current angular position on the walkway (radians, 0 to Tau).</summary>
    public float CurrentAngle => _currentAngle;

    /// <summary>Whether this citizen is currently visiting a room.</summary>
    public bool IsVisiting => _isVisiting;

    /// <summary>Current active wish template, or null if no wish.</summary>
    public WishTemplate CurrentWish => _currentWish;

    /// <summary>Current walking direction: +1.0 (CCW) or -1.0 (CW).</summary>
    public float Direction => _direction;

    /// <summary>
    /// Flat segment index of this citizen's home room. Null when unhoused.
    /// Set by HousingManager on assignment/displacement.
    /// HousingManager is the source of truth; this is a convenience cache.
    /// </summary>
    public int? HomeSegmentIndex { get; set; }

    /// <summary>Whether this citizen is currently resting at home.</summary>
    public bool IsAtHome => _isAtHome;

    // -------------------------------------------------------------------------
    // Save/Load helpers (internal -- used by CitizenManager.SpawnCitizenFromSave)
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets the walking direction. Used during save restoration to match
    /// the citizen's exact state from the previous session.
    /// </summary>
    internal void SetDirection(float dir) => _direction = dir;

    /// <summary>
    /// Updates the segment grid reference. Called by CitizenManager when the
    /// grid is discovered after citizens were spawned (title screen flow:
    /// citizens spawn during _Ready before Ring exists, grid found later in _Process).
    /// </summary>
    internal void SetGrid(SegmentGrid grid) => _grid = grid;

    /// <summary>
    /// Restores a citizen's active wish from save data. Looks up the template
    /// from WishBoard, sets internal state, creates the badge, and notifies
    /// WishBoard for tracking.
    /// </summary>
    internal void SetWishFromSave(string wishId)
    {
        var template = WishBoard.Instance?.GetTemplateById(wishId);
        if (template == null)
        {
            GD.PushWarning($"CitizenNode: Cannot restore wish '{wishId}' -- template not found.");
            return;
        }

        _currentWish = template;
        CreateWishBadge();
        GameEvents.Instance?.EmitWishGenerated(_data.CitizenName, wishId);
    }

    // -------------------------------------------------------------------------
    // Lifecycle (SafeNode pattern for Node3D)
    // -------------------------------------------------------------------------

    public override void _Ready()
    {
        // Start visit timer now that we're in the scene tree
        // (Timer.Start() requires being inside the tree — can't call in Initialize())
        _visitTimer?.Start();
        _wishTimer?.Start();

        // Start home timer only if citizen has a home
        if (HomeSegmentIndex != null)
            _homeTimer?.Start();
    }

    public override void _EnterTree()
    {
        SubscribeEvents();
    }

    public override void _ExitTree()
    {
        UnsubscribeEvents();
        _activeTween?.Kill();
        _badgeTween?.Kill();
        _wishBadge?.QueueFree();
        _zzzBobTween?.Kill();
        _zzzLabel?.QueueFree();
        _homeTimer?.Stop();
    }

    // ---- Stored delegate references for clean event unsubscription ----
    private System.Action<int> _onRoomDemolished;
    private System.Action<string, int> _onCitizenAssignedHome;
    private System.Action<string> _onCitizenUnhoused;

    /// <summary>
    /// Connect to signal sources. Called automatically in _EnterTree().
    /// </summary>
    protected virtual void SubscribeEvents()
    {
        if (WishBoard.Instance != null)
            WishBoard.Instance.WishNudgeRequested += OnWishNudgeRequested;

        if (GameEvents.Instance != null)
        {
            _onRoomDemolished = OnRoomDemolished;
            _onCitizenAssignedHome = OnCitizenAssignedHome;
            _onCitizenUnhoused = OnCitizenUnhoused;

            GameEvents.Instance.RoomDemolished += _onRoomDemolished;
            GameEvents.Instance.CitizenAssignedHome += _onCitizenAssignedHome;
            GameEvents.Instance.CitizenUnhoused += _onCitizenUnhoused;
        }
    }

    /// <summary>
    /// Disconnect from all signal sources. Called automatically in _ExitTree().
    /// MUST mirror every connection made in SubscribeEvents().
    /// </summary>
    protected virtual void UnsubscribeEvents()
    {
        if (WishBoard.Instance != null)
            WishBoard.Instance.WishNudgeRequested -= OnWishNudgeRequested;

        if (GameEvents.Instance != null)
        {
            if (_onRoomDemolished != null)
                GameEvents.Instance.RoomDemolished -= _onRoomDemolished;
            if (_onCitizenAssignedHome != null)
                GameEvents.Instance.CitizenAssignedHome -= _onCitizenAssignedHome;
            if (_onCitizenUnhoused != null)
                GameEvents.Instance.CitizenUnhoused -= _onCitizenUnhoused;
        }
    }

    // -------------------------------------------------------------------------
    // Initialization
    // -------------------------------------------------------------------------

    /// <summary>
    /// Initialize the citizen with data, starting angle, and grid reference.
    /// Must be called before adding to the scene tree.
    /// </summary>
    /// <param name="data">Citizen identity and appearance data.</param>
    /// <param name="startAngle">Starting angular position in radians (0 to Tau).</param>
    /// <param name="grid">Ring segment grid for occupancy checks during room visits.</param>
    /// <param name="housingConfig">Optional housing timing config for home return behavior.</param>
    public void Initialize(CitizenData data, float startAngle, SegmentGrid grid, HousingConfig housingConfig = null)
    {
        _data = data;
        _currentAngle = startAngle;
        _grid = grid;
        Name = data.CitizenName;

        // Random direction: CW or CCW
        _direction = GD.Randf() > 0.5f ? 1.0f : -1.0f;

        // Random speed variation (+/-15% of base speed)
        _speed = BaseSpeed * (1.0f + (GD.Randf() * 2f - 1f) * SpeedVariation);

        // Random bob phase to prevent sync across citizens (pitfall #5)
        _bobPhase = GD.Randf() * Mathf.Tau;

        // Create visual mesh from CitizenData
        _meshContainer = CitizenAppearance.CreateCitizenMesh(data);
        AddChild(_meshContainer);

        // Create visit timer as child Timer node
        // Explicit Timer.Start() over Autostart=true (Phase 3 decision: Autostart before AddChild unreliable)
        _visitTimer = new Timer
        {
            Name = "VisitTimer",
            OneShot = false,
            WaitTime = VisitTimerMin + GD.Randf() * (VisitTimerMax - VisitTimerMin)
        };
        _visitTimer.Timeout += OnVisitTimerTimeout;
        AddChild(_visitTimer);
        // Timer.Start() deferred to _Ready() — timer must be in scene tree first

        // Create wish generation timer (one-shot: fires once, then re-armed in handler)
        _wishTimer = new Timer
        {
            Name = "WishTimer",
            OneShot = true,
            WaitTime = WishTimerMin + GD.Randf() * (WishTimerMax - WishTimerMin)
        };
        _wishTimer.Timeout += OnWishTimerTimeout;
        AddChild(_wishTimer);
        // Timer.Start() deferred to _Ready() per established pattern

        // Create home return timer (one-shot: re-armed in handler with fresh random interval)
        if (housingConfig != null)
        {
            _housingConfig = housingConfig;
            _homeTimer = new Timer
            {
                Name = "HomeTimer",
                OneShot = true,
                WaitTime = housingConfig.HomeTimerMin
                    + GD.Randf() * (housingConfig.HomeTimerMax - housingConfig.HomeTimerMin)
            };
            _homeTimer.Timeout += OnHomeTimerTimeout;
            AddChild(_homeTimer);
            // Timer.Start() deferred to _Ready() — only starts if citizen has a home
        }

        // Set initial position from angle
        UpdatePositionFromAngle();
    }

    // -------------------------------------------------------------------------
    // Process (walking movement)
    // -------------------------------------------------------------------------

    public override void _Process(double delta)
    {
        // Don't walk during room visits or home rest
        if (_isVisiting || _isAtHome) return;

        float dt = (float)delta;

        // Advance angle
        _currentAngle += _direction * _speed * dt;

        // Wrap to [0, Tau)
        if (_currentAngle < 0) _currentAngle += Mathf.Tau;
        if (_currentAngle >= Mathf.Tau) _currentAngle -= Mathf.Tau;

        // Advance bob phase
        _bobPhase += BobFrequency * dt;

        UpdatePositionFromAngle();
    }

    // -------------------------------------------------------------------------
    // Position computation
    // -------------------------------------------------------------------------

    /// <summary>
    /// Computes the citizen's 3D position from the current angle using polar coordinates.
    /// Includes vertical bob and proper surface Y offset.
    /// </summary>
    private void UpdatePositionFromAngle()
    {
        SetRadialPosition(_currentAngle, WalkwayRadius, includeBob: true);
    }

    /// <summary>
    /// Sets the citizen's 3D position from polar coordinates (angle + radius).
    /// Used for both normal walking and visit drift animation.
    /// </summary>
    /// <param name="angle">Angular position in radians.</param>
    /// <param name="radius">Radial distance from center.</param>
    /// <param name="includeBob">Whether to include vertical bob offset.</param>
    private void SetRadialPosition(float angle, float radius, bool includeBob = false)
    {
        float x = Mathf.Cos(angle) * radius;
        float z = Mathf.Sin(angle) * radius;

        float bob = includeBob ? Mathf.Sin(_bobPhase) * BobAmplitude : 0f;

        float surfaceY = SegmentGrid.RingHeight / 2f - SegmentGrid.WalkwayRecess
                       + CitizenAppearance.GetCapsuleHeight(_data.Body) / 2f;

        Position = new Vector3(x, surfaceY + bob, z);

        // Face direction of travel (tangent to circle)
        float facingAngle = angle + (_direction > 0 ? Mathf.Pi * 0.5f : -Mathf.Pi * 0.5f);
        Rotation = new Vector3(0, -facingAngle, 0);
    }

    // -------------------------------------------------------------------------
    // Room visit system
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired periodically by _visitTimer. Checks for nearby occupied segments
    /// and initiates a visit if one is close enough.
    /// </summary>
    private void OnVisitTimerTimeout()
    {
        // Don't start a new visit while already visiting or resting at home
        if (_isVisiting || _isAtHome) return;

        // No grid reference means no occupancy data (shouldn't happen, but guard)
        if (_grid == null) return;

        // Find nearest occupied segment by angular distance
        float myAngle = _currentAngle;
        int bestSegment = -1;
        float bestAngleDist = float.MaxValue;
        SegmentRow bestRow = SegmentRow.Outer;

        for (int i = 0; i < SegmentGrid.TotalSegments; i++)
        {
            var (row, pos) = SegmentGrid.FromIndex(i);
            if (!_grid.IsOccupied(row, pos)) continue;

            float segMidAngle = SegmentGrid.GetStartAngle(pos) + SegmentGrid.SegmentArc * 0.5f;
            float angleDist = AngleDistance(myAngle, segMidAngle);

            // Apply wish-aware distance weighting (per CONTEXT.md locked decision)
            float effectiveDist = angleDist;
            if (_currentWish != null)
            {
                var placedRoom = BuildManager.Instance?.GetPlacedRoom(i);
                if (placedRoom != null)
                {
                    string roomId = placedRoom.Value.Definition.RoomId;
                    foreach (var fulfillingId in _currentWish.FulfillingRoomIds)
                    {
                        if (roomId == fulfillingId)
                        {
                            effectiveDist *= WishMatchDistanceMultiplier;
                            break;
                        }
                    }
                }
            }

            if (effectiveDist < bestAngleDist)
            {
                bestAngleDist = effectiveDist;
                bestSegment = i;
                bestRow = row;
            }
        }

        // Only visit if nearest occupied segment is within proximity threshold
        if (bestSegment >= 0 && bestAngleDist < SegmentGrid.SegmentArc * VisitProximityThreshold)
        {
            _visitTargetSegment = bestSegment;
            var (_, bestPos) = SegmentGrid.FromIndex(bestSegment);
            StartVisit(bestRow, bestPos);
        }

        // Reset timer with new random interval regardless of whether visit started
        _visitTimer.WaitTime = VisitTimerMin + GD.Randf() * (VisitTimerMax - VisitTimerMin);
        _visitTimer.Start();
    }

    /// <summary>
    /// Begins the room visit animation sequence: walk angularly to the target segment,
    /// drift radially to room edge, fade out, wait inside, fade in, drift back to walkway.
    /// </summary>
    /// <param name="targetRow">Which row the target room is in (determines drift direction).</param>
    /// <param name="targetPosition">Clock position (0-11) of the target segment.</param>
    private void StartVisit(SegmentRow targetRow, int targetPosition)
    {
        _isVisiting = true;

        // Kill any active tween before creating new one (pitfall #7)
        _activeTween?.Kill();

        float targetRadius = targetRow == SegmentRow.Outer ? OuterDriftRadius : InnerDriftRadius;
        float visitDuration = VisitDurationMin + GD.Randf() * (VisitDurationMax - VisitDurationMin);
        float currentRadius = WalkwayRadius;
        float currentAngle = _currentAngle;
        float targetAngle = SegmentGrid.GetStartAngle(targetPosition) + SegmentGrid.SegmentArc * 0.5f;
        string citizenName = _data.CitizenName;

        // Compute shortest angular distance and direction (handle Tau wraparound)
        float rawDelta = targetAngle - currentAngle;
        // Normalize to [-Pi, Pi] for shortest path
        if (rawDelta > Mathf.Pi) rawDelta -= Mathf.Tau;
        if (rawDelta < -Mathf.Pi) rawDelta += Mathf.Tau;
        float shortestDelta = rawDelta;

        // Calculate walk duration based on angular distance / walking speed
        float walkDuration = Mathf.Abs(shortestDelta) / _speed;
        // Clamp minimum walk duration to avoid zero-length tweens
        walkDuration = Mathf.Max(walkDuration, 0.1f);

        // Prepare materials for transparency (pitfall #4: set BEFORE fading)
        SetMeshTransparencyMode(true);

        // Create chained tween sequence
        var tween = CreateTween();
        _activeTween = tween;

        // Phase 0: Walk angularly along walkway to target segment's mid-angle
        tween.TweenMethod(
            Callable.From((float t) =>
            {
                float a = currentAngle + shortestDelta * t;
                if (a < 0) a += Mathf.Tau;
                if (a >= Mathf.Tau) a -= Mathf.Tau;
                SetRadialPosition(a, WalkwayRadius, includeBob: true);
            }),
            0.0f, 1.0f, walkDuration
        );

        // Update _currentAngle after walk phase completes
        tween.TweenCallback(Callable.From(() =>
        {
            _currentAngle = targetAngle;
        }));

        // Phase 1: Drift radially from walkway to room edge
        tween.TweenMethod(
            Callable.From((float t) => SetRadialPosition(targetAngle, Mathf.Lerp(currentRadius, targetRadius, t))),
            0.0f, 1.0f, DriftDuration
        );

        // Phase 2: Fade out
        tween.TweenMethod(
            Callable.From((float alpha) => SetMeshAlpha(alpha)),
            1.0f, 0.0f, FadeDuration
        );

        // Phase 3: Hide and emit entered event
        tween.TweenCallback(Callable.From(() =>
        {
            Visible = false;
            GameEvents.Instance?.EmitCitizenEnteredRoom(citizenName, _visitTargetSegment);
        }));

        // Phase 4: Wait inside room
        tween.TweenInterval(visitDuration);

        // Phase 5: Show at target radius and emit exited event
        tween.TweenCallback(Callable.From(() =>
        {
            Visible = true;
            SetRadialPosition(targetAngle, targetRadius);
            GameEvents.Instance?.EmitCitizenExitedRoom(citizenName, _visitTargetSegment);
        }));

        // Phase 6: Fade in
        tween.TweenMethod(
            Callable.From((float alpha) => SetMeshAlpha(alpha)),
            0.0f, 1.0f, FadeDuration
        );

        // Phase 7: Drift back from room edge to walkway
        tween.TweenMethod(
            Callable.From((float t) => SetRadialPosition(targetAngle, Mathf.Lerp(targetRadius, currentRadius, t))),
            0.0f, 1.0f, DriftDuration
        );

        // Phase 8: Restore opaque materials, check wish fulfillment, and resume walking
        tween.TweenCallback(Callable.From(() =>
        {
            SetMeshAlpha(1.0f);
            SetMeshTransparencyMode(false);
            _isVisiting = false;
            _activeTween = null;

            // Check if visited room matches active wish (wish fulfillment)
            if (_currentWish != null && _visitTargetSegment >= 0)
            {
                var placedRoom = BuildManager.Instance?.GetPlacedRoom(_visitTargetSegment);
                if (placedRoom != null)
                {
                    string roomId = placedRoom.Value.Definition.RoomId;
                    foreach (var fulfillingId in _currentWish.FulfillingRoomIds)
                    {
                        if (roomId == fulfillingId)
                        {
                            FulfillWish();
                            break;
                        }
                    }
                }
            }
            _visitTargetSegment = -1;
        }));
    }

    // -------------------------------------------------------------------------
    // Wish system
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired by _wishTimer. Generates a random wish if citizen has none.
    /// </summary>
    private void OnWishTimerTimeout()
    {
        // Don't generate a wish if already have one
        if (_currentWish != null) return;

        // Get a random wish template from WishBoard
        var template = WishBoard.Instance?.GetRandomTemplate();
        if (template == null) return;

        _currentWish = template;

        // Create badge Sprite3D
        CreateWishBadge();

        // Emit event for WishBoard tracking
        GameEvents.Instance?.EmitWishGenerated(_data.CitizenName, template.WishId);
    }

    /// <summary>
    /// Creates the Sprite3D badge above the citizen's head showing the wish category icon.
    /// Badge inherits citizen visibility (hides during room visits automatically).
    /// </summary>
    private void CreateWishBadge()
    {
        // Remove old badge if any
        _wishBadge?.QueueFree();

        // Determine icon texture path based on wish category
        string iconPath = _currentWish.Category switch
        {
            WishTemplate.WishCategory.Social => "res://Resources/Icons/wish_social.png",
            WishTemplate.WishCategory.Comfort => "res://Resources/Icons/wish_comfort.png",
            WishTemplate.WishCategory.Curiosity => "res://Resources/Icons/wish_curiosity.png",
            WishTemplate.WishCategory.Variety => "res://Resources/Icons/wish_variety.png",
            _ => "res://Resources/Icons/wish_social.png"
        };

        var texture = ResourceLoader.Load<Texture2D>(iconPath);
        if (texture == null)
        {
            GD.PushWarning($"CitizenNode: Failed to load wish icon at {iconPath}");
            return;
        }

        _wishBadge = new Sprite3D
        {
            Name = "WishBadge",
            Texture = texture,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = BadgePixelSize,
            Shaded = false,
            AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
            Position = new Vector3(0, BadgeVerticalOffset, 0)
        };

        AddChild(_wishBadge);
    }

    /// <summary>
    /// Fulfills the citizen's active wish: pop animation on badge, emit event,
    /// clear state, restart wish timer with cooldown interval.
    /// Called when citizen visits a room matching their active wish.
    /// </summary>
    private void FulfillWish()
    {
        if (_currentWish == null) return;

        string wishId = _currentWish.WishId;

        // Emit fulfillment event BEFORE clearing state (WishBoard needs to track)
        GameEvents.Instance?.EmitWishFulfilled(_data.CitizenName, wishId);

        // Pop animation on badge: scale up + fade out, then remove
        if (_wishBadge != null)
        {
            _badgeTween?.Kill();
            var tween = CreateTween();
            _badgeTween = tween;

            tween.SetParallel(true);
            tween.TweenProperty(_wishBadge, "scale", Vector3.One * 1.5f, 0.3f)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Back);
            tween.TweenProperty(_wishBadge, "modulate:a", 0.0f, 0.3f)
                .SetEase(Tween.EaseType.In);

            tween.SetParallel(false);
            tween.TweenCallback(Callable.From(() =>
            {
                _wishBadge?.QueueFree();
                _wishBadge = null;
                _badgeTween = null;
            }));
        }

        // Clear wish state
        _currentWish = null;

        // Restart wish timer with cooldown interval (30-90 seconds)
        _wishTimer.WaitTime = WishCooldownMin + GD.Randf() * (WishCooldownMax - WishCooldownMin);
        _wishTimer.Start();
    }

    /// <summary>
    /// Handles WishNudgeRequested event from WishBoard. Resets visit timer to a
    /// short delay so the citizen visits a matching room promptly after it's built.
    /// </summary>
    private void OnWishNudgeRequested(string citizenName)
    {
        // Only respond to nudges for this citizen
        if (_data.CitizenName != citizenName) return;

        // Don't nudge if currently visiting or resting at home
        if (_isVisiting || _isAtHome) return;

        // Reset visit timer to short delay for responsive feedback
        _visitTimer.WaitTime = NudgeDelay;
        _visitTimer.Start();
    }

    // -------------------------------------------------------------------------
    // Home return system
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fired by _homeTimer. Checks guards and initiates home return if appropriate.
    /// </summary>
    private void OnHomeTimerTimeout()
    {
        if (_isVisiting || _isAtHome) return;
        if (HomeSegmentIndex == null) return;
        // BEHV-04: active wish takes priority
        if (_currentWish != null)
        {
            RearmHomeTimer();
            return;
        }
        StartHomeReturn();
    }

    /// <summary>
    /// Re-arms the home timer with a fresh random interval.
    /// </summary>
    private void RearmHomeTimer()
    {
        if (_homeTimer == null || _housingConfig == null) return;
        _homeTimer.WaitTime = _housingConfig.HomeTimerMin
            + GD.Randf() * (_housingConfig.HomeTimerMax - _housingConfig.HomeTimerMin);
        _homeTimer.Start();
    }

    /// <summary>
    /// Starts the home timer when this citizen is assigned a home.
    /// </summary>
    private void OnCitizenAssignedHome(string citizenName, int segmentIndex)
    {
        if (citizenName != _data.CitizenName) return;
        // Start home timer if not already running
        if (_homeTimer != null && _homeTimer.IsStopped())
            RearmHomeTimer();
    }

    /// <summary>
    /// Stops the home timer and aborts home return when this citizen loses their home.
    /// </summary>
    private void OnCitizenUnhoused(string citizenName)
    {
        if (citizenName != _data.CitizenName) return;
        _homeTimer?.Stop();
        // If walking to home or resting, abort
        if (_isAtHome || _walkingToHome)
            AbortHomeReturn();
    }

    /// <summary>
    /// Begins the full home return tween sequence: walk to home segment, drift,
    /// fade out, rest with Zzz indicator, fade in, drift back.
    /// Implemented in Task 2.
    /// </summary>
    private void StartHomeReturn()
    {
        // Full implementation added in Task 2
    }

    /// <summary>
    /// Creates the "Zzz" Label3D indicator at the given world position.
    /// Uses TopLevel=true so it stays visible when citizen node is hidden.
    /// </summary>
    private void CreateZzzLabel(Vector3 worldPosition)
    {
        _zzzLabel = new Label3D
        {
            Text = "Zzz",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 32,
            OutlineSize = 4,
            Modulate = new Color(0.6f, 0.55f, 0.9f, 0.0f),
            OutlineModulate = new Color(0.3f, 0.25f, 0.5f, 0.0f),
            PixelSize = 0.005f,
            NoDepthTest = true,
            Shaded = false,
            DoubleSided = true,
            HorizontalAlignment = HorizontalAlignment.Center,
            TopLevel = true, // Renders independently from parent visibility
        };
        _zzzLabel.GlobalPosition = worldPosition;
        AddChild(_zzzLabel);
    }

    /// <summary>
    /// Removes and nulls the Zzz label and its bob tween.
    /// </summary>
    private void RemoveZzzLabel()
    {
        if (_zzzLabel == null) return;
        _zzzBobTween?.Kill();
        _zzzBobTween = null;
        _zzzLabel.QueueFree();
        _zzzLabel = null;
    }

    /// <summary>
    /// Starts a gentle infinite vertical bob animation on the Zzz label.
    /// </summary>
    private void StartZzzBob()
    {
        if (_zzzLabel == null) return;
        _zzzBobTween?.Kill();
        var baseY = _zzzLabel.GlobalPosition.Y;
        _zzzBobTween = CreateTween();
        _zzzBobTween.SetLoops();
        _zzzBobTween.TweenProperty(_zzzLabel, "global_position:y", baseY + 0.03f, 1.5f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
        _zzzBobTween.TweenProperty(_zzzLabel, "global_position:y", baseY, 1.5f)
            .SetEase(Tween.EaseType.InOut)
            .SetTrans(Tween.TransitionType.Sine);
    }

    /// <summary>
    /// Aborts an in-progress home return: kills tween, restores visual state,
    /// re-arms the home timer if still housed.
    /// </summary>
    private void AbortHomeReturn()
    {
        _activeTween?.Kill();
        _activeTween = null;
        _isAtHome = false;
        _walkingToHome = false;

        // Restore visual state
        Visible = true;
        SetMeshAlpha(1.0f);
        SetMeshTransparencyMode(false);
        RemoveZzzLabel();

        // Restore wish badge visibility if citizen has a wish
        if (_currentWish != null && _wishBadge != null)
            _wishBadge.Visible = true;

        // Resume wish timer if it was paused
        if (_wishTimer != null && _wishTimer.IsStopped() && _currentWish == null)
            _wishTimer.Start();

        // Re-arm home timer (only if still housed)
        if (HomeSegmentIndex != null)
            RearmHomeTimer();
    }

    /// <summary>
    /// Called when this citizen's home is demolished while resting inside.
    /// Restores citizen to walkway immediately.
    /// </summary>
    private void EjectFromHome()
    {
        _activeTween?.Kill();
        _activeTween = null;

        // Resume wish timer
        _wishTimer?.Start();

        // Clean up Zzz
        RemoveZzzLabel();

        // Restore citizen visibility and state
        Visible = true;
        SetMeshAlpha(1.0f);
        SetMeshTransparencyMode(false);
        _isAtHome = false;
        _walkingToHome = false;

        // Restore wish badge if has active wish
        if (_currentWish != null && _wishBadge != null)
            _wishBadge.Visible = true;

        // Don't re-arm home timer -- citizen is now unhoused
        _homeTimer?.Stop();
    }

    /// <summary>
    /// Handles RoomDemolished event. Ejects citizen if demolished room is their home.
    /// </summary>
    private void OnRoomDemolished(int segmentIndex)
    {
        // Only react if this is our home room
        if (HomeSegmentIndex == null || HomeSegmentIndex.Value != segmentIndex) return;

        if (_isAtHome || _walkingToHome)
            EjectFromHome();
    }

    // -------------------------------------------------------------------------
    // Mesh transparency helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Enables or disables alpha transparency mode on all citizen mesh materials.
    /// Must be called BEFORE fading to avoid Z-fighting artifacts (pitfall #4).
    /// </summary>
    internal void SetMeshTransparencyMode(bool enabled)
    {
        if (_meshContainer == null) return;

        foreach (var child in _meshContainer.GetChildren())
        {
            if (child is MeshInstance3D mesh && mesh.MaterialOverride is StandardMaterial3D mat)
            {
                if (enabled)
                {
                    mat.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
                    mat.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Always;
                }
                else
                {
                    mat.Transparency = BaseMaterial3D.TransparencyEnum.Disabled;
                    mat.DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.OpaqueOnly;
                }
            }
        }
    }

    /// <summary>
    /// Sets the alpha channel on all citizen mesh materials (body + band).
    /// </summary>
    /// <param name="alpha">Alpha value from 0 (invisible) to 1 (opaque).</param>
    internal void SetMeshAlpha(float alpha)
    {
        if (_meshContainer == null) return;

        foreach (var child in _meshContainer.GetChildren())
        {
            if (child is MeshInstance3D mesh && mesh.MaterialOverride is StandardMaterial3D mat)
            {
                var color = mat.AlbedoColor;
                mat.AlbedoColor = new Color(color.R, color.G, color.B, alpha);
            }
        }
    }

    // -------------------------------------------------------------------------
    // Static helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the shortest angular distance between two angles (always positive).
    /// Handles wraparound at Tau boundary.
    /// </summary>
    private static float AngleDistance(float a, float b)
    {
        float diff = Mathf.Abs(a - b);
        return Mathf.Min(diff, Mathf.Tau - diff);
    }

    // -------------------------------------------------------------------------
    // Public helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the primary MeshInstance3D (first child of mesh container).
    /// Used for emission glow on selected citizen.
    /// </summary>
    public MeshInstance3D GetPrimaryMesh()
    {
        if (_meshContainer == null) return null;
        return _meshContainer.GetChildOrNull<MeshInstance3D>(0);
    }
}
