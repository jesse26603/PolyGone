using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System.Linq;

namespace PolyGone;

internal class ControlsScene : IScene
{
    private Texture2D? _pixel;
    private SpriteFont? _font;
    private readonly ContentManager _content;
    private readonly SceneManager _sceneManager;
    private readonly GraphicsDeviceManager _graphics;

    private int _selectedIndex;
    private int _selectedColumn;
    private bool _listeningForInput;

    private const float RowSpacing = 42f;
    private const float ActionColumnXOffset = -300f;
    private const float KeyboardColumnXOffset = -10f;
    private const float GamepadColumnXOffset = 250f;

    private int LastRowIndex => InputManager.RemappableActions.Count + 1; // + reset + back

    public ControlsScene(ContentManager content, SceneManager sceneManager, GraphicsDeviceManager graphics)
    {
        _content = content;
        _sceneManager = sceneManager;
        _graphics = graphics;
        _selectedIndex = 0;
        _selectedColumn = 1;
        InputManager.ResetClickCooldown();
    }

    public void Load()
    {
        if (_font == null)
        {
            var fontAssetPath = Path.Combine(_content.RootDirectory, "Fonts", "PauseMenu.xnb");
            if (File.Exists(fontAssetPath))
            {
                _font = _content.Load<SpriteFont>("Fonts/PauseMenu");
            }
        }
    }

    public void Update(GameTime gameTime)
    {
        if (_listeningForInput)
        {
            HandleRebindInput();
            return;
        }

        if (InputManager.MenuUp())
        {
            _selectedIndex = (_selectedIndex - 1 + LastRowIndex + 1) % (LastRowIndex + 1);
        }
        if (InputManager.MenuDown())
        {
            _selectedIndex = (_selectedIndex + 1) % (LastRowIndex + 1);
        }

        if (_selectedIndex < InputManager.RemappableActions.Count)
        {
            if (InputManager.MenuLeft()) { _selectedColumn = 1; }
            if (InputManager.MenuRight()) { _selectedColumn = 2; }
        }
        else
        {
            _selectedColumn = 0;
        }

        if (InputManager.MenuConfirm())
        {
            if (_selectedIndex == InputManager.RemappableActions.Count)
            {
                InputManager.ResetBindingsToDefaults();
                InputManager.SaveBindings();
                return;
            }

            if (_selectedIndex == LastRowIndex)
            {
                _sceneManager.PopScene(this);
                return;
            }

            _listeningForInput = true;
            InputManager.ResetClickCooldown();
        }

        if (InputManager.MenuBack())
        {
            _sceneManager.PopScene(this);
        }
    }

    private void HandleRebindInput()
    {
        if (InputManager.MenuBack())
        {
            _listeningForInput = false;
            return;
        }

        if (_selectedIndex < 0 || _selectedIndex >= InputManager.RemappableActions.Count)
        {
            _listeningForInput = false;
            return;
        }

        var action = InputManager.RemappableActions[_selectedIndex];
        if (_selectedColumn == 1 && InputManager.IsKeyboardRebindable(action))
        {
            var key = InputManager.GetJustPressedKeys()
                .Where(k => k != Keys.Escape)
                .OrderBy(k => k)
                .FirstOrDefault();
            if (key != Keys.None)
            {
                InputManager.SetKeyboardBinding(action, key);
                _listeningForInput = false;
            }
        }
        else if (_selectedColumn == 2 && InputManager.IsGamepadRebindable(action))
        {
            var button = InputManager.GetJustPressedGamepadBinding();
            if (button.HasValue)
            {
                InputManager.SetGamepadBinding(action, button.Value);
                _listeningForInput = false;
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

        spriteBatch.Draw(_pixel, new Rectangle(0, 0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height), Color.Gray);
        if (_font == null)
        {
            return;
        }

        var viewport = spriteBatch.GraphicsDevice.Viewport;
        var startY = viewport.Height / 2f - (LastRowIndex + 2) * RowSpacing / 2f;

        DrawCentered(spriteBatch, "Controls", viewport.Width / 2f, startY - 60f, Color.LightGray);
        DrawCentered(spriteBatch, "Action", viewport.Width / 2f + ActionColumnXOffset, startY, Color.LightGray);
        DrawCentered(spriteBatch, "Keyboard", viewport.Width / 2f + KeyboardColumnXOffset, startY, Color.LightGray);
        DrawCentered(spriteBatch, "Controller", viewport.Width / 2f + GamepadColumnXOffset, startY, Color.LightGray);

        for (int i = 0; i < InputManager.RemappableActions.Count; i++)
        {
            var action = InputManager.RemappableActions[i];
            var y = startY + (i + 1) * RowSpacing;
            bool rowSelected = i == _selectedIndex;

            DrawCentered(spriteBatch, InputManager.GetActionDisplayName(action), viewport.Width / 2f + ActionColumnXOffset, y, rowSelected ? Color.Yellow : Color.White);

            string keyText = InputManager.GetKeyboardBinding(action)?.ToString() ?? "-";
            string gamepadText = InputManager.GetGamepadBinding(action)?.ToString() ?? "-";

            DrawCentered(spriteBatch, keyText, viewport.Width / 2f + KeyboardColumnXOffset, y,
                rowSelected && _selectedColumn == 1 ? Color.Yellow : Color.White);
            DrawCentered(spriteBatch, gamepadText, viewport.Width / 2f + GamepadColumnXOffset, y,
                rowSelected && _selectedColumn == 2 ? Color.Yellow : Color.White);
        }

        var resetY = startY + (InputManager.RemappableActions.Count + 1) * RowSpacing;
        DrawCentered(spriteBatch, "Reset Defaults", viewport.Width / 2f, resetY, _selectedIndex == InputManager.RemappableActions.Count ? Color.OrangeRed : Color.White);

        var backY = startY + (InputManager.RemappableActions.Count + 2) * RowSpacing;
        DrawCentered(spriteBatch, "Back", viewport.Width / 2f, backY, _selectedIndex == LastRowIndex ? Color.Yellow : Color.White);

        if (_listeningForInput)
        {
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.6f);
            DrawCentered(spriteBatch, "Press a key/button to rebind", viewport.Width / 2f, viewport.Height / 2f - 20f, Color.White);
            DrawCentered(spriteBatch, "Esc/B to cancel", viewport.Width / 2f, viewport.Height / 2f + 20f, Color.LightGray);
        }
    }

    private void DrawCentered(SpriteBatch spriteBatch, string text, float centerX, float y, Color color)
    {
        if (_font == null)
        {
            return;
        }

        var size = _font.MeasureString(text);
        spriteBatch.DrawString(_font, text, new Vector2(centerX - size.X / 2f, y), color);
    }
}
