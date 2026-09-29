using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum GameState
{
    WaitingToStart,
    Playing,
    LevelComplete,
    GameComplete
}

/// <summary>Keeps track of the current level and the main game states.</summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int CurrentLevelNumber { get; private set; } = 1;
    public int DeathCount { get; private set; }
    public GameState State { get; private set; } = GameState.WaitingToStart;

    public event Action<GameState> StateChanged;
    public event Action<int> LevelChanged;
    public event Action<int> DeathCountChanged;
    public event Action LevelReset;

    LevelBuilder builder;
    PaintTool paintTool;
    bool initialized;

    // This creates the manager so we do not need to edit the scene yet.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateRuntimeManager()
    {
        if (FindFirstObjectByType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        PlayerController.Died += OnPlayerDied;
    }

    void Update()
    {
        TryInitialize();
        if (!initialized)
            return;

        if (EnterPressed())
            HandleEnter();

        if (State == GameState.Playing && ResetPressed())
            ResetCurrentLevel();

        int requestedLevel = ReadLevelShortcut();
        if (requestedLevel > 0)
            StartLevel(requestedLevel);
    }

    void TryInitialize()
    {
        if (initialized)
            return;

        builder = FindFirstObjectByType<LevelBuilder>();
        paintTool = FindFirstObjectByType<PaintTool>();
        if (builder == null || paintTool == null || builder.Map == null)
            return;

        initialized = true;
        CurrentLevelNumber = 1;
        DeathCount = 0;
        builder.BuildLevel(CurrentLevelNumber);
        paintTool.ConfigureLevel(CurrentLevelNumber);
        paintTool.enabled = false;
        SetState(GameState.WaitingToStart);
        Debug.Log("Press Enter to start.");
    }

    void HandleEnter()
    {
        if (State == GameState.WaitingToStart)
        {
            paintTool.enabled = true;
            SetState(GameState.Playing);
            Debug.Log("Game started.");
            return;
        }

        if (State != GameState.LevelComplete)
            return;

        if (CurrentLevelNumber < LevelData.Levels.Count)
            StartLevel(CurrentLevelNumber + 1);
        else
            SetState(GameState.GameComplete);
    }

    /// <summary>Loads a level and starts it right away.</summary>
    public void StartLevel(int levelNumber)
    {
        if (!initialized || levelNumber < 1 || levelNumber > LevelData.Levels.Count)
            return;

        CurrentLevelNumber = levelNumber;
        builder.BuildLevel(levelNumber);
        paintTool.ConfigureLevel(levelNumber);
        paintTool.enabled = true;
        SetState(GameState.Playing);
        LevelChanged?.Invoke(levelNumber);
        Debug.Log($"Started Level {levelNumber}.");
    }

    /// <summary>Restores paint, ice, and the player's starting state.</summary>
    public void ResetCurrentLevel()
    {
        if (!initialized || builder.Map == null)
            return;

        builder.Map.Reset();
        paintTool.ConfigureLevel(CurrentLevelNumber);
        LevelReset?.Invoke();
        Debug.Log($"Reset Level {CurrentLevelNumber}.");
    }

    /// <summary>The player controller can call this after touching the flag.</summary>
    public void CompleteCurrentLevel()
    {
        if (State != GameState.Playing)
            return;

        paintTool.enabled = false;
        SetState(GameState.LevelComplete);
        Debug.Log($"Level {CurrentLevelNumber} complete. Press Enter to continue.");
    }

    void OnPlayerDied(string reason)
    {
        if (State != GameState.Playing)
            return;

        DeathCount++;
        DeathCountChanged?.Invoke(DeathCount);
        Debug.Log($"Death {DeathCount}: {reason}");
    }

    void SetState(GameState nextState)
    {
        State = nextState;
        StateChanged?.Invoke(State);
    }

    void OnDestroy()
    {
        PlayerController.Died -= OnPlayerDied;
        if (Instance == this)
            Instance = null;
    }

    static bool EnterPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null &&
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
    }

    static bool ResetPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }

    static int ReadLevelShortcut()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return 0;
        if (keyboard.f1Key.wasPressedThisFrame) return 1;
        if (keyboard.f2Key.wasPressedThisFrame) return 2;
        if (keyboard.f3Key.wasPressedThisFrame) return 3;
        if (keyboard.f4Key.wasPressedThisFrame) return 4;
        if (keyboard.f5Key.wasPressedThisFrame) return 5;
        return 0;
#else
        if (Input.GetKeyDown(KeyCode.F1)) return 1;
        if (Input.GetKeyDown(KeyCode.F2)) return 2;
        if (Input.GetKeyDown(KeyCode.F3)) return 3;
        if (Input.GetKeyDown(KeyCode.F4)) return 4;
        if (Input.GetKeyDown(KeyCode.F5)) return 5;
        return 0;
#endif
    }
}
