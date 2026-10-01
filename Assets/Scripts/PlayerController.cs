using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Everything the movement simulation needs between steps.</summary>
public sealed class PlayerState
{
    public readonly KinematicBody Body = new KinematicBody(PlayerController.Width, PlayerController.Height);
    public bool OnBlue;
    public float CoyoteTimer;
    public float JumpBufferTimer;
    public int Facing = 1;
}

/// <summary>What happened during one simulation step.</summary>
public struct StepResult
{
    public string DeathReason;   // null = alive
    public bool ReachedGoal;
    public bool Bounced;
}

/// <summary>
/// Run + jump platformer controller on the level grid, with hand-written AABB collision
/// (KinematicCollision.cs) and a fixed 1/120 s step. 1 Unity unit = 1 tile.
///   Normal jump ~1.4 tiles high / ~2.5 tiles far, blue jump ~5 tiles far, green bounce ~3.5 tiles.
///   Coyote time 0.08 s, jump buffer 0.12 s.
/// Integrates with Junhao's systems: reads LevelBuilder.Map, calls GameManager.CompleteCurrentLevel()
/// at the flag, raises Died(reason) on death, and listens to GameManager.LevelReset.
/// </summary>
public sealed class PlayerController : MonoBehaviour
{
    public const float Width = 0.7f, Height = 0.9f;
    public const float RunSpeed = 5f;
    public const float GroundAccel = 60f;
    public const float AirAccel = 35f;
    public const float AirDrag = 15f;
    public const float Gravity = 40f;
    public const float JumpSpeed = 10.6f;
    public const float MaxFallSpeed = 22f;
    public const float CoyoteTime = 0.08f;
    public const float JumpBuffer = 0.12f;
    public const float FixedStep = 1f / 120f;

    public static PlayerController Instance { get; private set; }

    /// <summary>Raised on every death with a short reason ("Spiked!", "Burned by red paint!", ...).</summary>
    public static event Action<string> Died;
    public static event Action Bounced;

    /// <summary>Optional input override for automated tests (null = read the keyboard).</summary>
    public static Func<int> TestDirection;
    public static Func<bool> TestJump;

    public PlayerState State { get; private set; } = new PlayerState();

    LevelBuilder builder;
    TileMapData map;
    GameManager subscribedManager;
    SpriteRenderer eyeLeft, eyeRight;
    float accumulator;
    bool jumpLatched;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimePlayer()
    {
        if (FindFirstObjectByType<PlayerController>() == null)
            new GameObject("Player").AddComponent<PlayerController>();
    }

    void Awake()
    {
        Instance = this;
        Sprite square = CreateSquareSprite();
        CreatePart("Body", square, new Vector2(Width, Height), new Color(0.97f, 0.98f, 0.99f), 20);
        eyeLeft = CreatePart("EyeL", square, new Vector2(0.09f, 0.14f), new Color(0.06f, 0.08f, 0.13f), 21);
        eyeRight = CreatePart("EyeR", square, new Vector2(0.09f, 0.14f), new Color(0.06f, 0.08f, 0.13f), 21);
    }

    void OnDestroy()
    {
        if (subscribedManager != null) subscribedManager.LevelReset -= Respawn;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        ConnectToLevel();
        if (map == null) return;

        if (JumpPressed()) jumpLatched = true;

        bool playing = GameManager.Instance == null || GameManager.Instance.State == GameState.Playing;
        if (playing)
        {
            accumulator += Mathf.Min(Time.deltaTime, 0.1f);
            while (accumulator >= FixedStep)
            {
                accumulator -= FixedStep;
                int dir = TestDirection != null ? TestDirection() : (RightHeld() ? 1 : 0) - (LeftHeld() ? 1 : 0);
                bool jump = jumpLatched || (TestJump != null && TestJump());
                jumpLatched = false;

                StepResult result = Simulate(State, map, dir, jump, FixedStep);
                if (result.Bounced) Bounced?.Invoke();
                if (result.DeathReason != null) { Die(result.DeathReason); break; }
                if (result.ReachedGoal) { GameManager.Instance?.CompleteCurrentLevel(); break; }
            }
        }
        else
        {
            accumulator = 0f;
            jumpLatched = false;
        }

        UpdateVisuals();
    }

    /// <summary>Picks up a newly built level (first load, level switch) and the reset event.</summary>
    void ConnectToLevel()
    {
        if (builder == null) builder = FindFirstObjectByType<LevelBuilder>();
        if (builder != null && builder.Map != map)
        {
            map = builder.Map;
            if (map != null)
            {
                HideSpawnMarker();
                Respawn();
            }
        }

        if (subscribedManager == null && GameManager.Instance != null)
        {
            subscribedManager = GameManager.Instance;
            subscribedManager.LevelReset += Respawn;
        }
    }

    /// <summary>Back to the spawn point with zero velocity (used for death and for R reset).</summary>
    public void Respawn()
    {
        if (map == null) return;
        State = new PlayerState();
        State.Body.Position = SpawnPosition(map);
        accumulator = 0f;
        UpdateVisuals();
    }

    void Die(string reason)
    {
        Respawn();
        Died?.Invoke(reason);
    }

    /// <summary>Bottom-left corner that puts the player on the floor of the spawn cell.</summary>
    public static Vector2 SpawnPosition(TileMapData map)
    {
        return new Vector2(map.PlayerSpawn.x - Width * 0.5f, map.PlayerSpawn.y - 0.5f);
    }

    /// <summary>
    /// One fixed step of movement, collision, paint effects and hazard checks.
    /// Static and engine-free so it can be tested without entering Play mode.
    /// </summary>
    public static StepResult Simulate(PlayerState s, TileMapData map, int dir, bool jumpPressed, float dt)
    {
        var result = new StepResult();
        KinematicBody b = s.Body;
        if (dir != 0) s.Facing = dir;

        // Horizontal control depends on the surface we stood on last step.
        Surface surface = ColorEffects.For(b.GroundCells, RunSpeed, GroundAccel);
        float vx = b.Velocity.x;
        if (b.Grounded && !surface.Brakes)
        {
            if (dir != 0) vx = Mathf.Clamp(vx + dir * surface.Accel * dt, -surface.MaxSpeed, surface.MaxSpeed);
        }
        else if (b.Grounded)
        {
            vx = Mathf.MoveTowards(vx, dir * surface.MaxSpeed, surface.Accel * dt);
        }
        else if (dir != 0)
        {
            // In the air keep momentum above run speed (so blue jumps carry), steer below it.
            float along = dir * vx;
            if (along < RunSpeed) vx = dir * Mathf.Min(RunSpeed, along + AirAccel * dt);
        }
        else if (Mathf.Abs(vx) <= RunSpeed)
        {
            vx = Mathf.MoveTowards(vx, 0f, AirDrag * dt);
        }
        b.Velocity.x = vx;

        // Jump with buffer + coyote time.
        if (jumpPressed) s.JumpBufferTimer = JumpBuffer;
        s.JumpBufferTimer -= dt;
        s.CoyoteTimer -= dt;
        if (s.JumpBufferTimer > 0f && s.CoyoteTimer > 0f)
        {
            b.Velocity.y = JumpSpeed;
            s.JumpBufferTimer = 0f;
            s.CoyoteTimer = 0f;
        }

        b.Velocity.y = Mathf.Max(b.Velocity.y - Gravity * dt, -MaxFallSpeed);

        b.MoveX(map, dt);
        b.MoveY(map, dt);

        if (b.Grounded)
        {
            Surface under = ColorEffects.For(b.GroundCells, RunSpeed, GroundAccel);
            if (under.BounceSpeed > 0f)
            {
                // Green: launch straight away; we are airborne again.
                b.Velocity.y = under.BounceSpeed;
                b.LeaveGround();
                s.CoyoteTimer = 0f;
                result.Bounced = true;
            }
            else
            {
                s.CoyoteTimer = CoyoteTime;
            }
        }
        s.OnBlue = b.Grounded && !surface.Brakes;

        result.DeathReason = HazardSystem.CheckDeath(map, b);
        if (result.DeathReason == null) result.ReachedGoal = HazardSystem.ReachedGoal(map, b);
        return result;
    }

    // ---------- Visuals (generated in code, no external assets) ----------
    void UpdateVisuals()
    {
        Vector2 c = State.Body.Center;
        transform.position = new Vector3(c.x, c.y, -1f);
        float eyeX = State.Facing * 0.1f;
        eyeLeft.transform.localPosition = new Vector3(eyeX - 0.1f, 0.18f, -0.01f);
        eyeRight.transform.localPosition = new Vector3(eyeX + 0.1f, 0.18f, -0.01f);
    }

    void HideSpawnMarker()
    {
        // LevelBuilder draws a placeholder box at the spawn cell; the real player replaces it.
        foreach (SpriteRenderer r in builder.GetComponentsInChildren<SpriteRenderer>())
            if (r.gameObject.name == "Player Spawn") r.enabled = false;
    }

    SpriteRenderer CreatePart(string partName, Sprite sprite, Vector2 size, Color color, int order)
    {
        var go = new GameObject(partName);
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = order;
        return sr;
    }

    static Sprite CreateSquareSprite()
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    // ---------- Input (new Input System or old Input Manager) ----------
#if ENABLE_INPUT_SYSTEM
    static bool LeftHeld() { var k = Keyboard.current; return k != null && (k.aKey.isPressed || k.leftArrowKey.isPressed); }
    static bool RightHeld() { var k = Keyboard.current; return k != null && (k.dKey.isPressed || k.rightArrowKey.isPressed); }
    static bool JumpPressed() { var k = Keyboard.current; return k != null && (k.spaceKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame); }
#else
    static bool LeftHeld() { return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow); }
    static bool RightHeld() { return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow); }
    static bool JumpPressed() { return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow); }
#endif
}
