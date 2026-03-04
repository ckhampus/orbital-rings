using Godot;

namespace OrbitalRings.Core;

/// <summary>
/// Base class enforcing the signal lifecycle convention for all nodes that
/// connect to GameEvents or other long-lived signal sources.
///
/// Subclasses override <see cref="SubscribeEvents"/> and <see cref="UnsubscribeEvents"/>
/// to connect/disconnect from event delegates. Every += in SubscribeEvents MUST have
/// a matching -= in UnsubscribeEvents.
///
/// <para>
/// Why _EnterTree/_ExitTree instead of _Ready/_ExitTree:
/// - _EnterTree and _ExitTree are called symmetrically for any node lifecycle
///   (add, remove, re-parent, QueueFree).
/// - Autoloads are always ready before scene nodes enter the tree, so
///   GameEvents.Instance is guaranteed non-null in SubscribeEvents.
/// </para>
///
/// <para>
/// Safety note: Using -= on a C# event delegate that was never += is a safe no-op.
/// This is unlike Godot's Disconnect() which throws if the signal was never connected.
/// This makes cleanup robust even in edge cases (e.g., node removed before _EnterTree).
/// </para>
/// </summary>
public partial class SafeNode : Node
{
  public override void _EnterTree()
  {
    base._EnterTree();
    SubscribeEvents();
  }

  public override void _ExitTree()
  {
    UnsubscribeEvents();
    base._ExitTree();
  }

  /// <summary>
  /// Override to connect to GameEvents.Instance and other signal sources.
  /// Called automatically in _EnterTree(). GameEvents.Instance is guaranteed
  /// available because Autoloads initialize before scene nodes.
  /// </summary>
  protected virtual void SubscribeEvents() { }

  /// <summary>
  /// Override to disconnect from all signal sources. Called automatically
  /// in _ExitTree(). MUST mirror every connection made in SubscribeEvents().
  /// </summary>
  protected virtual void UnsubscribeEvents() { }
}
