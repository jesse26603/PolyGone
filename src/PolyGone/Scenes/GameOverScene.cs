using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

internal class GameOverScene : IScene
{
    private Texture2D? pixel;
    private SpriteFont? font;
    private KeyboardState keyboardState;
    private KeyboardState previousKeyboardState;
    private readonly ContentManager content;
    private readonly SceneManager sceneManager;
    private readonly GraphicsDeviceManager graphics;
    private readonly GameScene gameScene;
    private readonly string[] options = { "Restart Level", "Change Loadout", "Level Select", "Main Menu" };
    private int selectedIndex;

    public GameOverScene(ContentManager content, SceneManager sceneManager, GraphicsDeviceManager graphics, GameScene gameScene)
    {
        this.content = content;
        this.sceneManager = sceneManager;
        this.graphics = graphics;
        this.gameScene = gameScene;
        previousKeyboardState = Keyboard.GetState();
        selectedIndex = 0;
    }

    public void Load()
    {
        if (font == null)
        {
            font = content.Load<SpriteFont>("Fonts/PauseMenu");
        }
    }

    public void Update(GameTime gameTime)
    {
        keyboardState = Keyboard.GetState();

        // Mouse navigation
        if (font != null)
        {
            var viewport = graphics.GraphicsDevice.Viewport;
            var startY = viewport.Height / 2f + 30f;

            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var textSize = font.MeasureString(option);
                var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, startY + i * 45f);
                var bounds = new Rectangle((int)position.X, (int)position.Y, (int)textSize.X, (int)textSize.Y);

                if (bounds.Contains(InputManager.GetMousePosition()))
                {
                    selectedIndex = i;

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

    private void ExecuteSelection()
    {
        string levelName = gameScene.GetLevelName();
        List<ItemType> currentItems = gameScene.GetSelectedItems();
        WeaponType currentWeapon = gameScene.GetSelectedWeapon();

        switch (options[selectedIndex])
        {
            case "Restart Level":
                sceneManager.PopScene(this);
                sceneManager.PopScene(gameScene);
                sceneManager.AddScene(new GameScene(content, sceneManager, graphics, levelName, currentItems, currentWeapon));
                InputManager.ResetClickCooldown();
                break;

            case "Change Loadout":
                sceneManager.PopScene(this);
                sceneManager.PopScene(gameScene);
                sceneManager.AddScene(new InventoryManagement(content, sceneManager, graphics, levelName));
                InputManager.ResetClickCooldown();
                break;

            case "Level Select":
                sceneManager.PopScene(this);
                sceneManager.PopScene(gameScene);
                // Pop any scenes between here and LevelSelect
                while (sceneManager.GetCurrentScene() != null && sceneManager.GetCurrentScene() is not LevelSelect)
                {
                    sceneManager.PopScene(sceneManager.GetCurrentScene());
                }
                // If no LevelSelect found in the stack, push one
                if (sceneManager.GetCurrentScene() is not LevelSelect)
                {
                    sceneManager.AddScene(new LevelSelect(content, sceneManager, graphics));
                }
                InputManager.ResetClickCooldown();
                break;

            case "Main Menu":
                sceneManager.PopScene(this);
                sceneManager.PopScene(gameScene);
                while (sceneManager.GetCurrentScene() != null && sceneManager.GetCurrentScene() is not MenuScene)
                {
                    sceneManager.PopScene(sceneManager.GetCurrentScene());
                }
                if (sceneManager.GetCurrentScene() is not MenuScene)
                {
                    sceneManager.AddScene(new MenuScene(content, sceneManager, graphics));
                }
                InputManager.ResetClickCooldown();
                break;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (pixel == null)
        {
            pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });
        }

        var viewport = spriteBatch.GraphicsDevice.Viewport;

        // Dark red overlay
        spriteBatch.Draw(pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.DarkRed * 0.85f);

        if (font != null)
        {
            // "Game Over" title
            string title = "Game Over";
            var titleSize = font.MeasureString(title);
            var titlePos = new Vector2(viewport.Width / 2f - titleSize.X / 2f, viewport.Height / 2f - 120f);
            spriteBatch.DrawString(font, title, titlePos + new Vector2(2, 2), Color.Black * 0.6f); // drop shadow
            spriteBatch.DrawString(font, title, titlePos, Color.OrangeRed);

            // Menu options
            var startY = viewport.Height / 2f + 30f;

            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var color = i == selectedIndex ? Color.Yellow : Color.White;
                var textSize = font.MeasureString(option);
                var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, startY + i * 45f);

                // Drop shadow for selected item
                if (i == selectedIndex)
                {
                    spriteBatch.DrawString(font, option, position + new Vector2(2, 2), Color.Black * 0.5f);
                }

                spriteBatch.DrawString(font, option, position, color);
            }
        }
    }

    public void Unload()
    {
        pixel?.Dispose();
        pixel = null!;
    }

    private bool IsKeyPressed(Keys key)
    {
        return keyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
    }
}
