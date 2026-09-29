using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Melts a whole connected ice group when red paint touches it.</summary>
public sealed class IceMeltSystem : MonoBehaviour
{
    static readonly Vector2Int[] Neighbors =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };

    public event Action<int> IceMelted;

    LevelBuilder builder;
    PaintTool paintTool;

    // This makes testing easy without adding anything to the scene by hand.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeSystem()
    {
        if (FindFirstObjectByType<IceMeltSystem>() == null)
            new GameObject("IceMeltSystem").AddComponent<IceMeltSystem>();
    }

    void Update()
    {
        TryConnectSystems();

        // F3 is just a handy test shortcut until the level buttons are added.
        if (F3Pressed() && builder != null && paintTool != null)
        {
            builder.BuildLevel(3);
            paintTool.ConfigureLevel(3);
            Debug.Log("Loaded Level 3 for the ice melting test.");
        }
    }

    void TryConnectSystems()
    {
        if (builder == null)
            builder = FindFirstObjectByType<LevelBuilder>();

        if (paintTool != null)
            return;

        paintTool = FindFirstObjectByType<PaintTool>();
        if (paintTool != null)
            paintTool.CellPainted += OnCellPainted;
    }

    void OnCellPainted(GridCell paintedCell)
    {
        if (paintedCell == null || paintedCell.Paint != PaintColor.Red || builder?.Map == null)
            return;

        int meltedCount = 0;
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        // Red only starts melting from ice directly above, below, left, or right.
        foreach (Vector2Int direction in Neighbors)
        {
            Vector2Int next = paintedCell.Position + direction;
            GridCell cell = builder.Map.GetCell(next);
            if (cell != null && cell.Kind == GridCellKind.Ice && visited.Add(next))
                queue.Enqueue(next);
        }

        // Once melting starts, every connected ice tile joins the same chain.
        while (queue.Count > 0)
        {
            Vector2Int position = queue.Dequeue();

            foreach (Vector2Int direction in Neighbors)
            {
                Vector2Int next = position + direction;
                GridCell neighbor = builder.Map.GetCell(next);
                if (neighbor != null && neighbor.Kind == GridCellKind.Ice && visited.Add(next))
                    queue.Enqueue(next);
            }

            if (builder.Map.TryMeltIce(position))
                meltedCount++;
        }

        if (meltedCount <= 0)
            return;

        IceMelted?.Invoke(meltedCount);
        Debug.Log($"Ice melted: {meltedCount} tile(s).");
    }

    void OnDestroy()
    {
        if (paintTool != null)
            paintTool.CellPainted -= OnCellPainted;
    }

    static bool F3Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F3);
#endif
    }
}
