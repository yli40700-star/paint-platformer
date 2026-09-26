using System;
using UnityEngine;

public enum PaintColor { None, Blue, Green, Red }
public enum TileKind { Ground, Ice }

/// <summary>
/// Shared contract between the player side (A) and the level side (B).
/// B's LevelBuilder adds this to every ground / ice block, and B's PaintTool sets <see cref="Paint"/>.
/// A's ColorEffects only reads <see cref="Paint"/> to decide how the surface behaves.
/// </summary>
[DisallowMultipleComponent]
public class Tile : MonoBehaviour
{
    public TileKind Kind = TileKind.Ground;

    [SerializeField] PaintColor paint = PaintColor.None;

    /// <summary>Raised whenever the paint changes (use it to recolor the sprite).</summary>
    public event Action<Tile> PaintChanged;

    public PaintColor Paint
    {
        get { return paint; }
        set
        {
            if (paint == value) return;
            paint = value;
            if (PaintChanged != null) PaintChanged(this);
        }
    }

    /// <summary>Only plain ground can be painted (not ice).</summary>
    public bool Paintable { get { return Kind == TileKind.Ground; } }
}
