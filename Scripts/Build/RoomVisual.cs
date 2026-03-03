using Godot;
using OrbitalRings.Data;
using OrbitalRings.Ring;

namespace OrbitalRings.Build;

/// <summary>
/// Static helper for creating 3D room block meshes on the ring.
/// Room blocks are raised colored annular sectors sitting on top of ring segments.
/// Each block gets its own independent StandardMaterial3D to avoid shared-material contamination.
/// </summary>
public static class RoomVisual
{
    /// <summary>
    /// Creates a MeshInstance3D for a room block (ghost preview or placed room).
    /// The mesh spans <paramref name="segmentCount"/> consecutive segments starting at
    /// <paramref name="startPos"/> in the given row.
    /// </summary>
    /// <param name="row">Which row (Outer or Inner) the room is placed in.</param>
    /// <param name="startPos">Clock position (0-11) of the first segment.</param>
    /// <param name="segmentCount">Number of consecutive segments (1-3).</param>
    /// <param name="room">Room definition for color and height lookup.</param>
    /// <param name="isGhost">True for semi-transparent ghost preview, false for placed room.</param>
    /// <returns>A positioned MeshInstance3D ready to be added to the scene tree.</returns>
    public static MeshInstance3D CreateRoomBlock(
        SegmentRow row, int startPos, int segmentCount,
        RoomDefinition room, bool isGhost = false)
    {
        (float innerR, float outerR) = SegmentGrid.GetRowRadii(row);

        float startAngle = SegmentGrid.GetStartAngle(startPos);
        float endAngle = startAngle + SegmentGrid.SegmentArc * segmentCount;

        // Block height with subtle increase for multi-segment rooms
        float blockHeight = RoomColors.GetBlockHeight(room.RoomId);
        blockHeight *= 1.0f + 0.08f * (segmentCount - 1);

        ArrayMesh mesh = RingMeshBuilder.CreateAnnularSector(
            innerR, outerR, startAngle, endAngle,
            blockHeight, subdivisions: 4 * segmentCount);

        // Each room block gets its own independent material instance
        var material = new StandardMaterial3D();
        if (isGhost)
        {
            material.AlbedoColor = RoomColors.GetGhostColor(room.Category);
            material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        }
        else
        {
            material.AlbedoColor = RoomColors.GetCategoryColor(room.Category);
        }

        // Position block sitting on top of the ring surface
        float yOffset = SegmentGrid.RingHeight * 0.5f + blockHeight * 0.5f;
        string namePrefix = isGhost ? "Ghost" : "Room";
        string nameSuffix = isGhost ? room.RoomId : $"{room.RoomId}_{row}_{startPos}";

        var meshInstance = new MeshInstance3D
        {
            Name = $"{namePrefix}_{nameSuffix}",
            Mesh = mesh,
            MaterialOverride = material,
            Transform = new Transform3D(Basis.Identity, new Vector3(0, yOffset, 0))
        };

        return meshInstance;
    }

    /// <summary>
    /// Converts a ghost mesh to an opaque placed room material.
    /// Creates a NEW material instance -- never reuses the ghost material.
    /// </summary>
    /// <param name="roomMesh">The MeshInstance3D to make opaque.</param>
    /// <param name="category">Room category for color lookup.</param>
    public static void MakeOpaque(MeshInstance3D roomMesh, RoomDefinition.RoomCategory category)
    {
        var material = new StandardMaterial3D
        {
            AlbedoColor = RoomColors.GetCategoryColor(category),
            Transparency = BaseMaterial3D.TransparencyEnum.Disabled
        };
        roomMesh.MaterialOverride = material;
    }
}
