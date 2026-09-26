using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Run + jump platformer controller. 1 Unity unit = 1 tile.
/// Normal jump: ~1.4 tiles high, ~2.5 tiles far. Ground behavior is modified by ColorEffects.
/// Events for B's UI: <see cref="Died"/> (reason text) and <see cref="Bounced"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    public static event Action<string> Died;
    public static event Action Bounced;

    [Header("Movement (tiles / seconds)")]
    public float runSpeed = 5f;
    public float groundAccel = 60f;
    public float airAccel = 35f;
    public float gravity = 40f;
    public float jumpHeight = 1.4f;
    public float maxFallSpeed = 22f;
    public float coyoteTime = 0.08f;
    public float jumpBuffer = 0.12f;

    [Header("Respawn")]
    [Tooltip("Falling below this Y kills the player.")]
    public float killY = -10f;

    /// <summary>Optional input override for automated tests (null = read the keyboard).</summary>
    public static Func<int> TestDirection;
    public static Func<bool> TestJump;

    public bool Grounded { get; private set; }
    public int Facing { get; private set; }
    public readonly List<Tile> GroundTiles = new List<Tile>();

    Rigidbody2D rb;
    BoxCollider2D box;
    ColorEffects effects;
    Vector3 spawnPoint;
    float coyoteTimer, jumpBufferTimer;
    readonly List<Collider2D> hits = new List<Collider2D>();

    void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        effects = GetComponent<ColorEffects>();

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = gravity / Mathf.Abs(Physics2D.gravity.y);
        // Zero friction so the player never sticks to walls; ColorEffects decides braking instead.
        box.sharedMaterial = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };

        spawnPoint = transform.position;
        Facing = 1;
    }

    /// <summary>Called by B's LevelBuilder after it places the player.</summary>
    public void SetSpawn(Vector3 position) { spawnPoint = position; transform.position = position; }

    public void Die(string reason)
    {
        transform.position = spawnPoint;
        rb.linearVelocity = Vector2.zero;
        coyoteTimer = jumpBufferTimer = 0f;
        if (Died != null) Died(reason);
    }

    void Update()
    {
        if (JumpPressed()) jumpBufferTimer = jumpBuffer;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        if (TestJump != null && TestJump()) jumpBufferTimer = jumpBuffer;
        int dir = TestDirection != null ? TestDirection() : (RightHeld() ? 1 : 0) - (LeftHeld() ? 1 : 0);
        if (dir != 0) Facing = dir;

        Vector2 v = rb.linearVelocity;
        UpdateGround(v.y);

        if (effects == null) effects = GetComponent<ColorEffects>();
        Surface s = effects != null ? effects.GetSurface(GroundTiles)
            : new Surface { MaxSpeed = runSpeed, Accel = groundAccel, Brakes = true };

        // Green: automatic bounce as soon as we land on it.
        if (Grounded && s.BounceSpeed > 0f)
        {
            v.y = s.BounceSpeed;
            Grounded = false;
            coyoteTimer = 0f;
            if (Bounced != null) Bounced();
        }

        if (Grounded)
        {
            if (s.Brakes) v.x = Mathf.MoveTowards(v.x, dir * s.MaxSpeed, s.Accel * dt);
            else if (dir != 0) v.x = Mathf.Clamp(v.x + dir * s.Accel * dt, -s.MaxSpeed, s.MaxSpeed); // blue: no braking
        }
        else
        {
            // In the air: keep momentum above run speed (so blue jumps go far), steer below it.
            if (dir != 0)
            {
                float along = dir * v.x;
                if (along < runSpeed) v.x = dir * Mathf.Min(runSpeed, along + airAccel * dt);
            }
            else if (Mathf.Abs(v.x) <= runSpeed) v.x = Mathf.MoveTowards(v.x, 0f, 15f * dt);
        }

        // Jump with coyote time and input buffering.
        coyoteTimer = Grounded ? coyoteTime : coyoteTimer - dt;
        jumpBufferTimer -= dt;
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            v.y = Mathf.Sqrt(2f * gravity * jumpHeight);
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }

        v.y = Mathf.Max(v.y, -maxFallSpeed);
        rb.linearVelocity = v;

        if (transform.position.y < killY) Hazard.KillPlayer("Fell off!");
    }

    void UpdateGround(float vy)
    {
        GroundTiles.Clear();
        Grounded = false;
        if (vy > 0.01f) return; // moving up: not standing on anything

        Bounds b = box.bounds;
        Vector2 center = new Vector2(b.center.x, b.min.y - 0.03f);
        Vector2 size = new Vector2(b.size.x * 0.95f, 0.05f);
        int n = Physics2D.OverlapBox(center, size, 0f, ContactFilter2D.noFilter, hits);
        for (int i = 0; i < n; i++)
        {
            Collider2D c = hits[i];
            if (c == box || c.isTrigger) continue;
            Grounded = true;
            Tile t = c.GetComponent<Tile>();
            if (t != null) GroundTiles.Add(t);
        }
    }

    // ---------- Input (works with the new Input System or the old Input Manager) ----------
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
