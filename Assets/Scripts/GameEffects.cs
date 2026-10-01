using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Adds a small paint burst and the red death flash.</summary>
public sealed class GameEffects : MonoBehaviour
{
    const float FlashDuration = 0.28f;

    PaintTool paintTool;
    Image flashImage;
    Material particleMaterial;
    float flashTimeLeft;

    // This keeps the scene setup simple while the prototype is still changing.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeEffects()
    {
        if (FindFirstObjectByType<GameEffects>() == null)
            new GameObject("GameEffects").AddComponent<GameEffects>();
    }

    void Awake()
    {
        CreateFlashCanvas();
        CreateParticleMaterial();
        PlayerController.Died += OnPlayerDied;
    }

    void Update()
    {
        TryConnectPaintTool();
        UpdateDeathFlash();

#if UNITY_EDITOR
        // F8 lets us check the flash before the final player is connected.
        if (TestFlashPressed())
            PlayDeathFlash();
#endif
    }

    void TryConnectPaintTool()
    {
        if (paintTool != null)
            return;

        paintTool = FindFirstObjectByType<PaintTool>();
        if (paintTool != null)
            paintTool.CellPainted += OnCellPainted;
    }

    void OnCellPainted(GridCell cell)
    {
        if (cell == null)
            return;

        CreatePaintBurst(cell.Position, ColorForPaint(cell.Paint));
    }

    void OnPlayerDied(string reason)
    {
        PlayDeathFlash();
    }

    public void PlayDeathFlash()
    {
        flashTimeLeft = FlashDuration;
        SetFlashAlpha(0.38f);
    }

    void UpdateDeathFlash()
    {
        if (flashTimeLeft <= 0f)
            return;

        flashTimeLeft = Mathf.Max(0f, flashTimeLeft - Time.unscaledDeltaTime);
        float alpha = 0.38f * (flashTimeLeft / FlashDuration);
        SetFlashAlpha(alpha);
    }

    void SetFlashAlpha(float alpha)
    {
        if (flashImage != null)
            flashImage.color = new Color(0.9f, 0.03f, 0.03f, alpha);
    }

    void CreatePaintBurst(Vector2 position, Color color)
    {
        GameObject burstObject = new GameObject("Paint Burst");
        burstObject.transform.position = new Vector3(position.x, position.y, -0.2f);

        ParticleSystem particles = burstObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.3f, 2.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.startColor = color;
        main.gravityModifier = 0.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.22f;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = particleMaterial;
        particleRenderer.sortingOrder = 40;

        particles.Play();
    }

    void CreateFlashCanvas()
    {
        GameObject canvasObject = new GameObject("Death Flash Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        GameObject imageObject = new GameObject("Red Flash");
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        flashImage = imageObject.AddComponent<Image>();
        flashImage.raycastTarget = false;
        SetFlashAlpha(0f);
    }

    void CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        particleMaterial = new Material(shader);
        particleMaterial.name = "Generated Paint Particle Material";
    }

    void OnDestroy()
    {
        PlayerController.Died -= OnPlayerDied;
        if (paintTool != null)
            paintTool.CellPainted -= OnCellPainted;
        if (particleMaterial != null)
            Destroy(particleMaterial);
    }

    static Color ColorForPaint(PaintColor color)
    {
        switch (color)
        {
            case PaintColor.Blue: return new Color(0.18f, 0.48f, 1f);
            case PaintColor.Green: return new Color(0.2f, 0.82f, 0.38f);
            case PaintColor.Red: return new Color(0.95f, 0.2f, 0.18f);
            default: return Color.white;
        }
    }

#if UNITY_EDITOR
    static bool TestFlashPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F8);
#endif
    }
#endif
}
