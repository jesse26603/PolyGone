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
    internal class PauseScene : IScene
    {
        private Texture2D _pixel;
        private SpriteFont _font;
        private KeyboardState keyboardState;
        private KeyboardState previousKeyboardState;
        private readonly ContentManager _content;
        private readonly SceneManager _sceneManager;
        private readonly GraphicsDeviceManager _graphics;
        private readonly string[] _options = { "Continue", "Exit to Menu" };
        private int _selectedIndex;

        public PauseScene(ContentManager content, SceneManager sceneManager, GraphicsDeviceManager graphics)
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

            if (IsKeyPressed(Keys.W))
            {
                _selectedIndex = (_selectedIndex - 1 + _options.Length) % _options.Length;
            }

            if (IsKeyPressed(Keys.S))
            {
                _selectedIndex = (_selectedIndex + 1) % _options.Length;
            }

            if (IsKeyPressed(Keys.Enter))
            {
                if (_selectedIndex == 0)
                {
                    _sceneManager.RemoveScene(this);
                }
                else if (_selectedIndex == 1)
                {
                    _sceneManager.RemoveScene(this);
                    _sceneManager.RemoveScene(_sceneManager.GetCurrentScene());
                    _sceneManager.AddScene(new MenuScene(_content, _sceneManager, _graphics));
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
                var startY = viewport.Height / 2f - (_options.Length * 40f) / 2f;

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