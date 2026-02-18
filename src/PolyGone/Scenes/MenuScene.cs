/* Main Menu
[] Level Select (sub-menu)
[] Options (sub-menu)
[] Exit Game
*/

/* Pause Menu
[] Continue
[] Exit to Menu
*/
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System.Text.Json;
using System.Linq;
using System;

namespace PolyGone
{
    internal class MenuScene : IScene
    {
        private Texture2D _pixel;
        private SpriteFont _font;
        private KeyboardState keyboardState;
        private KeyboardState previousKeyboardState;
        private readonly ContentManager _content;
        private readonly SceneManager _sceneManager;
        private readonly GraphicsDeviceManager _graphics;
        private readonly string[] _options = { "Test Level 1", "Test Level 2", "Test Level 3", "Exit to Desktop" };
        private int _selectedIndex;

        public MenuScene(ContentManager content, SceneManager sceneManager, GraphicsDeviceManager graphics)
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
                _font = _content.Load<SpriteFont>("Fonts/PauseMenu");
            }
        }

        public void Update(GameTime gameTime)
        {
            keyboardState = Keyboard.GetState();

            if (IsKeyPressed(Keys.Up))
            {
                _selectedIndex = (_selectedIndex - 1 + _options.Length) % _options.Length;
            }

            if (IsKeyPressed(Keys.Down))
            {
                _selectedIndex = (_selectedIndex + 1) % _options.Length;
            }

            if (IsKeyPressed(Keys.Enter))
            {
                if (_selectedIndex == 0)
                {
                    _sceneManager.PopScene(this);
                    _sceneManager.AddScene(new GameScene(_content, _sceneManager, _graphics));
                }
                else if (_selectedIndex == 1)
                {
                    _sceneManager.PopScene(this);
                    _sceneManager.AddScene(new GameScene(_content, _sceneManager, _graphics, "goog.."));
                }
                else if (_selectedIndex == 2)
                {
                    _sceneManager.PopScene(this);
                    _sceneManager.AddScene(new GameScene(_content, _sceneManager, _graphics, "FishLevel"));
                }
                else if (_selectedIndex == _options.Length - 1)
                {
                    Environment.Exit(0);
                }
            }

            previousKeyboardState = keyboardState;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }

            // Use the already-begun SpriteBatch from Game1.Draw
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height), Color.Gray);

            if (_font != null)
            {
                var viewport = spriteBatch.GraphicsDevice.Viewport;
                var startY = viewport.Height / 2f - _options.Length * 40f / 2f;

                for (var i = 0; i < _options.Length; i++)
                {
                    var option = _options[i];
                    var color = i == _selectedIndex ? Color.Yellow : Color.White;
                    var textSize = _font.MeasureString(option);
                    var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, startY + i * 40f);

                    spriteBatch.DrawString(_font, option, position, color);
                }
            }
        }

        private bool IsKeyPressed(Keys key)
        {
            return keyboardState.IsKeyDown(key) && !previousKeyboardState.IsKeyDown(key);
        }


    }
}