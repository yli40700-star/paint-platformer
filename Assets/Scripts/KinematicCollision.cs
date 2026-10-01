using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hand-written AABB collision against the level grid (no Rigidbody2D / Physics2D).
///
/// World space matches LevelBuilder: cell (x, y) is centered on (x, y), so it covers
/// [x - 0.5, x + 0.5] horizontally and [y - 0.5, y + 0.5] vertically. y points up.
/// The body's Position is its bottom-left corner. Movement resolves X first, then Y.
/// </summary>
public sealed class KinematicBody
{
    public const float Epsilon = 1e-4f;

    public Vector2 Position;          // bottom-left corner of the box
    public Vector2 Velocity;          // tiles per second, +y is up
    public readonly float Width;
    public readonly float Height;

    /// <summary>True when the last Y move landed on a solid cell.</summary>
    public bool Grounded { get; private set; }

    /// <summary>The solid cells directly under the feet after the last landing.</summary>
    public readonly List<GridCell> GroundCells = new List<GridCell>();

    /// <summary>Called when something launches the body off the ground (green bounce).</summary>
    public void LeaveGround()
    {
        Grounded = false;
        GroundCells.Clear();
    }

    public KinematicBody(float width, float height)
    {
        Width = width;
        Height = height;
    }

    public float Left => Position.x;
    public float Right => Position.x + Width;
    public float Bottom => Position.y;
    public float Top => Position.y + Height;
    public Vector2 Center => Position + new Vector2(Width, Height) * 0.5f;

    /// <summary>Grid index of the cell that contains a world coordinate.</summary>
    public static int Cell(float worldCoordinate) => Mathf.FloorToInt(worldCoordinate + 0.5f);

    /// <summary>Moves horizontally and stops against walls (map edges count as walls).</summary>
    public void MoveX(TileMapData map, float dt)
    {
        Position.x += Velocity.x * dt;
        int bottomRow = Cell(Bottom + Epsilon);
        int topRow = Cell(Top - Epsilon);

        if (Velocity.x > 0f)
        {
            int column = Cell(Right - Epsilon);
            for (int y = bottomRow; y <= topRow; y++)
            {
                if (!map.IsSolid(column, y)) continue;
                Position.x = column - 0.5f - Width;
                Velocity.x = 0f;
                return;
            }
        }
        else if (Velocity.x < 0f)
        {
            int column = Cell(Left + Epsilon);
            for (int y = bottomRow; y <= topRow; y++)
            {
                if (!map.IsSolid(column, y)) continue;
                Position.x = column + 0.5f;
                Velocity.x = 0f;
                return;
            }
        }
    }

    /// <summary>Moves vertically; lands on floors (recording the cells underfoot) and bonks on ceilings.</summary>
    public void MoveY(TileMapData map, float dt)
    {
        Position.y += Velocity.y * dt;
        int leftColumn = Cell(Left + Epsilon);
        int rightColumn = Cell(Right - Epsilon);

        Grounded = false;
        GroundCells.Clear();

        if (Velocity.y < 0f)
        {
            int row = Cell(Bottom);
            for (int x = leftColumn; x <= rightColumn; x++)
            {
                if (!map.IsSolid(x, row)) continue;
                Grounded = true;
                GridCell cell = map.GetCell(x, row);
                if (cell != null) GroundCells.Add(cell);
            }
            if (Grounded)
            {
                Position.y = row + 0.5f;
                Velocity.y = 0f;
            }
        }
        else if (Velocity.y > 0f)
        {
            int row = Cell(Top - Epsilon);
            for (int x = leftColumn; x <= rightColumn; x++)
            {
                if (!map.IsSolid(x, row)) continue;
                Position.y = row - 0.5f - Height;
                Velocity.y = 0f;
                return;
            }
        }
    }

    /// <summary>Axis-aligned overlap test between this body (optionally inset) and a rectangle.</summary>
    public bool Overlaps(float minX, float minY, float maxX, float maxY, float insetX = 0f, float insetY = 0f)
    {
        return Left + insetX < maxX && Right - insetX > minX &&
               Bottom + insetY < maxY && Top - insetY > minY;
    }
}
