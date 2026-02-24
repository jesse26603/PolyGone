using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

/// <summary>
/// Shown at startup.  The player must enter their Formbar server URL and API key
/// (found at /profile on the Formbar website) before the main menu becomes accessible.
/// </summary>
internal class FormbarLoginScene : IScene
{
    private SpriteFont? _font;
    private Texture2D? _pixel;
    private readonly ContentManager _content;
    private readonly SceneManager _sceneManager;
    private readonly GraphicsDeviceManager _graphics;

    // Text field state
    private string _serverUrl = "https://formbeta.yorktechapps.com";
    private string _apiKey = "";
    private int _focusedField = 0; // 0 = serverUrl, 1 = apiKey

    // Async login state
    private string _statusMessage = "";
    private bool _isLoading = false;
    private Task<(FormbarService.UserInfo? user, string? error)>? _loginTask;

    private KeyboardState _keyboardState;
    private KeyboardState _previousKeyboardState;

    public FormbarLoginScene(ContentManager content, SceneManager sceneManager, GraphicsDeviceManager graphics)
    {
        _content = content;
        _sceneManager = sceneManager;
        _graphics = graphics;
        _previousKeyboardState = Keyboard.GetState();
    }

    public void Load()
    {
        if (_font == null)
        {
            try { _font = _content.Load<SpriteFont>("Fonts/PauseMenu"); }
            catch { }
        }

        // Ensure purchase data is ready before the player can access levels
        PurchaseTracker.Load();
    }

    public void Update(GameTime gameTime)
    {
        _keyboardState = Keyboard.GetState();

        // ---- Check if the async login task has completed ----
        if (_loginTask != null && _loginTask.IsCompleted)
        {
            var (user, error) = _loginTask.Result;
            _loginTask = null;
            _isLoading = false;

            if (user != null)
            {
                // Populate session and reveal the main menu
                FormbarSession.UserId = user.Id;
                FormbarSession.DisplayName = user.DisplayName;
                FormbarSession.Digipogs = user.Digipogs;
                FormbarSession.ApiKey = _apiKey;
                FormbarSession.ServerUrl = _serverUrl;
                FormbarSession.IsLoggedIn = true;
                _sceneManager.PopScene(this);
            }
            else
            {
                _statusMessage = $"Login failed: {error}";
            }
        }

        if (_isLoading)
        {
            _previousKeyboardState = _keyboardState;
            return;
        }

        // ---- Process typed characters ----
        string typed = InputManager.ConsumeTypedCharacters();
        foreach (char c in typed)
        {
            if (c == '\b')
            {
                // Backspace
                if (_focusedField == 0 && _serverUrl.Length > 0)
                    _serverUrl = _serverUrl[..^1];
                else if (_focusedField == 1 && _apiKey.Length > 0)
                    _apiKey = _apiKey[..^1];
            }
            else if (c >= ' ')
            {
                if (_focusedField == 0)
                    _serverUrl += c;
                else
                    _apiKey += c;
            }
        }

        // Tab switches focus between fields
        if (IsKeyPressed(Keys.Tab))
            _focusedField = (_focusedField + 1) % 2;

        // Enter submits when the API key is not empty
        if (IsKeyPressed(Keys.Enter) && !string.IsNullOrWhiteSpace(_apiKey))
            StartLogin();

        // ---- Mouse clicks ----
        if (_font != null && InputManager.IsLeftMouseButtonClicked())
        {
            var viewport = _graphics.GraphicsDevice.Viewport;
            var mousePos = InputManager.GetMousePosition();

            float urlFieldY = viewport.Height / 2f - 80f;
            var urlBounds = new Rectangle(viewport.Width / 2 - 200, (int)urlFieldY - 3, 400, 28);
            if (urlBounds.Contains(mousePos))
            {
                _focusedField = 0;
                InputManager.ConsumeClick();
            }

            float apiFieldY = viewport.Height / 2f;
            var apiBounds = new Rectangle(viewport.Width / 2 - 200, (int)apiFieldY - 3, 400, 28);
            if (apiBounds.Contains(mousePos))
            {
                _focusedField = 1;
                InputManager.ConsumeClick();
            }

            float buttonY = viewport.Height / 2f + 80f;
            string buttonLabel = "Login";
            var buttonSize = _font.MeasureString(buttonLabel);
            var buttonBounds = new Rectangle(
                (int)(viewport.Width / 2f - buttonSize.X / 2f) - 10,
                (int)buttonY - 5,
                (int)buttonSize.X + 20,
                (int)buttonSize.Y + 10);
            if (buttonBounds.Contains(mousePos) && !string.IsNullOrWhiteSpace(_apiKey))
            {
                StartLogin();
                InputManager.ConsumeClick();
            }
        }

        _previousKeyboardState = _keyboardState;
    }

    private void StartLogin()
    {
        _isLoading = true;
        _statusMessage = "Logging in...";
        _loginTask = FormbarService.GetCurrentUser(_serverUrl, _apiKey);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixel == null)
        {
            _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        var viewport = spriteBatch.GraphicsDevice.Viewport;

        // Background
        spriteBatch.Draw(_pixel,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            new Color(15, 25, 50));

        if (_font == null) return;

        // ---- Title ----
        DrawCentered(spriteBatch, viewport, "Formbar Login",
            viewport.Height / 4f, Color.White, 1f);
        DrawCentered(spriteBatch, viewport,
            "Enter your Formbar API key to continue. (Find it at /profile on the Formbar site)",
            viewport.Height / 4f + 40f, Color.LightGray, 1f);

        // ---- Server URL field ----
        float urlLabelY = viewport.Height / 2f - 110f;
        spriteBatch.DrawString(_font, "Server URL:",
            new Vector2(viewport.Width / 2f - 200f, urlLabelY), Color.White);

        float urlFieldY = viewport.Height / 2f - 80f;
        DrawTextField(spriteBatch, viewport, _serverUrl, urlFieldY, _focusedField == 0, masked: false);

        // ---- API Key field ----
        float apiLabelY = viewport.Height / 2f - 30f;
        spriteBatch.DrawString(_font, "API Key:",
            new Vector2(viewport.Width / 2f - 200f, apiLabelY), Color.White);

        float apiFieldY = viewport.Height / 2f;
        DrawTextField(spriteBatch, viewport, _apiKey, apiFieldY, _focusedField == 1, masked: true);

        // ---- Login button ----
        float buttonY = viewport.Height / 2f + 80f;
        string buttonLabel = _isLoading ? "Logging in..." : "Login";
        Color buttonBg = _isLoading ? Color.Gray : Color.DarkGreen;
        var btnSize = _font.MeasureString(buttonLabel);
        float btnX = viewport.Width / 2f - btnSize.X / 2f;
        spriteBatch.Draw(_pixel,
            new Rectangle((int)btnX - 10, (int)buttonY - 5, (int)btnSize.X + 20, (int)btnSize.Y + 10),
            buttonBg);
        spriteBatch.DrawString(_font, buttonLabel, new Vector2(btnX, buttonY), Color.White);

        // ---- Status message ----
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            Color statusColor = _statusMessage.StartsWith("Login failed", StringComparison.OrdinalIgnoreCase)
                ? Color.OrangeRed
                : Color.LightGray;
            DrawCentered(spriteBatch, viewport, _statusMessage,
                viewport.Height / 2f + 130f, statusColor, 1f);
        }
    }

    private void DrawTextField(SpriteBatch spriteBatch, Viewport viewport,
        string value, float fieldY, bool focused, bool masked)
    {
        Color borderColor = focused ? Color.Yellow : Color.DimGray;
        spriteBatch.Draw(_pixel,
            new Rectangle(viewport.Width / 2 - 200, (int)fieldY - 3, 400, 28),
            borderColor);
        spriteBatch.Draw(_pixel,
            new Rectangle(viewport.Width / 2 - 198, (int)fieldY - 1, 396, 24),
            Color.Black);

        string display;
        if (masked && value.Length > 0)
        {
            // Show only the last 4 chars; mask the rest
            int visible = Math.Min(4, value.Length);
            display = new string('*', value.Length - visible) + value[^visible..];
        }
        else
        {
            display = value;
        }

        if (focused) display += "|";

        spriteBatch.DrawString(_font!, display,
            new Vector2(viewport.Width / 2f - 195f, fieldY),
            Color.White);
    }

    private void DrawCentered(SpriteBatch spriteBatch, Viewport viewport,
        string text, float y, Color color, float scale)
    {
        var size = _font!.MeasureString(text) * scale;
        spriteBatch.DrawString(_font, text,
            new Vector2(viewport.Width / 2f - size.X / 2f, y),
            color);
    }

    private bool IsKeyPressed(Keys key) =>
        _keyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
}
