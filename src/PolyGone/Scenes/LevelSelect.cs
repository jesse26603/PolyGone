using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone
{
    internal class LevelSelect : IScene
    {
        private Texture2D? _pixel;
        private SpriteFont? _font;
        private KeyboardState keyboardState;
        private KeyboardState previousKeyboardState;
        private readonly ContentManager _content;
        private readonly SceneManager _sceneManager;
        private readonly GraphicsDeviceManager _graphics;
        private readonly string[] _levelNames = { "Test Level 1", "Test Level 2", "Test Level 3", "Back to Menu" };
        private readonly string?[] _levelFiles = { "TestLevel", "TestLevel2", "TestLevel3", null };
        private int _selectedIndex;

        public LevelSelect(ContentManager content, SceneManager sceneManager, GraphicsDeviceManager graphics)
        {
            _pixel = null;
            _content = content;
            _sceneManager = sceneManager;
            _graphics = graphics;
            previousKeyboardState = Keyboard.GetState();
            _selectedIndex = 0;
        }

        public void Load()
        {
            if (_font == null)
            {
                try
                {
                    _font = _content.Load<SpriteFont>("Fonts/PauseMenu");
                }
                catch
                {
                    // Font not available
                }
            }
        }

        public void Update(GameTime gameTime)
        {
            keyboardState = Keyboard.GetState();

            // Mouse navigation
            if (_font != null)
            {
                var viewport = _graphics.GraphicsDevice.Viewport;
                var startY = viewport.Height / 2f - _levelNames.Length * 40f / 2f;

                for (var i = 0; i < _levelNames.Length; i++)
                {
                    var levelName = _levelNames[i];
                    var textSize = _font.MeasureString(levelName);
                    var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, startY + i * 40f);
                    var bounds = new Rectangle((int)position.X, (int)position.Y, (int)textSize.X, (int)textSize.Y);

                    if (bounds.Contains(InputManager.GetMousePosition()))
                    {
                        _selectedIndex = i;
                        
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
                _selectedIndex = (_selectedIndex - 1 + _levelNames.Length) % _levelNames.Length;
            }

            if (IsKeyPressed(Keys.Down))
            {
                _selectedIndex = (_selectedIndex + 1) % _levelNames.Length;
            }

            if (IsKeyPressed(Keys.Enter))
            {
                ExecuteSelection();
            }

            if (IsKeyPressed(Keys.Escape))
            {
                // Also allow Escape to go back
                _sceneManager.PopScene(this);
            }

            previousKeyboardState = keyboardState;
        }

        private void ExecuteSelection()
        {
            if (_selectedIndex == _levelNames.Length - 1)
            {
                // Back to Menu
                _sceneManager.PopScene(this);
            }
            else
            {
                // Go to inventory management with selected level
                string? levelFile = _levelFiles[_selectedIndex];
                if (levelFile != null)
                {
                    _sceneManager.AddScene(new InventoryManagement(_content, _sceneManager, _graphics, levelFile));
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }

            // Draw background
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height), Color.DarkSlateGray);

            if (_font != null)
            {
                var viewport = spriteBatch.GraphicsDevice.Viewport;
                
                // Draw title
                string title = "Select a Level";
                var titleSize = _font.MeasureString(title);
                var titlePos = new Vector2(viewport.Width / 2f - titleSize.X / 2f, 100);
                spriteBatch.DrawString(_font, title, titlePos, Color.White);

                // Draw level options
                var startY = viewport.Height / 2f - _levelNames.Length * 40f / 2f;

                for (var i = 0; i < _levelNames.Length; i++)
                {
                    var levelName = _levelNames[i];
                    var color = i == _selectedIndex ? Color.Yellow : Color.White;
                    var textSize = _font.MeasureString(levelName);
                    var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, startY + i * 40f);

                    spriteBatch.DrawString(_font, levelName, position, color);
                }
            }
        }

        private bool IsKeyPressed(Keys key)
        {
            return keyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
        }
    }
}
