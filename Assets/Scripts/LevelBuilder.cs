using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a gray-box level directly from a LevelDefinition.
/// Every visual is generated in code so the prototype does not rely on external assets.
/// </summary>
public sealed class LevelBuilder : MonoBehaviour
{
    const float PixelsPerUnit = 32f;

    static Sprite squareSprite;
    static Sprite spikeSprite;

    readonly Dictionary<Vector2Int, SpriteRenderer> cellRenderers =
        new Dictionary<Vector2Int, SpriteRenderer>();

    Transform levelRoot;

    public TileMapData Map { get; private set; }

    /// <summary>
    /// Creates a builder automatically for the current prototype scene.
    /// This keeps the scene setup minimal while the level systems are under development.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeBuilder()
    {
        if (FindFirstObjectByType<LevelBuilder>() != null)
            return;

        new GameObject("LevelBuilder").AddComponent<LevelBuilder>();
    }

    void Start()
    {
        BuildLevel(1);
    }

    /// <summary>Clears the previous level and builds the requested one-based level number.</summary>
    public void BuildLevel(int levelNumber)
    {
        LevelDefinition definition = LevelData.Get(levelNumber);
        ClearLevel();
        EnsureSprites();

        Map = new TileMapData(definition);
        Map.CellChanged += UpdateCellVisual;

        levelRoot = new GameObject($"Level {definition.Number} - {definition.Name}").transform;
        levelRoot.SetParent(transform, false);

        for (int y = 0; y < Map.Height; y++)
        {
            for (int x = 0; x < Map.Width; x++)
            {
                GridCell cell = Map.GetCell(x, y);
                CreateCell(cell);
            }
        }

        ConfigureCamera(definition);
    }

    void CreateCell(GridCell cell)
    {
        Vector2 position = cell.Position;

        switch (cell.Kind)
        {
            case GridCellKind.Ground:
                CreateBlock(cell, "Ground", position, GroundColor(), TileKind.Ground);
                break;
            case GridCellKind.Ice:
                CreateBlock(cell, "Ice", position, IceColor(), TileKind.Ice);
                break;
            case GridCellKind.FloorSpike:
                CreateSpike("Floor Spike", position, false);
                break;
            case GridCellKind.CeilingSpike:
                CreateSpike("Ceiling Spike", position, true);
                break;
            case GridCellKind.PlayerSpawn:
                CreatePlayerMarker(position);
                break;
            case GridCellKind.Goal:
                CreateFlag(position);
                break;
        }
    }

    void CreateBlock(GridCell cell, string objectName, Vector2 position, Color color, TileKind kind)
    {
        GameObject block = CreateSpriteObject(objectName, position, squareSprite, color, 0);
        Tile tile = block.AddComponent<Tile>();
        tile.Kind = kind;
        cellRenderers[cell.Position] = block.GetComponent<SpriteRenderer>();
    }

    // This keeps the picture in sync when another script paints or melts a cell.
    void UpdateCellVisual(GridCell cell)
    {
        if (!cellRenderers.TryGetValue(cell.Position, out SpriteRenderer renderer))
            return;

        renderer.enabled = cell.Kind != GridCellKind.Empty;
        if (!renderer.enabled)
            return;

        renderer.color = cell.Paint switch
        {
            PaintColor.Blue => new Color(0.18f, 0.48f, 1f),
            PaintColor.Green => new Color(0.2f, 0.82f, 0.38f),
            PaintColor.Red => new Color(0.95f, 0.2f, 0.18f),
            _ => cell.Kind == GridCellKind.Ice ? IceColor() : GroundColor()
        };
    }

    void CreateSpike(string objectName, Vector2 position, bool pointsDown)
    {
        GameObject spike = CreateSpriteObject(
            objectName,
            position,
            spikeSprite,
            new Color(0.92f, 0.25f, 0.28f),
            1);

        if (pointsDown)
            spike.transform.rotation = Quaternion.Euler(0f, 0f, 180f);
    }

    void CreatePlayerMarker(Vector2 position)
    {
        GameObject player = CreateSpriteObject(
            "Player Spawn",
            position,
            squareSprite,
            new Color(0.25f, 0.78f, 1f),
            3);

        player.transform.localScale = new Vector3(0.7f, 0.9f, 1f);
    }

    void CreateFlag(Vector2 position)
    {
        GameObject flagRoot = new GameObject("Goal Flag");
        flagRoot.transform.SetParent(levelRoot, false);
        flagRoot.transform.position = position;

        GameObject pole = CreateSpriteObject(
            "Pole",
            position + new Vector2(-0.28f, -0.05f),
            squareSprite,
            new Color(0.85f, 0.88f, 0.92f),
            2);
        pole.transform.SetParent(flagRoot.transform, true);
        pole.transform.localScale = new Vector3(0.08f, 1.7f, 1f);

        GameObject cloth = CreateSpriteObject(
            "Flag",
            position + new Vector2(0.02f, 0.48f),
            squareSprite,
            new Color(1f, 0.83f, 0.15f),
            3);
        cloth.transform.SetParent(flagRoot.transform, true);
        cloth.transform.localScale = new Vector3(0.62f, 0.42f, 1f);
    }

    GameObject CreateSpriteObject(string objectName, Vector2 position, Sprite sprite, Color color, int order)
    {
        GameObject instance = new GameObject(objectName);
        instance.transform.SetParent(levelRoot, false);
        instance.transform.position = position;

        SpriteRenderer renderer = instance.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return instance;
    }

    void ConfigureCamera(LevelDefinition definition)
    {
        Camera sceneCamera = Camera.main;
        if (sceneCamera == null)
            sceneCamera = FindFirstObjectByType<Camera>();

        if (sceneCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            sceneCamera = cameraObject.AddComponent<Camera>();
        }

        sceneCamera.orthographic = true;
        sceneCamera.backgroundColor = new Color(0.055f, 0.065f, 0.09f);
        sceneCamera.transform.position = new Vector3(
            (definition.Width - 1) * 0.5f,
            (definition.Height - 1) * 0.5f,
            -10f);

        float verticalSize = definition.Height * 0.5f + 1f;
        float horizontalSize = definition.Width / (2f * Mathf.Max(sceneCamera.aspect, 0.1f)) + 1f;
        sceneCamera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);
    }

    void ClearLevel()
    {
        if (Map != null)
            Map.CellChanged -= UpdateCellVisual;

        Map = null;
        cellRenderers.Clear();

        if (levelRoot != null)
            Destroy(levelRoot.gameObject);
    }

    static Color GroundColor()
    {
        return new Color(0.35f, 0.37f, 0.42f);
    }

    static Color IceColor()
    {
        return new Color(0.45f, 0.85f, 1f);
    }

    static void EnsureSprites()
    {
        if (squareSprite == null)
            squareSprite = CreateSquareSprite();
        if (spikeSprite == null)
            spikeSprite = CreateSpikeSprite();
    }

    static Sprite CreateSquareSprite()
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "Generated Square Texture";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    static Sprite CreateSpikeSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Generated Spike Texture";
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            // Texture coordinates start at the bottom, so the triangle is widest
            // at y = 0 and narrows toward the upward-facing point.
            float halfWidth = (size - y) * 0.5f;
            float center = (size - 1) * 0.5f;
            for (int x = 0; x < size; x++)
            {
                bool insideTriangle = Mathf.Abs(x - center) <= halfWidth;
                pixels[y * size + x] = insideTriangle ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);
    }
}
