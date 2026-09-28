using System;
using UnityEngine;

/// <summary>Logical contents of one grid cell, independent of rendering and physics.</summary>
public enum GridCellKind
{
    Empty,
    Ground,
    FloorSpike,
    CeilingSpike,
    Ice,
    Goal,
    PlayerSpawn
}

/// <summary>
/// Mutable runtime state for one cell. OriginalKind is preserved so a level reset
/// can restore melted ice and remove every painted surface.
/// </summary>
public sealed class GridCell
{
    public Vector2Int Position { get; }
    public GridCellKind OriginalKind { get; }
    public GridCellKind Kind { get; private set; }
    public PaintColor Paint { get; private set; }

    public bool IsSolid => Kind == GridCellKind.Ground || Kind == GridCellKind.Ice;
    public bool IsPaintable => Kind == GridCellKind.Ground;

    public GridCell(Vector2Int position, GridCellKind kind)
    {
        Position = position;
        OriginalKind = kind;
        Kind = kind;
        Paint = PaintColor.None;
    }

    /// <summary>Paints a normal ground cell and rejects all other cell types.</summary>
    public bool TryPaint(PaintColor color)
    {
        if (!IsPaintable || color == PaintColor.None || Paint != PaintColor.None)
            return false;

        Paint = color;
        return true;
    }

    /// <summary>Turns an ice cell into air. Returns false when the cell is not ice.</summary>
    public bool TryMeltIce()
    {
        if (Kind != GridCellKind.Ice)
            return false;

        Kind = GridCellKind.Empty;
        return true;
    }

    /// <summary>Restores the cell to its original level-start state.</summary>
    public void Reset()
    {
        Kind = OriginalKind;
        Paint = PaintColor.None;
    }
}

/// <summary>
/// Runtime grid shared by level rendering, painting, ice melting, and custom AABB collision.
/// Coordinates use the bottom-left of the ASCII map as (0, 0).
/// </summary>
public sealed class TileMapData
{
    readonly GridCell[,] cells;

    public int Width { get; }
    public int Height { get; }
    public Vector2Int PlayerSpawn { get; }
    public Vector2Int Goal { get; }

    /// <summary>Raised whenever painting, melting, or resetting changes a cell.</summary>
    public event Action<GridCell> CellChanged;

    public TileMapData(LevelDefinition definition)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        Width = definition.Width;
        Height = definition.Height;
        cells = new GridCell[Width, Height];

        Vector2Int spawn = default;
        Vector2Int goal = default;

        for (int row = 0; row < Height; row++)
        {
            int y = Height - 1 - row;
            for (int x = 0; x < Width; x++)
            {
                GridCellKind kind = ParseSymbol(definition.Rows[row][x]);
                GridCell cell = new GridCell(new Vector2Int(x, y), kind);
                cells[x, y] = cell;

                if (kind == GridCellKind.PlayerSpawn)
                    spawn = cell.Position;
                else if (kind == GridCellKind.Goal)
                    goal = cell.Position;
            }
        }

        PlayerSpawn = spawn;
        Goal = goal;
    }

    /// <summary>Returns the cell at integer grid coordinates, or null outside the map.</summary>
    public GridCell GetCell(int x, int y)
    {
        return IsInside(x, y) ? cells[x, y] : null;
    }

    public GridCell GetCell(Vector2Int position)
    {
        return GetCell(position.x, position.y);
    }

    public bool IsInside(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    /// <summary>
    /// Checks solidity using the assignment's boundary rule: the left and right sides
    /// are walls while space above and below the map remains empty.
    /// </summary>
    public bool IsSolid(int x, int y)
    {
        if (y < 0 || y >= Height)
            return false;
        if (x < 0 || x >= Width)
            return true;

        return cells[x, y].IsSolid;
    }

    /// <summary>Paints one cell and broadcasts its changed state.</summary>
    public bool TryPaint(Vector2Int position, PaintColor color)
    {
        GridCell cell = GetCell(position);
        if (cell == null || !cell.TryPaint(color))
            return false;

        CellChanged?.Invoke(cell);
        return true;
    }

    /// <summary>Melts one ice cell and broadcasts its changed state.</summary>
    public bool TryMeltIce(Vector2Int position)
    {
        GridCell cell = GetCell(position);
        if (cell == null || !cell.TryMeltIce())
            return false;

        CellChanged?.Invoke(cell);
        return true;
    }

    /// <summary>Restores every cell to the original ASCII level definition.</summary>
    public void Reset()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                GridCell cell = cells[x, y];
                cell.Reset();
                CellChanged?.Invoke(cell);
            }
        }
    }

    static GridCellKind ParseSymbol(char symbol)
    {
        switch (symbol)
        {
            case '#': return GridCellKind.Ground;
            case '^': return GridCellKind.FloorSpike;
            case 'v': return GridCellKind.CeilingSpike;
            case 'I': return GridCellKind.Ice;
            case 'F': return GridCellKind.Goal;
            case 'P': return GridCellKind.PlayerSpawn;
            default: return GridCellKind.Empty;
        }
    }
}
