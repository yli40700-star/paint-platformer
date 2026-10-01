using System;
using System.Collections.Generic;

/// <summary>
/// Immutable design data for one level.
/// Keeping level rules separate from scene objects makes resets deterministic
/// and lets the player controller query the same grid that the renderer uses.
/// </summary>
public sealed class LevelDefinition
{
    public int Number { get; }
    public string Name { get; }
    public string Hint { get; }
    public int PaintBudget { get; }
    public string[] Rows { get; }
    public string[] ProgressiveHints { get; }

    readonly HashSet<PaintColor> allowedColors;

    public int Width => Rows[0].Length;
    public int Height => Rows.Length;

    public LevelDefinition(
        int number,
        string name,
        string hint,
        int paintBudget,
        PaintColor[] allowedColors,
        string[] rows,
        string[] progressiveHints = null)
    {
        Number = number;
        Name = name;
        Hint = hint;
        PaintBudget = paintBudget;
        Rows = rows;
        ProgressiveHints = progressiveHints ?? Array.Empty<string>();
        this.allowedColors = new HashSet<PaintColor>(allowedColors);

        Validate();
    }

    /// <summary>Returns whether the level allows the requested paint color.</summary>
    public bool Allows(PaintColor color)
    {
        return color != PaintColor.None && allowedColors.Contains(color);
    }

    void Validate()
    {
        if (Rows == null || Rows.Length == 0)
            throw new ArgumentException($"Level {Number} must contain at least one row.");

        int expectedWidth = Rows[0].Length;
        int playerCount = 0;
        int flagCount = 0;

        for (int row = 0; row < Rows.Length; row++)
        {
            if (Rows[row].Length != expectedWidth)
                throw new ArgumentException($"Level {Number} row {row} has an inconsistent width.");

            foreach (char symbol in Rows[row])
            {
                if (symbol == 'P') playerCount++;
                if (symbol == 'F') flagCount++;
                if (".#^vIPF".IndexOf(symbol) < 0)
                    throw new ArgumentException($"Level {Number} contains unknown tile symbol '{symbol}'.");
            }
        }

        if (playerCount != 1 || flagCount != 1)
            throw new ArgumentException($"Level {Number} must contain exactly one player and one flag.");
    }
}

/// <summary>
/// Canonical definitions for all five prototype levels.
/// Rows are stored top-to-bottom exactly as written in the game design.
/// </summary>
public static class LevelData
{
    public static readonly IReadOnlyList<LevelDefinition> Levels =
        new List<LevelDefinition>
        {
            new LevelDefinition(
                1,
                "Blue",
                "The spike pit is four tiles wide. A normal jump only covers about 2.5 tiles. How can you jump farther?",
                2,
                new[] { PaintColor.Blue },
                new[]
                {
                    "................",
                    "................",
                    "................",
                    "................",
                    "P.............F.",
                    "#####^^^^#######",
                    "################"
                }),

            new LevelDefinition(
                2,
                "Green",
                "The wall is three tiles high. Watch the ceiling spikes: bouncing too high can also kill you.",
                2,
                new[] { PaintColor.Green },
                new[]
                {
                    "................",
                    ".....vvv.....F..",
                    "..........######",
                    "..........######",
                    "P.........######",
                    "################",
                    "################"
                }),

            new LevelDefinition(
                3,
                "Red",
                "Red paint melts adjacent ice, but touching red is fatal. Where is the safest place to paint?",
                1,
                new[] { PaintColor.Red },
                new[]
                {
                    "................",
                    "################",
                    "........I.......",
                    "P.......I.....F.",
                    "################",
                    "################"
                }),

            new LevelDefinition(
                4,
                "Combination",
                "Combine all three colors. Every drop of paint is needed.",
                4,
                new[] { PaintColor.Blue, PaintColor.Green, PaintColor.Red },
                new[]
                {
                    "..................",
                    "..............####",
                    ".....vvvvv.....I..",
                    "...............I.F",
                    ".............#####",
                    "P............#####",
                    "######^^^^########",
                    "##################"
                }),

            new LevelDefinition(
                5,
                "Ice Bridge",
                "The high platform is both far away and high above you. Combine effects and think before melting the ice.",
                3,
                new[] { PaintColor.Blue, PaintColor.Green, PaintColor.Red },
                new[]
                {
                    "........................",
                    "........................",
                    "................########",
                    "....................I...",
                    "....................I...",
                    "....................I.F.",
                    "............####III#I###",
                    "P...........####IIIII###",
                    "#######.....####.....###",
                    "#######^^^^^####^^^^^###",
                    "########################"
                },
                new[]
                {
                    "The high platform is too far for green alone and too high for blue alone.",
                    "One blue tile reaches full speed, but it must touch the green tile with no gray gap.",
                    "Connected ice melts together. The ice wall is connected to the bridge under your feet.",
                    "You do not need to finish painting before you start moving. Stop and paint later.",
                    "Stand on the gray island at the end of the bridge before painting red. Look above the ice wall."
                })
        };

    /// <summary>Returns a one-based level number or throws for invalid input.</summary>
    public static LevelDefinition Get(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > Levels.Count)
            throw new ArgumentOutOfRangeException(nameof(levelNumber));

        return Levels[levelNumber - 1];
    }
}
