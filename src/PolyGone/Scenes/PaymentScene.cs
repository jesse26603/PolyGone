using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

/// <summary>
/// Shown when a player tries to access a level they have not yet unlocked.
/// A one-time payment of <see cref="FormbarSession.LevelCost"/> Digipogs grants
/// access to all levels.  The player supplies their Digipog PIN to authorise
/// the transfer; the server is the authoritative source on whether the payment
/// succeeds.
/// </summary>
internal class PaymentScene : IScene
{
    private SpriteFont? _font;
    private Texture2D? _pixel;
    private readonly ContentManager _content;
    private readonly SceneManager _sceneManager;
    private readonly GraphicsDeviceManager _graphics;
    private readonly string _levelFile; // destination after successful purchase

    // PIN input
    private string _pin = "";

    // Async payment state
    private string _statusMessage = "";
    private bool _isProcessing = false;
    private Task<FormbarService.TransferResult>? _paymentTask;

    private KeyboardState _keyboardState;
    private KeyboardState _previousKeyboardState;

    public PaymentScene(
        ContentManager content,
        SceneManager sceneManager,
        GraphicsDeviceManager graphics,
        string levelFile,
        string levelDisplayName)   // kept for API compatibility; not used in the UI
    {
        _content = content;
        _sceneManager = sceneManager;
        _graphics = graphics;
        _levelFile = levelFile;
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

    public void Update(GameTime gameTime)
    {
        _keyboardState = Keyboard.GetState();

        // ---- Check if the async payment task completed ----
        if (_paymentTask != null && _paymentTask.IsCompleted)
        {
            var result = _paymentTask.Result;
            _paymentTask = null;
            _isProcessing = false;

            if (result.Success)
            {
                // Record once so all levels are unlocked from now on
                PurchaseTracker.RecordPurchase(FormbarSession.UserId, FormbarSession.AllLevelsKey);

                // Go straight to loadout selection for the chosen level
                _sceneManager.PopScene(this);
                _sceneManager.AddScene(new InventoryManagement(
                    _content, _sceneManager, _graphics, _levelFile));
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

        // ---- Escape to cancel ----
        if (InputManager.IsEscapeKeyPressed())
        {
            _sceneManager.PopScene(this);
            _previousKeyboardState = _keyboardState;
            return;
        }

        // ---- Typed characters (numeric PIN only) ----
        string typed = InputManager.ConsumeTypedCharacters();
        foreach (char c in typed)
        {
            if (c == '\b')
            {
                if (_pin.Length > 0)
                    _pin = _pin[..^1];
            }
            else if (char.IsDigit(c) && _pin.Length < FormbarSession.PinMaxLength)
            {
                _pin += c;
            }
        }

        // Enter to pay
        if (IsKeyPressed(Keys.Enter) && _pin.Length > 0)
            StartPayment();

        // ---- Mouse clicks ----
        if (_font != null && InputManager.IsLeftMouseButtonClicked())
        {
            var viewport = _graphics.GraphicsDevice.Viewport;
            var mousePos = InputManager.GetMousePosition();

            // PIN field click area
            float pinFieldY = viewport.Height / 2f - 10f;
            var pinBounds = new Rectangle(
                viewport.Width / 2 - 150, (int)pinFieldY - 3, 300, 28);
            if (pinBounds.Contains(mousePos))
            {
                InputManager.ConsumeClick();
            }

            // Pay button
            float payBtnY = viewport.Height / 2f + 35f;
            string payLabel = $"Pay {FormbarSession.LevelCost} Digipogs";
            var paySize = _font.MeasureString(payLabel);
            var payBounds = new Rectangle(
                (int)(viewport.Width / 2f - paySize.X / 2f) - 8,
                (int)payBtnY - 4,
                (int)paySize.X + 16,
                (int)paySize.Y + 8);
            if (payBounds.Contains(mousePos) && _pin.Length > 0)
            {
                StartPayment();
                InputManager.ConsumeClick();
            }

            // Cancel button
            float cancelBtnY = payBtnY + 40f;
            string cancelLabel = "Cancel";
            var cancelSize = _font.MeasureString(cancelLabel);
            var cancelBounds = new Rectangle(
                (int)(viewport.Width / 2f - cancelSize.X / 2f) - 8,
                (int)cancelBtnY - 4,
                (int)cancelSize.X + 16,
                (int)cancelSize.Y + 8);
            if (cancelBounds.Contains(mousePos))
            {
                _sceneManager.PopScene(this);
                InputManager.ConsumeClick();
            }
        }

        _previousKeyboardState = _keyboardState;
    }

    private void StartPayment()
    {
        if (!int.TryParse(_pin, out int pinInt))
        {
            _statusMessage = "PIN must be a number.";
            return;
        }

        _isProcessing = true;
        _statusMessage = "Processing payment...";
        _paymentTask = FormbarService.TransferDigipogs(
            FormbarSession.ServerUrl,
            FormbarSession.ApiKey,
            FormbarSession.UserId,
            FormbarSession.GameAccountId,
            FormbarSession.LevelCost,
            "PolyGone: unlock all levels",
            pinInt);
    }

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

        // Background
        spriteBatch.Draw(_pixel,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            new Color(20, 10, 40));

        if (_font == null) return;

        // ---- Title ----
        DrawCentered(spriteBatch, viewport, "Unlock All Levels", cy - 110f, Color.Gold);

        // ---- Description ----
        DrawCentered(spriteBatch, viewport,
            $"One-time payment of {FormbarSession.LevelCost} Digipogs unlocks every level.",
            cy - 75f, Color.White);

        // ---- Logged-in name ----
        DrawCentered(spriteBatch, viewport,
            $"Logged in as: {FormbarSession.DisplayName}",
            cy - 45f, Color.LightGray);

        // ---- PIN label ----
        spriteBatch.DrawString(_font, "Digipog PIN:",
            new Vector2(cx - 150f, cy - 30f), Color.White);

        // ---- PIN field ----
        float pinFieldY = cy - 10f;
        spriteBatch.Draw(_pixel,
            new Rectangle((int)cx - 150, (int)pinFieldY - 3, 300, 28),
            Color.Yellow);
        spriteBatch.Draw(_pixel,
            new Rectangle((int)cx - 148, (int)pinFieldY - 1, 296, 24),
            Color.Black);
        string maskedPin = new string('*', _pin.Length) + "|";
        spriteBatch.DrawString(_font, maskedPin,
            new Vector2(cx - 143f, pinFieldY), Color.White);

        // ---- Pay button ----
        float payBtnY = cy + 35f;
        string payLabel = _isProcessing ? "Processing..." : $"Pay {FormbarSession.LevelCost} Digipogs";
        Color payBg = _isProcessing ? Color.Gray : Color.DarkGreen;
        var paySize = _font.MeasureString(payLabel);
        float payX = cx - paySize.X / 2f;
        spriteBatch.Draw(_pixel,
            new Rectangle((int)payX - 8, (int)payBtnY - 4, (int)paySize.X + 16, (int)paySize.Y + 8),
            payBg);
        spriteBatch.DrawString(_font, payLabel, new Vector2(payX, payBtnY), Color.White);

        // ---- Cancel button ----
        float cancelBtnY = payBtnY + 40f;
        string cancelLabel = "Cancel";
        var cancelSize = _font.MeasureString(cancelLabel);
        float cancelX = cx - cancelSize.X / 2f;
        spriteBatch.Draw(_pixel,
            new Rectangle((int)cancelX - 8, (int)cancelBtnY - 4, (int)cancelSize.X + 16, (int)cancelSize.Y + 8),
            Color.DarkRed);
        spriteBatch.DrawString(_font, cancelLabel, new Vector2(cancelX, cancelBtnY), Color.White);

        // ---- Status message ----
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            Color statusColor = _statusMessage.StartsWith("Payment failed", StringComparison.OrdinalIgnoreCase)
                ? Color.OrangeRed
                : Color.LightGray;
            DrawCentered(spriteBatch, viewport, _statusMessage, cancelBtnY + 40f, statusColor);
        }
    }

    private void DrawCentered(SpriteBatch spriteBatch, Viewport viewport,
        string text, float y, Color color)
    {
        var size = _font!.MeasureString(text);
        spriteBatch.DrawString(_font, text,
            new Vector2(viewport.Width / 2f - size.X / 2f, y), color);
    }

    private bool IsKeyPressed(Keys key) =>
        _keyboardState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
}

