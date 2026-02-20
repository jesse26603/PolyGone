using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

public class WinScene : IScene
{
    private readonly ContentManager contentManager;
    private readonly SceneManager sceneManager;
    private readonly GraphicsDeviceManager graphics;
    private readonly string currentLevel;
    private readonly List<ItemType> selectedItems;
    private readonly WeaponType selectedWeapon;
    private SpriteFont font;
    private Texture2D pixel;
    private KeyboardState keyboardState;
    private KeyboardState previousKeyboardState;
    private readonly string[] options;
    private int selectedIndex;
    private static List<string>? levelOrder;

    public WinScene(ContentManager contentManager, SceneManager sceneManager, GraphicsDeviceManager graphics, string currentLevel = "TestLevel", List<ItemType> selectedItems = null, WeaponType selectedWeapon = WeaponType.Blaster)
    {
        this.contentManager = contentManager;
        this.sceneManager = sceneManager;
        this.graphics = graphics;
        this.currentLevel = currentLevel;
        this.selectedItems = selectedItems ?? new List<ItemType>();
        this.selectedWeapon = selectedWeapon;
        
        // Build options list based on whether there's a next level
        string? nextLevel = GetNextLevel(currentLevel);
        if (nextLevel != null)
        {
            options = new[] { "Next Level", "Level Select", "Main Menu" };
        }
        else
        {
            options = new[] { "Level Select", "Main Menu" };
        }
        
        previousKeyboardState = Keyboard.GetState();
        selectedIndex = 0;
    }

    public void Load()
    {
        if (font == null)
        {
            font = contentManager.Load<SpriteFont>("Fonts/PauseMenu");
        }
    }

    // Clean up resources to prevent memory leaks
    public void Unload()
    {
        pixel?.Dispose();
        pixel = null;
    }

    public void Update(GameTime gameTime)
    {
        keyboardState = Keyboard.GetState();

        // Mouse navigation
        if (font != null)
        {
            var viewport = graphics.GraphicsDevice.Viewport;
            var startY = viewport.Height / 2f + 50f; // Below the "Level Cleared!" text

            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var textSize = font.MeasureString(option);
                var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, startY + i * 40f);
                var bounds = new Rectangle((int)position.X, (int)position.Y, (int)textSize.X, (int)textSize.Y);

                if (bounds.Contains(InputManager.GetMousePosition()))
                {
                    selectedIndex = i;
                    
                    // Mouse click with InputManager
                    if (InputManager.IsLeftMouseButtonClicked())
                    {
                        ExecuteSelection();
                        InputManager.ConsumeClick();
                    }
                }
            }
        }

        // Keyboard navigation
        if (IsKeyPressed(Keys.Up))
        {
            selectedIndex = (selectedIndex - 1 + options.Length) % options.Length;
        }

        if (IsKeyPressed(Keys.Down))
        {
            selectedIndex = (selectedIndex + 1) % options.Length;
        }

        if (IsKeyPressed(Keys.Enter))
        {
            ExecuteSelection();
        }

        previousKeyboardState = keyboardState;
    }
    
    private bool IsKeyPressed(Keys key)
    {
        return keyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
    }
    
    private void ExecuteSelection()
    {
        string selectedOption = options[selectedIndex];
        
        if (selectedOption == "Next Level")
        {
            string? nextLevel = GetNextLevel(currentLevel);
            if (nextLevel != null)
            {
                sceneManager.PopScene(this); // Remove WinScene
                sceneManager.PopScene(sceneManager.GetCurrentScene()); // Remove old GameScene
                sceneManager.AddScene(new GameScene(contentManager, sceneManager, graphics, nextLevel, selectedItems, selectedWeapon));
                InputManager.ResetClickCooldown();
            }
        }
        else if (selectedOption == "Level Select")
        {
            sceneManager.PopScene(this); // Remove WinScene
            
            // Keep popping until we reach LevelSelect
            while (sceneManager.GetCurrentScene() != null && sceneManager.GetCurrentScene() is not LevelSelect)
            {
                sceneManager.PopScene(sceneManager.GetCurrentScene());
            }
            
            InputManager.ResetClickCooldown();
        }
        else if (selectedOption == "Main Menu")
        {
            sceneManager.PopScene(this); // Remove WinScene
            
            // Keep popping until we reach MenuScene
            while (sceneManager.GetCurrentScene() != null && sceneManager.GetCurrentScene() is not MenuScene)
            {
                sceneManager.PopScene(sceneManager.GetCurrentScene());
            }
            
            InputManager.ResetClickCooldown();
        }
    }
    
    private static void LoadLevelOrder()
    {
        if (levelOrder != null) return; // Already loaded
        
        try
        {
            string jsonPath = Path.Combine("../../../Content/Maps/LevelOrder.json");
            string jsonContent = File.ReadAllText(jsonPath);
            using JsonDocument doc = JsonDocument.Parse(jsonContent);
            JsonElement root = doc.RootElement;
            JsonElement levelsArray = root.GetProperty("levels");
            
            levelOrder = new List<string>();
            foreach (JsonElement level in levelsArray.EnumerateArray())
            {
                levelOrder.Add(level.GetString() ?? "");
            }
        }
        catch
        {
            // Fallback to hardcoded levels if file doesn't exist
            levelOrder = new List<string> { "TestLevel", "TestLevel2", "TestLevel3" };
        }
    }
    
    private string? GetNextLevel(string currentLevel)
    {
        LoadLevelOrder();
        
        if (levelOrder == null || levelOrder.Count == 0)
        {
            return null;
        }
        
        int currentIndex = levelOrder.IndexOf(currentLevel);
        if (currentIndex == -1 || currentIndex == levelOrder.Count - 1)
        {
            return null; // Current level not found or it's the last level
        }
        
        return levelOrder[currentIndex + 1];
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (pixel == null)
        {
            pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        // Draw gray background
        spriteBatch.Draw(pixel, new Rectangle(0, 0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height), Color.Gray);

        if (font != null)
        {
            var viewport = spriteBatch.GraphicsDevice.Viewport;
            
            // Draw "Level Cleared!" text
            string winText = "Level Cleared!";
            var winTextSize = font.MeasureString(winText);
            var winTextPosition = new Vector2(viewport.Width / 2f - winTextSize.X / 2f, viewport.Height / 2f - 100f);
            spriteBatch.DrawString(font, winText, winTextPosition, Color.Gold);

            // Draw menu options
            var startY = viewport.Height / 2f + 50f;

            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var color = i == selectedIndex ? Color.Yellow : Color.White;
                var textSize = font.MeasureString(option);
                var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, startY + i * 40f);

                spriteBatch.DrawString(font, option, position, color);
            }
        }
    }
}
