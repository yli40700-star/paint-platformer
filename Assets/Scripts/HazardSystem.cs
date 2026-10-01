using UnityEngine;

/// <summary>
/// Death and goal checks against the grid, with forgiving hitboxes:
///   floor spikes only hurt in the lower 55% of the cell, ceiling spikes in the upper 55%,
///   red paint ignores grazing contact closer than 0.15 tiles, and falling below the map kills.
/// </summary>
public static class HazardSystem
{
    const float SpikeHeight = 0.55f;
    const float SpikeInset = 0.15f;
    const float RedForgiveness = 0.15f;
    const float Touch = 0.04f;   // how close counts as touching red

    /// <summary>Returns a death reason, or null if the player is safe.</summary>
    public static string CheckDeath(TileMapData map, KinematicBody body)
    {
        int minX = KinematicBody.Cell(body.Left - 0.1f), maxX = KinematicBody.Cell(body.Right + 0.1f);
        int minY = KinematicBody.Cell(body.Bottom - 0.1f), maxY = KinematicBody.Cell(body.Top + 0.1f);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                GridCell cell = map.GetCell(x, y);
                if (cell == null) continue;
                float left = x - 0.5f, right = x + 0.5f, bottom = y - 0.5f, top = y + 0.5f;

                switch (cell.Kind)
                {
                    case GridCellKind.FloorSpike:
                        if (body.Overlaps(left + SpikeInset, bottom, right - SpikeInset, bottom + SpikeHeight))
                            return "Spiked!";
                        break;
                    case GridCellKind.CeilingSpike:
                        if (body.Overlaps(left + SpikeInset, top - SpikeHeight, right - SpikeInset, top))
                            return "Hit the ceiling spikes!";
                        break;
                    case GridCellKind.Ground:
                        if (cell.Paint == PaintColor.Red && TouchesRed(body, left, bottom, right, top))
                            return "Burned by red paint!";
                        break;
                }
            }
        }

        if (body.Top < -1.5f) return "Fell off!";
        return null;
    }

    /// <summary>True when the player overlaps the goal flag's cell.</summary>
    public static bool ReachedGoal(TileMapData map, KinematicBody body)
    {
        Vector2Int g = map.Goal;
        return body.Overlaps(g.x - 0.3f, g.y - 0.5f, g.x + 0.3f, g.y + 0.5f);
    }

    // Standing on / bumping red from above or below needs real horizontal overlap;
    // touching it from the side needs real vertical overlap.
    static bool TouchesRed(KinematicBody body, float left, float bottom, float right, float top)
    {
        bool fromAboveOrBelow =
            body.Left + RedForgiveness < right && body.Right - RedForgiveness > left &&
            body.Bottom - Touch < top && body.Top + Touch > bottom;
        bool fromSide =
            body.Left - Touch < right && body.Right + Touch > left &&
            body.Bottom + RedForgiveness < top && body.Top - RedForgiveness > bottom;
        return fromAboveOrBelow || fromSide;
    }
}
