using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

/// <summary>
/// Shown when the game needs to be paid for.  A one-time payment of
/// <see cref="FormbarSession.LevelCost"/> Digipogs unlocks all levels.
/// The player enters their Digipog PIN; the Formbar server is the authoritative
/// source on whether the payment succeeds.  After success this scene pops itself
/// (revealing the main menu below).
/// </summary>
internal class PaymentScene : IScene
{
    private SpriteFont? _font;
    private Texture2D? _pixel;
    private readonly ContentManager _content;
    private readonly SceneManager _sceneManager;
    private readonly GraphicsDeviceManager _graphics;

    private string _pin = "";
    private string _statusMessage = "";
    private bool _isProcessing = false;
    private Task<FormbarService.TransferResult>? _paymentTask;

    private KeyboardState _keyboardState;
    private KeyboardState _previousKeyboardState;

    // Maximum chars rendered inside the PIN box (prevents overflow)
    private const int PinDisplayMax = 8;

    public PaymentScene(ContentManager content, SceneManager sceneManager, GraphicsDeviceManager graphics)
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

    // -----------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------

    public void Update(GameTime gameTime)
    {
        _keyboardState = Keyboard.GetState();

        // Poll async payment task
        if (_paymentTask != null && _paymentTask.IsCompleted)
        {
            var result = _paymentTask.Result;
            _paymentTask = null;
            _isProcessing = false;

            if (result.Success)
            {
                PurchaseTracker.RecordPurchase(FormbarSession.UserId, FormbarSession.AllLevelsKey);
                _sceneManager.PopScene(this); // reveal the menu
            }
            else
            {
                _statusMessage = $"Payment failed: {result.Message}";
            }
        }

        if (_isProcessing)
        {
            _previousKeyboardState = _keyboardState;
            return;
        }

        // PIN input
        string typed = InputManager.ConsumeTypedCharacters();
        foreach (char c in typed)
        {
            if (c == '\b') { if (_pin.Length > 0) _pin = _pin[..^1]; }
            else if (char.IsDigit(c) && _pin.Length < FormbarSession.PinMaxLength) _pin += c;
        }

        if (IsKeyPressed(Keys.Enter) && _pin.Length > 0)
            StartPayment();

        if (_font == null || !InputManager.IsLeftMouseButtonClicked())
        {
            _previousKeyboardState = _keyboardState;
            return;
        }

        var viewport = _graphics.GraphicsDevice.Viewport;
        var mousePos = InputManager.GetMousePosition();
        float cx = viewport.Width / 2f;
        float cy = viewport.Height / 2f;

        // Pay button  (same Y as drawn: cy + RowGap)
        float payBtnY = cy + RowGap;
        string payLabel = $"Pay {FormbarSession.LevelCost} Digipogs";
        var paySize = _font.MeasureString(payLabel);
        var payBounds = Btn((int)(cx - paySize.X / 2f), (int)payBtnY, paySize);
        if (payBounds.Contains(mousePos) && _pin.Length > 0)
        {
            StartPayment();
            InputManager.ConsumeClick();
        }

        // Log Out button  (same Y as drawn: cy + 2*RowGap)
        float logoutBtnY = cy + RowGap * 2f;
        string logoutLabel = "Log Out";
        var logoutSize = _font.MeasureString(logoutLabel);
        var logoutBounds = Btn((int)(cx - logoutSize.X / 2f), (int)logoutBtnY, logoutSize);
        if (logoutBounds.Contains(mousePos))
        {
            DoLogout();
            InputManager.ConsumeClick();
        }

        _previousKeyboardState = _keyboardState;
    }

    private void StartPayment()
    {
        if (!int.TryParse(_pin, out int pinInt)) { _statusMessage = "PIN must be a number."; return; }
        _isProcessing = true;
        _statusMessage = "Processing payment...";
        _paymentTask = FormbarService.TransferDigipogs(
            FormbarSession.ServerUrl, FormbarSession.ApiKey,
            FormbarSession.UserId, FormbarSession.GameAccountId,
            FormbarSession.LevelCost, "PolyGone: unlock all levels", pinInt);
    }

    private void DoLogout()
    {
        FormbarSession.Clear();
        _sceneManager.PopScene(this);
        _sceneManager.AddScene(new FormbarLoginScene(_content, _sceneManager, _graphics));
    }

    // -----------------------------------------------------------------------
    // Draw
    // -----------------------------------------------------------------------

    // Vertical spacing between UI rows (pixels)
    private const float RowGap = 70f;

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixel == null)
        {
            _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        var viewport = spriteBatch.GraphicsDevice.Viewport;
        float cx = viewport.Width / 2f;
        float cy = viewport.Height / 2f;

        spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(20, 10, 40));

        if (_font == null) return;

        // Row 0  (cy - 4*gap): title
        DrawCentered(spriteBatch, viewport, "Unlock PolyGone", cy - RowGap * 4f, Color.Gold);

        // Row 1  (cy - 3*gap): cost description
        DrawCentered(spriteBatch, viewport,
            $"Pay {FormbarSession.LevelCost} Digipogs to unlock all levels.",
            cy - RowGap * 3f, Color.White);

        // Row 2  (cy - 2*gap): logged-in name
        DrawCentered(spriteBatch, viewport,
            $"Logged in as: {FormbarSession.DisplayName}",
            cy - RowGap * 2f, new Color(160, 160, 180));

        // Row 3  (cy - gap): PIN label
        spriteBatch.DrawString(_font, "Digipog PIN:",
            new Vector2(cx - 120f, cy - RowGap), Color.LightGray);

        // Row 4  (cy): PIN field  (240 px wide; at most 8 asterisks + cursor)
        spriteBatch.Draw(_pixel, new Rectangle((int)cx - 120, (int)cy - 3, 240, 26), Color.DimGray);
        spriteBatch.Draw(_pixel, new Rectangle((int)cx - 119, (int)cy - 2, 238, 24), Color.Black);
        string displayed = new string('*', Math.Min(_pin.Length, PinDisplayMax)) + "|";
        spriteBatch.DrawString(_font, displayed, new Vector2(cx - 113f, cy), Color.White);

        // Row 5  (cy + gap): Pay button
        float payBtnY = cy + RowGap;
        string payLabel = _isProcessing ? "Processing..." : $"Pay {FormbarSession.LevelCost} Digipogs";
        Color payBg = _isProcessing ? Color.Gray : Color.DarkGreen;
        var paySize = _font.MeasureString(payLabel);
        float payX = cx - paySize.X / 2f;
        spriteBatch.Draw(_pixel, Btn((int)payX, (int)payBtnY, paySize), payBg);
        spriteBatch.DrawString(_font, payLabel, new Vector2(payX, payBtnY), Color.White);

        // Row 6  (cy + 2*gap): Log Out button
        float logoutBtnY = cy + RowGap * 2f;
        string logoutLabel = "Log Out";
        var logoutSize = _font.MeasureString(logoutLabel);
        float logoutX = cx - logoutSize.X / 2f;
        spriteBatch.Draw(_pixel, Btn((int)logoutX, (int)logoutBtnY, logoutSize), new Color(80, 30, 30));
        spriteBatch.DrawString(_font, logoutLabel, new Vector2(logoutX, logoutBtnY), Color.White);

        // Row 7  (cy + 3*gap): status message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            Color statusColor = _statusMessage.StartsWith("Payment failed", StringComparison.OrdinalIgnoreCase)
                ? Color.OrangeRed : Color.LightGray;
            DrawCentered(spriteBatch, viewport, _statusMessage, cy + RowGap * 3f, statusColor);
        }
    }

    private void DrawCentered(SpriteBatch spriteBatch, Viewport viewport,
        string text, float y, Color color)
    {
        var size = _font!.MeasureString(text);
        spriteBatch.DrawString(_font, text,
            new Vector2(viewport.Width / 2f - size.X / 2f, y), color);
    }

    /// <summary>Returns a padded button Rectangle for a text element at (x, y).</summary>
    private static Rectangle Btn(int x, int y, Vector2 textSize) =>
        new Rectangle(x - 10, y - 5, (int)textSize.X + 20, (int)textSize.Y + 10);

    private bool IsKeyPressed(Keys key) =>
        _keyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
}
