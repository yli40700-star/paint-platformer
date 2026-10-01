using System.Collections.Generic;

/// <summary>How the ground under the player behaves this step.</summary>
public struct Surface
{
    public float MaxSpeed;     // top horizontal speed (tiles/s)
    public float Accel;        // how fast we reach it
    public bool Brakes;        // false = no deceleration (blue keeps sliding)
    public float BounceSpeed;  // > 0 = launch upward on landing (green)
}

/// <summary>
/// The three paint colors, as surface rules read from the grid.
///   Blue  (slide):  2x run speed (5 -> 10), accelerates but never brakes.
///   Green (bounce): automatic ~3.5-tile bounce on landing.
///   Red   (burn):   handled by HazardSystem (kills on touch); melting ice is IceMeltSystem's job.
/// </summary>
public static class ColorEffects
{
    public const float BlueMaxSpeed = 10f;
    public const float BlueAccel = 60f;
    public const float BounceSpeed = 16.8f;   // with gravity 40 -> ~3.5 tiles high

    /// <summary>Surface rules for the cells under the player's feet. Green wins over blue.</summary>
    public static Surface For(List<GridCell> ground, float runSpeed, float groundAccel)
    {
        bool blue = false, green = false;
        foreach (GridCell cell in ground)
        {
            if (cell.Paint == PaintColor.Green) green = true;
            else if (cell.Paint == PaintColor.Blue) blue = true;
        }

        if (green)
            return new Surface { MaxSpeed = runSpeed, Accel = groundAccel, Brakes = true, BounceSpeed = BounceSpeed };
        if (blue)
            return new Surface { MaxSpeed = BlueMaxSpeed, Accel = BlueAccel, Brakes = false };
        return new Surface { MaxSpeed = runSpeed, Accel = groundAccel, Brakes = true };
    }
}
