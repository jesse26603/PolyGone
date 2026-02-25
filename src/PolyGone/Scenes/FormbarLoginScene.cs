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
/// Shown when the player needs to log in.  The player picks a Formbar server from
/// a preset list, then clicks "Login with Formbar".  A browser window opens to the
/// Formbar OAuth page; after successful login Formbar redirects back to a local
/// HTTP listener.  The JWT is decoded locally – no API key is required.
/// </summary>
internal class FormbarLoginScene : IScene
{
    private enum LoginState { Idle, WaitingForCallback }

    // Known Formbar server presets
    private static readonly string[] ServerPresets =
    {
        "https://formbeta.yorktechapps.com",
        "https://formbar.yorktechapps.com",
        "http://localhost:420",
    };

    private SpriteFont? _font;
    private Texture2D? _pixel;
    private readonly ContentManager _content;
    private readonly SceneManager _sceneManager;
    private readonly GraphicsDeviceManager _graphics;

    private int _presetIndex = 0;
    private LoginState _state = LoginState.Idle;
    private string _statusMessage = "";
    private string _oauthUrl = "";

    private HttpListener? _listener;
    private Task<HttpListenerContext>? _callbackTask;

    private KeyboardState _keyboardState;
    private KeyboardState _previousKeyboardState;

    private const int CallbackPort = 59200;

    public FormbarLoginScene(ContentManager content, SceneManager sceneManager, GraphicsDeviceManager graphics)
    {
        _content = content;
        _sceneManager = sceneManager;
        _graphics = graphics;
        _previousKeyboardState = Keyboard.GetState();

        // Start preset index at the saved server URL if it matches a preset
        for (int i = 0; i < ServerPresets.Length; i++)
        {
            if (ServerPresets[i].Equals(FormbarSession.ServerUrl, StringComparison.OrdinalIgnoreCase))
            {
                _presetIndex = i;
                break;
            }
        }
    }

    private string CurrentServer => ServerPresets[_presetIndex];

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
            _oauthUrl = $"{CurrentServer.TrimEnd('/')}/oauth?redirectURL={Uri.EscapeDataString(redirectUrl)}";

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
                    FormbarSession.ServerUrl = CurrentServer;
                    FormbarSession.UserId = userInfo.Id;
                    FormbarSession.DisplayName = userInfo.DisplayName;
                    FormbarSession.IsLoggedIn = true;
                    FormbarSession.SaveSession();

                    _sceneManager.PopScene(this);

                    // If the player hasn't paid yet, require payment before the menu
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
        // Left/Right arrows cycle through server presets
        if (IsKeyPressed(Keys.Left))
            _presetIndex = (_presetIndex - 1 + ServerPresets.Length) % ServerPresets.Length;
        if (IsKeyPressed(Keys.Right))
            _presetIndex = (_presetIndex + 1) % ServerPresets.Length;
        if (IsKeyPressed(Keys.Enter))
            StartOAuth();

        if (_font == null || !InputManager.IsLeftMouseButtonClicked()) return;

        var viewport = _graphics.GraphicsDevice.Viewport;
        var mousePos = InputManager.GetMousePosition();
        float cy = viewport.Height / 2f;

        // < arrow
        var leftBounds = GetArrowBounds(viewport, cy, left: true);
        if (leftBounds.Contains(mousePos))
        {
            _presetIndex = (_presetIndex - 1 + ServerPresets.Length) % ServerPresets.Length;
            InputManager.ConsumeClick();
            return;
        }

        // > arrow
        var rightBounds = GetArrowBounds(viewport, cy, left: false);
        if (rightBounds.Contains(mousePos))
        {
            _presetIndex = (_presetIndex + 1) % ServerPresets.Length;
            InputManager.ConsumeClick();
            return;
        }

        // Login button
        string btnLabel = "Login with Formbar";
        var btnSize = _font.MeasureString(btnLabel);
        float btnY = cy + 30f;
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

        string cancelLabel = "Cancel";
        var cancelSize = _font.MeasureString(cancelLabel);
        float cancelY = cy + 40f;
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

    // Returns the hit-box for the < or > arrow button
    private Rectangle GetArrowBounds(Viewport viewport, float cy, bool left)
    {
        int arrowW = 28, arrowH = 28;
        float serverBoxHalfW = 200f;
        int x = left
            ? (int)(viewport.Width / 2f - serverBoxHalfW) - arrowW - 6
            : (int)(viewport.Width / 2f + serverBoxHalfW) + 6;
        return new Rectangle(x, (int)cy - arrowH / 2, arrowW, arrowH);
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

        // Title
        DrawCentered(spriteBatch, viewport, "Sign in with Formbar", cy - 80f, Color.White);

        // Server label
        spriteBatch.DrawString(_font, "Formbar server:", new Vector2(cx - 200f, cy - 45f), Color.LightGray);

        // Server selector  < [URL] >
        var leftRect = GetArrowBounds(viewport, cy, left: true);
        var rightRect = GetArrowBounds(viewport, cy, left: false);

        // Box behind URL
        spriteBatch.Draw(_pixel, new Rectangle((int)cx - 200, (int)cy - 14, 400, 28), new Color(30, 30, 60));
        spriteBatch.Draw(_pixel, new Rectangle((int)cx - 200, (int)cy - 14, 400, 1), Color.SlateGray);
        spriteBatch.Draw(_pixel, new Rectangle((int)cx - 200, (int)cy + 14, 400, 1), Color.SlateGray);

        // Left arrow
        spriteBatch.Draw(_pixel, leftRect, new Color(50, 50, 80));
        DrawCentered(spriteBatch,
            new Rectangle(leftRect.X, leftRect.Y, leftRect.Width, leftRect.Height),
            "<", Color.White);

        // Right arrow
        spriteBatch.Draw(_pixel, rightRect, new Color(50, 50, 80));
        DrawCentered(spriteBatch,
            new Rectangle(rightRect.X, rightRect.Y, rightRect.Width, rightRect.Height),
            ">", Color.White);

        // Current URL text (truncate if needed)
        string url = TruncateToFit(CurrentServer, 390f);
        DrawCentered(spriteBatch, viewport, url, cy - 10f, Color.Cyan);

        // Preset counter  e.g. "1 / 3"
        string counter = $"{_presetIndex + 1} / {ServerPresets.Length}";
        DrawCentered(spriteBatch, viewport, counter, cy + 18f, new Color(120, 120, 140));

        // Login button
        string btnLabel = "Login with Formbar";
        var btnSize = _font.MeasureString(btnLabel);
        float btnY = cy + 30f;
        float btnX = cx - btnSize.X / 2f;
        spriteBatch.Draw(_pixel,
            new Rectangle((int)btnX - 12, (int)btnY - 6, (int)btnSize.X + 24, (int)btnSize.Y + 12),
            Color.DarkGreen);
        spriteBatch.DrawString(_font, btnLabel, new Vector2(btnX, btnY), Color.White);

        // Status / error
        if (!string.IsNullOrEmpty(_statusMessage))
            DrawCentered(spriteBatch, viewport, _statusMessage, cy + 80f, Color.OrangeRed);
    }

    private void DrawWaiting(SpriteBatch spriteBatch, Viewport viewport)
    {
        float cy = viewport.Height / 2f;

        DrawCentered(spriteBatch, viewport, "Waiting for browser login...", cy - 80f, Color.White);
        DrawCentered(spriteBatch, viewport, "Complete login in your browser, then return here.", cy - 48f, Color.LightGray);

        DrawCentered(spriteBatch, viewport, "If your browser didn't open, visit:", cy - 10f, Color.LightGray);
        string shortUrl = TruncateToFit(_oauthUrl, (float)viewport.Width - 40f);
        DrawCentered(spriteBatch, viewport, shortUrl, cy + 20f, Color.Cyan);

        // Cancel button
        string cancelLabel = "Cancel";
        var cancelSize = _font!.MeasureString(cancelLabel);
        float cancelY = cy + 40f;
        float cancelX = viewport.Width / 2f - cancelSize.X / 2f;
        spriteBatch.Draw(_pixel!,
            new Rectangle((int)cancelX - 10, (int)cancelY - 5, (int)cancelSize.X + 20, (int)cancelSize.Y + 10),
            Color.DarkRed);
        spriteBatch.DrawString(_font, cancelLabel, new Vector2(cancelX, cancelY), Color.White);
    }

    // -----------------------------------------------------------------------
    // Drawing utilities
    // -----------------------------------------------------------------------

    /// <summary>Draw text centred inside a viewport-width row at the given Y.</summary>
    private void DrawCentered(SpriteBatch spriteBatch, Viewport viewport, string text, float y, Color color)
    {
        var size = _font!.MeasureString(text);
        spriteBatch.DrawString(_font, text, new Vector2(viewport.Width / 2f - size.X / 2f, y), color);
    }

    /// <summary>Draw text centred inside an arbitrary Rectangle.</summary>
    private void DrawCentered(SpriteBatch spriteBatch, Rectangle rect, string text, Color color)
    {
        var size = _font!.MeasureString(text);
        spriteBatch.DrawString(_font, text,
            new Vector2(rect.X + (rect.Width - size.X) / 2f, rect.Y + (rect.Height - size.Y) / 2f),
            color);
    }

    /// <summary>Truncates a string so its rendered width fits within <paramref name="maxPixels"/>.</summary>
    private string TruncateToFit(string text, float maxPixels)
    {
        if (_font == null) return text;
        if (_font.MeasureString(text).X <= maxPixels) return text;
        while (text.Length > 3 && _font.MeasureString(text + "...").X > maxPixels)
            text = text[..^1];
        return text + "...";
    }

    // -----------------------------------------------------------------------
    // HTTP utilities
    // -----------------------------------------------------------------------

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
