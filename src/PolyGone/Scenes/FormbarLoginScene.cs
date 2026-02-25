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
/// Shown when the player needs to log in.  Clicking "Login with Formbar" opens the
/// system browser to the Formbar OAuth page; after login Formbar redirects back to a
/// local HTTP listener and the JWT is decoded locally – no API key required.
///
/// The Formbar server is controlled by <see cref="FormbarSession.DefaultServerUrl"/>.
/// </summary>
internal class FormbarLoginScene : IScene
{
    private enum LoginState { Idle, WaitingForCallback }

    private SpriteFont? _font;
    private Texture2D? _pixel;
    private readonly ContentManager _content;
    private readonly SceneManager _sceneManager;
    private readonly GraphicsDeviceManager _graphics;

    private LoginState _state = LoginState.Idle;
    private string _statusMessage = "";
    private string _oauthUrl = "";

    private HttpListener? _listener;
    private Task<HttpListenerContext>? _callbackTask;

    private KeyboardState _keyboardState;
    private KeyboardState _previousKeyboardState;

    private const int CallbackPort = 59200;

    // Vertical spacing between UI rows (pixels)
    private const float RowGap = 32f;

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
    }

    public void Unload() => StopListener();

    // -----------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------

    public void Update(GameTime gameTime)
    {
        _keyboardState = Keyboard.GetState();

        if (_callbackTask != null && _callbackTask.IsCompleted)
            ProcessCallbackResult();

        if (_state == LoginState.Idle)
            HandleIdleInput();
        else
        {
            if (InputManager.IsEscapeKeyPressed() || IsCancelButtonClicked())
                CancelOAuth();
        }

        _previousKeyboardState = _keyboardState;
    }

    // -----------------------------------------------------------------------
    // OAuth flow
    // -----------------------------------------------------------------------

    private void StartOAuth()
    {
        try
        {
            string prefix = $"http://localhost:{CallbackPort}/";
            string redirectUrl = $"http://localhost:{CallbackPort}/login";
            _oauthUrl = $"{FormbarSession.ServerUrl.TrimEnd('/')}/oauth?redirectURL={Uri.EscapeDataString(redirectUrl)}";

            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _listener.Start();
            _callbackTask = _listener.GetContextAsync();

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
            SendBrowserResponse(context, "Login successful! You can close this tab and return to PolyGone.");
            StopListener();

            string query = context.Request.Url?.Query ?? "";
            string? jwt = ParseQueryParam(query, "token");

            if (jwt != null)
            {
                var userInfo = FormbarService.DecodeJwt(jwt);
                if (userInfo != null)
                {
                    FormbarSession.ApiKey = jwt;
                    FormbarSession.UserId = userInfo.Id;
                    FormbarSession.DisplayName = userInfo.DisplayName;
                    FormbarSession.IsLoggedIn = true;
                    FormbarSession.SaveSession();

                    _sceneManager.PopScene(this);

                    if (!PurchaseTracker.HasPurchased(FormbarSession.UserId, FormbarSession.AllLevelsKey))
                        _sceneManager.AddScene(new PaymentScene(_content, _sceneManager, _graphics));

                    return;
                }
            }

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
        _callbackTask = null;
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
        if (IsKeyPressed(Keys.Enter))
            StartOAuth();

        if (_font == null || !InputManager.IsLeftMouseButtonClicked()) return;

        var viewport = _graphics.GraphicsDevice.Viewport;
        var mousePos = InputManager.GetMousePosition();
        float cy = viewport.Height / 2f;

        // Login button  (same Y as drawn below)
        float btnY = cy + RowGap;
        string btnLabel = "Login with Formbar";
        var btnSize = _font.MeasureString(btnLabel);
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
        float cy = viewport.Height / 2f;

        // Cancel button  (same Y as drawn below)
        float cancelY = cy + RowGap * 2f;
        string cancelLabel = "Cancel";
        var cancelSize = _font.MeasureString(cancelLabel);
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
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(15, 25, 50));

        if (_font == null) return;

        if (_state == LoginState.Idle)
            DrawIdle(spriteBatch, viewport);
        else
            DrawWaiting(spriteBatch, viewport);
    }

    private void DrawIdle(SpriteBatch spriteBatch, Viewport viewport)
    {
        float cx = viewport.Width / 2f;
        float cy = viewport.Height / 2f;

        // Row 0  (cy - 2*gap): title
        DrawCentered(spriteBatch, viewport, "Sign in with Formbar", cy - RowGap * 2f, Color.White);

        // Row 1  (cy - gap): server URL (read-only; edit FormbarSession.DefaultServerUrl in code)
        DrawCentered(spriteBatch, viewport, FormbarSession.ServerUrl, cy - RowGap, new Color(120, 180, 255));

        // Row 2  (cy): "Login with Formbar" button — centred on cy
        string btnLabel = "Login with Formbar";
        var btnSize = _font.MeasureString(btnLabel);
        float btnY = cy;
        float btnX = cx - btnSize.X / 2f;
        spriteBatch.Draw(_pixel!,
            new Rectangle((int)btnX - 10, (int)btnY - 5, (int)btnSize.X + 20, (int)btnSize.Y + 10),
            Color.DarkGreen);
        spriteBatch.DrawString(_font, btnLabel, new Vector2(btnX, btnY), Color.White);

        // Row 3  (cy + gap): status / error
        if (!string.IsNullOrEmpty(_statusMessage))
            DrawCentered(spriteBatch, viewport, _statusMessage, cy + RowGap, Color.OrangeRed);
    }

    private void DrawWaiting(SpriteBatch spriteBatch, Viewport viewport)
    {
        float cx = viewport.Width / 2f;
        float cy = viewport.Height / 2f;

        // Row 0: heading
        DrawCentered(spriteBatch, viewport, "Waiting for browser login...", cy - RowGap * 2f, Color.White);

        // Row 1: instruction
        DrawCentered(spriteBatch, viewport,
            "Complete login in your browser, then return here.",
            cy - RowGap, Color.LightGray);

        // Row 2: OAuth URL (truncated to viewport width minus margins)
        string shortUrl = TruncateToFit(_oauthUrl, viewport.Width - 80f);
        DrawCentered(spriteBatch, viewport, shortUrl, cy, new Color(120, 180, 255));

        // Row 3: Cancel button
        float cancelY = cy + RowGap;
        string cancelLabel = "Cancel";
        var cancelSize = _font!.MeasureString(cancelLabel);
        float cancelX = cx - cancelSize.X / 2f;
        spriteBatch.Draw(_pixel!,
            new Rectangle((int)cancelX - 10, (int)cancelY - 5, (int)cancelSize.X + 20, (int)cancelSize.Y + 10),
            Color.DarkRed);
        spriteBatch.DrawString(_font, cancelLabel, new Vector2(cancelX, cancelY), Color.White);
    }

    // -----------------------------------------------------------------------
    // Utilities
    // -----------------------------------------------------------------------

    private void DrawCentered(SpriteBatch spriteBatch, Viewport viewport, string text, float y, Color color)
    {
        var size = _font!.MeasureString(text);
        spriteBatch.DrawString(_font, text, new Vector2(viewport.Width / 2f - size.X / 2f, y), color);
    }

    private string TruncateToFit(string text, float maxPixels)
    {
        if (_font == null || _font.MeasureString(text).X <= maxPixels) return text;
        while (text.Length > 4 && _font.MeasureString(text + "...").X > maxPixels)
            text = text[..^1];
        return text + "...";
    }

    private static void SendBrowserResponse(HttpListenerContext context, string message)
    {
        try
        {
            string html = $"<html><body style='font-family:sans-serif;text-align:center;padding:60px'><h2>{message}</h2></body></html>";
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = buffer.Length;
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }
        catch { }
    }

    private static string? ParseQueryParam(string query, string key)
    {
        string q = query.TrimStart('?');
        foreach (string part in q.Split('&'))
        {
            int eq = part.IndexOf('=');
            if (eq < 0) continue;
            string k = Uri.UnescapeDataString(part[..eq]);
            string v = Uri.UnescapeDataString(part[(eq + 1)..]);
            if (k == key) return v;
        }
        return null;
    }

    private bool IsKeyPressed(Keys key) =>
        _keyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
}
