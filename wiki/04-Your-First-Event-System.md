# Your First Event System

Let's build a complete event-driven system from scratch! We'll create a simple game where players can take damage, heal, and die - all using events.

## Project Overview

We'll build a system with:
- **Player Controller** - Handles input and player state
- **Health System** - Manages health and sends events
- **UI Manager** - Updates the user interface
- **Sound Manager** - Plays audio feedback
- **Game Manager** - Handles game state

## Step 1: Define Our Events

First, let's create all the events we'll need:

```csharp
using UnityEngine;
using com.DvosTools.bus;

// Player health changed
public class PlayerHealthChangedEvent
{
    public int CurrentHealth { get; set; }
    public int MaxHealth { get; set; }
    public int HealthChange { get; set; } // Positive for healing, negative for damage
}

// Player died
public class PlayerDiedEvent
{
    public Vector3 DeathPosition { get; set; }
    public int FinalScore { get; set; }
}

// Game state changed
public class GameStateChangedEvent
{
    public GameState OldState { get; set; }
    public GameState NewState { get; set; }
}

// Player scored points
public class PlayerScoredEvent
{
    public int Points { get; set; }
    public int TotalScore { get; set; }
}

public enum GameState
{
    Menu,
    Playing,
    Paused,
    GameOver
}
```

## Step 2: Create the Health System

This will be the core of our event system:

```csharp
using UnityEngine;
using com.DvosTools.bus;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    
    void Start()
    {
        currentHealth = maxHealth;
        
        // Send initial health event
        EventBus.Send(new PlayerHealthChangedEvent
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth,
            HealthChange = 0
        });
    }
    
    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return; // Already dead
        
        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - damage);
        
        // Send health changed event
        EventBus.Send(new PlayerHealthChangedEvent
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth,
            HealthChange = currentHealth - oldHealth
        });
        
        // Check if player died
        if (currentHealth <= 0)
        {
            EventBus.Send(new PlayerDiedEvent
            {
                DeathPosition = transform.position,
                FinalScore = GetComponent<ScoreSystem>()?.TotalScore ?? 0
            });
        }
    }
    
    public void Heal(int amount)
    {
        if (currentHealth <= 0) return; // Can't heal when dead
        
        int oldHealth = currentHealth;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        
        // Send health changed event
        EventBus.Send(new PlayerHealthChangedEvent
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth,
            HealthChange = currentHealth - oldHealth
        });
    }
}
```

## Step 3: Create the UI Manager

This will respond to health events and update the UI:

```csharp
using UnityEngine;
using UnityEngine.UI;
using com.DvosTools.bus;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Slider healthBar;
    [SerializeField] private Text healthText;
    [SerializeField] private Text scoreText;
    [SerializeField] private GameObject gameOverPanel;
    
    void Start()
    {
        // Subscribe to events
        EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(OnHealthChanged);
        EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnPlayerDied);
        EventBus.RegisterUnityHandler<PlayerScoredEvent>(OnPlayerScored);
        EventBus.RegisterUnityHandler<GameStateChangedEvent>(OnGameStateChanged);
        
        // Initialize UI
        UpdateHealthUI(100, 100);
        UpdateScoreUI(0);
        gameOverPanel.SetActive(false);
    }
    
    void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        UpdateHealthUI(evt.CurrentHealth, evt.MaxHealth);
        
        // Show damage/heal feedback
        if (evt.HealthChange < 0)
        {
            ShowDamageEffect();
        }
        else if (evt.HealthChange > 0)
        {
            ShowHealEffect();
        }
    }
    
    void OnPlayerDied(PlayerDiedEvent evt)
    {
        gameOverPanel.SetActive(true);
        ShowGameOverMessage(evt.FinalScore);
    }
    
    void OnPlayerScored(PlayerScoredEvent evt)
    {
        UpdateScoreUI(evt.TotalScore);
        ShowScorePopup(evt.Points);
    }
    
    void OnGameStateChanged(GameStateChangedEvent evt)
    {
        // Handle game state changes
        switch (evt.NewState)
        {
            case GameState.Playing:
                gameOverPanel.SetActive(false);
                break;
            case GameState.GameOver:
                gameOverPanel.SetActive(true);
                break;
        }
    }
    
    void UpdateHealthUI(int current, int max)
    {
        healthBar.value = (float)current / max;
        healthText.text = $"{current}/{max}";
    }
    
    void UpdateScoreUI(int score)
    {
        scoreText.text = $"Score: {score}";
    }
    
    void ShowDamageEffect()
    {
        // Flash red or shake screen
        StartCoroutine(FlashRed());
    }
    
    void ShowHealEffect()
    {
        // Flash green or show healing particles
        StartCoroutine(FlashGreen());
    }
    
    void ShowScorePopup(int points)
    {
        // Show floating score text
        Debug.Log($"+{points} points!");
    }
    
    void ShowGameOverMessage(int finalScore)
    {
        Debug.Log($"Game Over! Final Score: {finalScore}");
    }
    
    System.Collections.IEnumerator FlashRed()
    {
        // Simple red flash effect
        var originalColor = Camera.main.backgroundColor;
        Camera.main.backgroundColor = Color.red;
        yield return new WaitForSeconds(0.1f);
        Camera.main.backgroundColor = originalColor;
    }
    
    System.Collections.IEnumerator FlashGreen()
    {
        // Simple green flash effect
        var originalColor = Camera.main.backgroundColor;
        Camera.main.backgroundColor = Color.green;
        yield return new WaitForSeconds(0.1f);
        Camera.main.backgroundColor = originalColor;
    }
}
```

## Step 4: Create the Sound Manager

This will play audio based on events:

```csharp
using UnityEngine;
using com.DvosTools.bus;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip scoreSound;
    
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        // Subscribe to events
        EventBus.RegisterUnityHandler<PlayerHealthChangedEvent>(OnHealthChanged);
        EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnPlayerDied);
        EventBus.RegisterUnityHandler<PlayerScoredEvent>(OnPlayerScored);
    }
    
    void OnHealthChanged(PlayerHealthChangedEvent evt)
    {
        if (evt.HealthChange < 0)
        {
            PlaySound(damageSound);
        }
        else if (evt.HealthChange > 0)
        {
            PlaySound(healSound);
        }
    }
    
    void OnPlayerDied(PlayerDiedEvent evt)
    {
        PlaySound(deathSound);
    }
    
    void OnPlayerScored(PlayerScoredEvent evt)
    {
        PlaySound(scoreSound);
    }
    
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}
```

## Step 5: Create the Game Manager

This will coordinate the overall game state:

```csharp
using UnityEngine;
using com.DvosTools.bus;

public class GameManager : MonoBehaviour
{
    private GameState currentState = GameState.Menu;
    private HealthSystem healthSystem;
    private ScoreSystem scoreSystem;
    
    void Start()
    {
        healthSystem = FindObjectOfType<HealthSystem>();
        scoreSystem = FindObjectOfType<ScoreSystem>();
        
        // Subscribe to events
        EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnPlayerDied);
        
        // Start the game
        ChangeGameState(GameState.Playing);
    }
    
    void Update()
    {
        // Handle input
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentState == GameState.Playing)
            {
                // Simulate taking damage
                healthSystem?.TakeDamage(10);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.H))
        {
            if (currentState == GameState.Playing)
            {
                // Simulate healing
                healthSystem?.Heal(20);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            // Restart game
            RestartGame();
        }
    }
    
    void OnPlayerDied(PlayerDiedEvent evt)
    {
        ChangeGameState(GameState.GameOver);
    }
    
    void ChangeGameState(GameState newState)
    {
        GameState oldState = currentState;
        currentState = newState;
        
        EventBus.Send(new GameStateChangedEvent
        {
            OldState = oldState,
            NewState = newState
        });
    }
    
    void RestartGame()
    {
        // Reset health
        if (healthSystem != null)
        {
            healthSystem.TakeDamage(-100); // Heal to full
        }
        
        // Reset score
        if (scoreSystem != null)
        {
            scoreSystem.ResetScore();
        }
        
        ChangeGameState(GameState.Playing);
    }
}
```

## Step 6: Create the Score System

This will handle scoring and send score events:

```csharp
using UnityEngine;
using com.DvosTools.bus;

public class ScoreSystem : MonoBehaviour
{
    private int totalScore = 0;
    
    public int TotalScore => totalScore;
    
    void Start()
    {
        // Subscribe to events
        EventBus.RegisterUnityHandler<PlayerDiedEvent>(OnPlayerDied);
    }
    
    public void AddScore(int points)
    {
        totalScore += points;
        
        EventBus.Send(new PlayerScoredEvent
        {
            Points = points,
            TotalScore = totalScore
        });
    }
    
    void OnPlayerDied(PlayerDiedEvent evt)
    {
        // Add bonus points for surviving
        AddScore(100);
    }
    
    public void ResetScore()
    {
        totalScore = 0;
    }
}
```

## Step 7: Set Up the Scene

1. Create a GameObject with `HealthSystem`, `ScoreSystem`, and `GameManager`
2. Create a UI Canvas with `UIManager`
3. Create a GameObject with `SoundManager` and an AudioSource
4. Set up the UI elements (health bar, score text, etc.)
5. Assign audio clips to the sound manager

## Step 8: Test Your System

Run the game and test:
- **Space** - Take damage (should flash red, play sound, update UI)
- **H** - Heal (should flash green, play sound, update UI)
- **R** - Restart game

## What We've Built

This complete system demonstrates:

✅ **Event-driven architecture** - Components communicate through events
✅ **Loose coupling** - No direct references between systems
✅ **Multiple subscribers** - UI, sound, and game manager all respond to the same events
✅ **Thread safety** - All handlers run on Unity's main thread
✅ **Easy extensibility** - Add new features by subscribing to existing events

## Key Benefits

1. **Modularity** - Each system is independent and focused
2. **Testability** - Easy to test individual components
3. **Maintainability** - Changes to one system don't affect others
4. **Extensibility** - Add new features without modifying existing code

## Next Steps

Now that you have a working event system, let's learn about threading and dispatchers:

**➡️ Continue to [05-Threading and Dispatchers](05-Threading-and-Dispatchers)**

---

## Troubleshooting

**Q: Events aren't being received**
A: Make sure you're registering handlers before sending events, and that the event types match exactly.

**Q: UI isn't updating**
A: Ensure you're using `RegisterUnityHandler` for UI updates, as Unity APIs must run on the main thread.

**Q: Sounds aren't playing**
A: Check that the AudioSource is properly configured and the audio clips are assigned.