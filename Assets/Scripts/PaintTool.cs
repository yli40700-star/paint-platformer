using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Handles color selection, the mouse preview, and painting tiles.</summary>
public sealed class PaintTool : MonoBehaviour
{
    public PaintColor SelectedColor { get; private set; }
    public int PaintLeft { get; private set; }

    public event Action StateChanged;
    public event Action<GridCell> CellPainted;

    LevelBuilder builder;
    LevelDefinition level;
    LineRenderer hoverOutline;
    Material outlineMaterial;
    Vector2Int hoveredCell;
    bool hasHoveredCell;

    // The scene is still simple, so this adds the tool for us when Play starts.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeTool()
    {
        if (FindFirstObjectByType<PaintTool>() == null)
            new GameObject("PaintTool").AddComponent<PaintTool>();
    }

    void Start()
    {
        CreateHoverOutline();
        ConfigureLevel(1);
    }

    void Update()
    {
        TryFindBuilder();
        ReadColorKeys();
        UpdateHoveredCell();

        if (LeftMousePressed())
            TryPaintHoveredCell();
    }

    /// <summary>Sets the rules and paint amount for a new level.</summary>
    public void ConfigureLevel(int levelNumber)
    {
        level = LevelData.Get(levelNumber);
        PaintLeft = level.PaintBudget;

        if (level.Allows(PaintColor.Blue)) SelectedColor = PaintColor.Blue;
        else if (level.Allows(PaintColor.Green)) SelectedColor = PaintColor.Green;
        else SelectedColor = PaintColor.Red;

        StateChanged?.Invoke();
    }

    /// <summary>Used by the UI later, but the number keys call it too.</summary>
    public bool TrySelectColor(PaintColor color)
    {
        if (level == null || !level.Allows(color))
            return false;

        SelectedColor = color;
        StateChanged?.Invoke();
        return true;
    }

    void TryPaintHoveredCell()
    {
        if (!CanPaintHoveredCell())
            return;

        if (!builder.Map.TryPaint(hoveredCell, SelectedColor))
            return;

        PaintLeft--;
        GridCell paintedCell = builder.Map.GetCell(hoveredCell);
        CellPainted?.Invoke(paintedCell);
        StateChanged?.Invoke();
        UpdateOutlineColor();

        Debug.Log($"Painted {hoveredCell} {SelectedColor}. Paint left: {PaintLeft}");
    }

    void ReadColorKeys()
    {
        if (NumberPressed(1)) TrySelectColor(PaintColor.Blue);
        if (NumberPressed(2)) TrySelectColor(PaintColor.Green);
        if (NumberPressed(3)) TrySelectColor(PaintColor.Red);
    }

    void UpdateHoveredCell()
    {
        if (builder == null || builder.Map == null || Camera.main == null)
        {
            SetOutlineVisible(false);
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(MouseScreenPosition());
        Vector2Int cellPosition = new Vector2Int(
            Mathf.RoundToInt(mouseWorld.x),
            Mathf.RoundToInt(mouseWorld.y));

        hasHoveredCell = builder.Map.IsInside(cellPosition.x, cellPosition.y);
        if (!hasHoveredCell)
        {
            SetOutlineVisible(false);
            return;
        }

        hoveredCell = cellPosition;
        hoverOutline.transform.position = new Vector3(cellPosition.x, cellPosition.y, -0.1f);
        SetOutlineVisible(true);
        UpdateOutlineColor();
    }

    bool CanPaintHoveredCell()
    {
        if (!hasHoveredCell || builder == null || builder.Map == null || PaintLeft <= 0)
            return false;

        GridCell cell = builder.Map.GetCell(hoveredCell);
        return cell != null && cell.IsPaintable && cell.Paint == PaintColor.None && level.Allows(SelectedColor);
    }

    void UpdateOutlineColor()
    {
        if (!hoverOutline.enabled)
            return;

        hoverOutline.startColor = CanPaintHoveredCell()
            ? ColorForPaint(SelectedColor, 0.95f)
            : new Color(1f, 0.18f, 0.18f, 0.55f);
        hoverOutline.endColor = hoverOutline.startColor;
    }

    void CreateHoverOutline()
    {
        GameObject outlineObject = new GameObject("Paint Hover Outline");
        outlineObject.transform.SetParent(transform, false);

        hoverOutline = outlineObject.AddComponent<LineRenderer>();
        hoverOutline.loop = true;
        hoverOutline.useWorldSpace = false;
        hoverOutline.positionCount = 4;
        hoverOutline.widthMultiplier = 0.07f;
        hoverOutline.numCornerVertices = 1;
        hoverOutline.sortingOrder = 20;
        hoverOutline.SetPositions(new[]
        {
            new Vector3(-0.47f, -0.47f, 0f),
            new Vector3(-0.47f, 0.47f, 0f),
            new Vector3(0.47f, 0.47f, 0f),
            new Vector3(0.47f, -0.47f, 0f)
        });

        Shader shader = Shader.Find("Sprites/Default");
        outlineMaterial = new Material(shader);
        hoverOutline.material = outlineMaterial;
        hoverOutline.enabled = false;
    }

    void TryFindBuilder()
    {
        if (builder == null)
            builder = FindFirstObjectByType<LevelBuilder>();
    }

    void SetOutlineVisible(bool visible)
    {
        if (hoverOutline != null)
            hoverOutline.enabled = visible;
    }

    void OnDestroy()
    {
        if (outlineMaterial != null)
            Destroy(outlineMaterial);
    }

    static Color ColorForPaint(PaintColor color, float alpha)
    {
        switch (color)
        {
            case PaintColor.Blue: return new Color(0.18f, 0.48f, 1f, alpha);
            case PaintColor.Green: return new Color(0.2f, 0.82f, 0.38f, alpha);
            case PaintColor.Red: return new Color(0.95f, 0.2f, 0.18f, alpha);
            default: return new Color(1f, 1f, 1f, alpha);
        }
    }

    static Vector2 MouseScreenPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    static bool LeftMousePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    static bool NumberPressed(int number)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return false;
        if (number == 1) return keyboard.digit1Key.wasPressedThisFrame;
        if (number == 2) return keyboard.digit2Key.wasPressedThisFrame;
        return number == 3 && keyboard.digit3Key.wasPressedThisFrame;
#else
        if (number == 1) return Input.GetKeyDown(KeyCode.Alpha1);
        if (number == 2) return Input.GetKeyDown(KeyCode.Alpha2);
        return number == 3 && Input.GetKeyDown(KeyCode.Alpha3);
#endif
    }
}
