using System.Collections.Generic;
using UnityEngine;

/// <summary>How the ground under the player behaves this frame.</summary>
public struct Surface
{
    public float MaxSpeed;     // top horizontal speed (tiles/s)
    public float Accel;        // how fast we reach it
    public bool Brakes;        // false = no deceleration (blue keeps sliding)
    public float BounceSpeed;  // > 0 = launch upward on contact (green)
}

/// <summary>
/// The three paint colors. Put this on the player next to PlayerController.
///   Blue  (slide):  ~2x run speed, never brakes -> jump farther, but you slide off ledges.
///   Green (bounce): automatic ~3.5-tile bounce -> reach high ledges, but you can hit ceiling spikes.
///   Red   (burn):   kills on touch (side, top or bottom). Melting ice is handled by B's PaintTool.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class ColorEffects : MonoBehaviour
{
    [Header("Blue - slide")]
    public float blueSpeedMultiplier = 2f;
    public float blueAccel = 60f;

    [Header("Green - bounce")]
    public float bounceHeight = 3.5f;

    [Header("Red - burn")]
    [Tooltip("How far (tiles) the player must overlap a red tile before it counts, so grazing a corner is forgiven.")]
    public float redForgiveness = 0.15f;

    PlayerController player;
    BoxCollider2D box;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        box = GetComponent<BoxCollider2D>();
    }

    /// <summary>Surface rules for the tiles the player is standing on. Green wins over blue.</summary>
    public Surface GetSurface(List<Tile> ground)
    {
        var s = new Surface { MaxSpeed = player.runSpeed, Accel = player.groundAccel, Brakes = true, BounceSpeed = 0f };
        bool blue = false, green = false;
        foreach (Tile t in ground)
        {
            if (t == null) continue;
            if (t.Paint == PaintColor.Green) green = true;
            if (t.Paint == PaintColor.Blue) blue = true;
        }
        if (green)
        {
            s.BounceSpeed = Mathf.Sqrt(2f * player.gravity * bounceHeight);
        }
        else if (blue)
        {
            s.MaxSpeed = player.runSpeed * blueSpeedMultiplier;
            s.Accel = blueAccel;
            s.Brakes = false;
        }
        return s;
    }

    void OnCollisionEnter2D(Collision2D c) { CheckRed(c.collider); }
    void OnCollisionStay2D(Collision2D c) { CheckRed(c.collider); }

    void CheckRed(Collider2D other)
    {
        Tile t = other.GetComponent<Tile>();
        if (t == null || t.Paint != PaintColor.Red) return;

        Bounds a = box.bounds, b = other.bounds;
        float overlapX = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
        float overlapY = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y);
        // Standing on / bumping into it from above or below needs real horizontal overlap;
        // touching it from the side needs real vertical overlap.
        if (overlapX > redForgiveness || overlapY > redForgiveness)
            Hazard.KillPlayer("Burned by red paint!");
    }
}
