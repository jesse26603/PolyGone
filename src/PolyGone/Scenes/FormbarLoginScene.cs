using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

/// <summary>
/// Shown at startup.  The player authenticates via Formbar Passport (OAuth):
/// a browser window opens to the Formbar login page and, after successful login,
/// Formbar redirects back to a local HTTP server running inside the game.
/// The JWT token in that redirect is decoded directly – no API key is required.
/// </summary>
internal class FormbarLoginScene : IScene
{
    private enum LoginState { Idle, WaitingForCallback }

    private SpriteFont? _font;
    private Texture2D? _pixel;
    private readonly ContentManager _content;
    private readonly SceneManager _sceneManager;
    private readonly GraphicsDeviceManager _graphics;

    // Editable server URL
    private string _serverUrl = "https://formbeta.yorktechapps.com";

    private LoginState _state = LoginState.Idle;
    private string _statusMessage = "";

    // The OAuth callback URL shown to the user if the browser did not open
    private string _oauthUrl = "";

    // Local HTTP listener for the OAuth redirect callback
    private HttpListener? _listener;
    private Task<HttpListenerContext>? _callbackTask;

    private KeyboardState _keyboardState;
    private KeyboardState _previousKeyboardState;

    /// <summary>Local port used to receive the Formbar OAuth redirect.</summary>
    private const int CallbackPort = 59200;

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

    public void Unload()
    {
        StopListener();
    }

    public void Update(GameTime gameTime)
    {
        _keyboardState = Keyboard.GetState();

        // ---- Check whether the OAuth callback task has completed ----
        if (_callbackTask != null && _callbackTask.IsCompleted)
        {
            ProcessCallbackResult();
        }

        if (_state == LoginState.Idle)
        {
            HandleIdleInput();
        }
        else // WaitingForCallback
        {
            // Escape / Cancel aborts the flow and restores the idle screen
            if (InputManager.IsEscapeKeyPressed() || IsCancelButtonClicked())
            {
                CancelOAuth();
            }
        }

        _previousKeyboardState = _keyboardState;
    }

    // -----------------------------------------------------------------------
    // OAuth flow
    // -----------------------------------------------------------------------

    private void StartOAuth()
    {
        // Validate that the server URL uses http or https to prevent arbitrary process execution
        if (!_serverUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !_serverUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _statusMessage = "Server URL must start with http:// or https://";
            return;
        }

        try
        {
            // Local callback URL (must end with "/" for HttpListener)
            string prefix = $"http://localhost:{CallbackPort}/";
            // redirectURL sent to Formbar (without trailing slash)
            string redirectUrl = $"http://localhost:{CallbackPort}/login";
            _oauthUrl = $"{_serverUrl.TrimEnd('/')}/oauth?redirectURL={Uri.EscapeDataString(redirectUrl)}";

            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            _callbackTask = _listener.GetContextAsync();

            // Open the user's default browser at the Formbar OAuth URL
            Process.Start(new ProcessStartInfo { FileName = _oauthUrl, UseShellExecute = true });

            _state = LoginState.WaitingForCallback;
            _statusMessage = "";
        }
        catch (Exception ex)
        {
            StopListener();
            _statusMessage = $"Could not start login: {ex.Message}";
        }
    }

    private void ProcessCallbackResult()
    {
        var task = _callbackTask!;
        _callbackTask = null;

        // Faulted = listener was stopped (e.g. user clicked Cancel)
        if (task.IsFaulted || task.IsCanceled)
        {
            StopListener();
            if (_state == LoginState.WaitingForCallback)
            {
                _state = LoginState.Idle;
                _statusMessage = "";
            }
            return;
        }

        try
        {
            var context = task.Result;

            // Send a friendly HTML response so the browser tab shows a success page
            SendBrowserResponse(context,
                "Login successful! You can close this tab and return to PolyGone.");

            StopListener();

            // Extract the ?token= query parameter
            string query = context.Request.Url?.Query ?? "";
            string? jwt = ParseQueryParam(query, "token");

            if (jwt != null)
            {
                var userInfo = FormbarService.DecodeJwt(jwt);
                if (userInfo != null)
                {
                    // Store the JWT as the auth token (used for Digipog transfers)
                    FormbarSession.ApiKey = jwt;
                    FormbarSession.ServerUrl = _serverUrl;
                    FormbarSession.UserId = userInfo.Id;
                    FormbarSession.DisplayName = userInfo.DisplayName;
                    FormbarSession.IsLoggedIn = true;
                    _sceneManager.PopScene(this);
                    return;
                }
            }

            // Token was missing or unreadable
            _state = LoginState.Idle;
            _statusMessage = "Login failed: could not read account info from token.";
        }
        catch (Exception ex)
        {
            StopListener();
            _state = LoginState.Idle;
            _statusMessage = $"Login error: {ex.Message}";
        }
    }

    private void CancelOAuth()
    {
        StopListener();
        _callbackTask = null; // don't process result after the listener has faulted
        _state = LoginState.Idle;
        _statusMessage = "";
    }

    private void StopListener()
    {
        try { _listener?.Stop(); } catch { }
        _listener = null;
    }

    // -----------------------------------------------------------------------
    // Input helpers
    // -----------------------------------------------------------------------

    private void HandleIdleInput()
    {
        // Server URL text field – process typed characters
        string typed = InputManager.ConsumeTypedCharacters();
        foreach (char c in typed)
        {
            if (c == '\b') { if (_serverUrl.Length > 0) _serverUrl = _serverUrl[..^1]; }
            else if (c >= ' ') _serverUrl += c;
        }

        // Enter triggers login
        if (IsKeyPressed(Keys.Enter))
            StartOAuth();

        if (_font == null || !InputManager.IsLeftMouseButtonClicked()) return;

        var viewport = _graphics.GraphicsDevice.Viewport;
        var mousePos = InputManager.GetMousePosition();

        // Server URL field (click to focus – already the only editable field)
        float fieldY = viewport.Height / 2f - 30f;
        var fieldBounds = new Rectangle(
            viewport.Width / 2 - 200, (int)fieldY - 3, 400, 28);
        if (fieldBounds.Contains(mousePos))
        {
            InputManager.ConsumeClick();
            return;
        }

        // "Login with Formbar" button
        string btnLabel = "Login with Formbar";
        var btnSize = _font.MeasureString(btnLabel);
        float btnY = viewport.Height / 2f + 60f;
        var btnBounds = new Rectangle(
            (int)(viewport.Width / 2f - btnSize.X / 2f) - 10,
            (int)btnY - 5,
            (int)btnSize.X + 20,
            (int)btnSize.Y + 10);
        if (btnBounds.Contains(mousePos))
        {
            StartOAuth();
            InputManager.ConsumeClick();
        }
    }

    private bool IsCancelButtonClicked()
    {
        if (_font == null || !InputManager.IsLeftMouseButtonClicked()) return false;

        var viewport = _graphics.GraphicsDevice.Viewport;
        var mousePos = InputManager.GetMousePosition();

        string cancelLabel = "Cancel";
        var cancelSize = _font.MeasureString(cancelLabel);
        float cancelY = viewport.Height / 2f + 80f;
        var cancelBounds = new Rectangle(
            (int)(viewport.Width / 2f - cancelSize.X / 2f) - 10,
            (int)cancelY - 5,
            (int)cancelSize.X + 20,
            (int)cancelSize.Y + 10);

        if (cancelBounds.Contains(mousePos))
        {
            InputManager.ConsumeClick();
            return true;
        }
        return false;
    }

    // -----------------------------------------------------------------------
    // Drawing
    // -----------------------------------------------------------------------

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

        if (_state == LoginState.Idle)
            DrawIdle(spriteBatch, viewport);
        else
            DrawWaiting(spriteBatch, viewport);
    }

    private void DrawIdle(SpriteBatch spriteBatch, Viewport viewport)
    {
        // Title
        DrawCentered(spriteBatch, viewport,
            "Sign in with Formbar", viewport.Height / 4f, Color.White);
        DrawCentered(spriteBatch, viewport,
            "Your browser will open so you can log in securely.",
            viewport.Height / 4f + 40f, Color.LightGray);

        // Server URL label + field
        float urlLabelY = viewport.Height / 2f - 65f;
        spriteBatch.DrawString(_font, "Formbar server:",
            new Vector2(viewport.Width / 2f - 200f, urlLabelY), Color.White);

        float fieldY = viewport.Height / 2f - 30f;
        DrawTextField(spriteBatch, viewport, _serverUrl, fieldY);

        // "Login with Formbar" button
        string btnLabel = "Login with Formbar";
        var btnSize = _font.MeasureString(btnLabel);
        float btnY = viewport.Height / 2f + 60f;
        float btnX = viewport.Width / 2f - btnSize.X / 2f;
        spriteBatch.Draw(_pixel,
            new Rectangle((int)btnX - 10, (int)btnY - 5, (int)btnSize.X + 20, (int)btnSize.Y + 10),
            Color.DarkGreen);
        spriteBatch.DrawString(_font, btnLabel, new Vector2(btnX, btnY), Color.White);

        // Status / error message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            DrawCentered(spriteBatch, viewport, _statusMessage,
                viewport.Height / 2f + 120f, Color.OrangeRed);
        }
    }

    private void DrawWaiting(SpriteBatch spriteBatch, Viewport viewport)
    {
        DrawCentered(spriteBatch, viewport,
            "Waiting for browser login...", viewport.Height / 4f, Color.White);
        DrawCentered(spriteBatch, viewport,
            "Check your browser and log in to Formbar.",
            viewport.Height / 4f + 40f, Color.LightGray);

        // Show the OAuth URL in case the browser didn't open automatically
        DrawCentered(spriteBatch, viewport,
            "If your browser didn't open, visit:", viewport.Height / 2f - 60f, Color.LightGray);
        DrawCentered(spriteBatch, viewport,
            _oauthUrl, viewport.Height / 2f - 20f, Color.Cyan);

        // Cancel button
        string cancelLabel = "Cancel";
        var cancelSize = _font.MeasureString(cancelLabel);
        float cancelY = viewport.Height / 2f + 80f;
        float cancelX = viewport.Width / 2f - cancelSize.X / 2f;
        spriteBatch.Draw(_pixel,
            new Rectangle((int)cancelX - 10, (int)cancelY - 5, (int)cancelSize.X + 20, (int)cancelSize.Y + 10),
            Color.DarkRed);
        spriteBatch.DrawString(_font, cancelLabel, new Vector2(cancelX, cancelY), Color.White);
    }

    private void DrawTextField(SpriteBatch spriteBatch, Viewport viewport,
        string value, float fieldY)
    {
        // Yellow border (always focused – only one field)
        spriteBatch.Draw(_pixel,
            new Rectangle(viewport.Width / 2 - 200, (int)fieldY - 3, 400, 28),
            Color.Yellow);
        spriteBatch.Draw(_pixel,
            new Rectangle(viewport.Width / 2 - 198, (int)fieldY - 1, 396, 24),
            Color.Black);
        spriteBatch.DrawString(_font!, value + "|",
            new Vector2(viewport.Width / 2f - 195f, fieldY), Color.White);
    }

    private void DrawCentered(SpriteBatch spriteBatch, Viewport viewport,
        string text, float y, Color color)
    {
        var size = _font!.MeasureString(text);
        spriteBatch.DrawString(_font, text,
            new Vector2(viewport.Width / 2f - size.X / 2f, y), color);
    }

    // -----------------------------------------------------------------------
    // Utilities
    // -----------------------------------------------------------------------

    private static void SendBrowserResponse(HttpListenerContext context, string message)
    {
        try
        {
            string html =
                $"<html><body style='font-family:sans-serif;text-align:center;padding:60px'>" +
                $"<h2>{message}</h2></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }
        catch { }
    }

    /// <summary>Parses a single query parameter from a raw query string (e.g. "?token=abc").</summary>
    private static string? ParseQueryParam(string query, string key)
    {
        string q = query.TrimStart('?');
        foreach (string part in q.Split('&'))
        {
            int eq = part.IndexOf('=');
            if (eq < 0) continue;  // skip parts with no '=' at all
            string k = Uri.UnescapeDataString(part[..eq]);
            string v = Uri.UnescapeDataString(part[(eq + 1)..]);
            if (k == key) return v;
        }
        return null;
    }

    private bool IsKeyPressed(Keys key) =>
        _keyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
}

