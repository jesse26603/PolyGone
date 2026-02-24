using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

/// <summary>
/// Shown when a player tries to access a level they have not yet purchased.
/// Charges <see cref="FormbarSession.LevelCost"/> Digipogs via the Formbar API
/// before granting access.  The player must supply their Digipog PIN (from
/// their /profile page on the Formbar website) to authorise the transfer.
/// </summary>
internal class PaymentScene : IScene
{
    private SpriteFont? _font;
    private Texture2D? _pixel;
    private readonly ContentManager _content;
    private readonly SceneManager _sceneManager;
    private readonly GraphicsDeviceManager _graphics;
    private readonly string _levelFile;
    private readonly string _levelDisplayName;

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
        string levelDisplayName)
    {
        _content = content;
        _sceneManager = sceneManager;
        _graphics = graphics;
        _levelFile = levelFile;
        _levelDisplayName = levelDisplayName;
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
                // Record the purchase so the player is not charged again
                PurchaseTracker.RecordPurchase(FormbarSession.UserId, _levelFile);
                // Reflect the deduction locally (actual source of truth is the Formbar server)
                FormbarSession.Digipogs -= FormbarSession.LevelCost;

                // Move directly to inventory / loadout selection
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
            float pinFieldY = viewport.Height / 2f;
            var pinBounds = new Rectangle(
                viewport.Width / 2 - 200, (int)pinFieldY - 3, 400, 28);
            if (pinBounds.Contains(mousePos))
            {
                InputManager.ConsumeClick();
            }

            // Pay button
            float payBtnY = viewport.Height / 2f + 80f;
            string payLabel = "Pay " + FormbarSession.LevelCost + " Digipogs";
            var paySize = _font.MeasureString(payLabel);
            var payBounds = new Rectangle(
                (int)(viewport.Width / 2f - paySize.X / 2f) - 10,
                (int)payBtnY - 5,
                (int)paySize.X + 20,
                (int)paySize.Y + 10);
            if (payBounds.Contains(mousePos) && _pin.Length > 0)
            {
                StartPayment();
                InputManager.ConsumeClick();
            }

            // Cancel button
            float cancelBtnY = payBtnY + 50f;
            string cancelLabel = "Cancel";
            var cancelSize = _font.MeasureString(cancelLabel);
            var cancelBounds = new Rectangle(
                (int)(viewport.Width / 2f - cancelSize.X / 2f) - 10,
                (int)cancelBtnY - 5,
                (int)cancelSize.X + 20,
                (int)cancelSize.Y + 10);
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

        if (FormbarSession.Digipogs < FormbarSession.LevelCost)
        {
            _statusMessage = $"Insufficient Digipogs. You need {FormbarSession.LevelCost} but have {FormbarSession.Digipogs}.";
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
            $"PolyGone level unlock: {_levelDisplayName}",
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

        // Background
        spriteBatch.Draw(_pixel,
            new Rectangle(0, 0, viewport.Width, viewport.Height),
            new Color(20, 10, 40));

        if (_font == null) return;

        // ---- Title ----
        DrawCentered(spriteBatch, viewport, "Unlock Level", viewport.Height / 4f, Color.Gold);
        DrawCentered(spriteBatch, viewport,
            $"Access to \"{_levelDisplayName}\" costs {FormbarSession.LevelCost} Digipogs.",
            viewport.Height / 4f + 45f, Color.White);

        // ---- Player info ----
        DrawCentered(spriteBatch, viewport,
            $"Logged in as: {FormbarSession.DisplayName}  |  Balance: {FormbarSession.Digipogs} Digipogs",
            viewport.Height / 4f + 85f,
            FormbarSession.Digipogs >= FormbarSession.LevelCost ? Color.LightGreen : Color.OrangeRed);

        // ---- PIN field ----
        float pinLabelY = viewport.Height / 2f - 40f;
        spriteBatch.DrawString(_font, "Digipog PIN (from your /profile page):",
            new Vector2(viewport.Width / 2f - 200f, pinLabelY), Color.White);

        float pinFieldY = viewport.Height / 2f;
        // Border
        spriteBatch.Draw(_pixel,
            new Rectangle(viewport.Width / 2 - 200, (int)pinFieldY - 3, 400, 28),
            Color.Yellow);
        // Background
        spriteBatch.Draw(_pixel,
            new Rectangle(viewport.Width / 2 - 198, (int)pinFieldY - 1, 396, 24),
            Color.Black);
        // Masked PIN
        string maskedPin = new string('*', _pin.Length) + "|";
        spriteBatch.DrawString(_font, maskedPin,
            new Vector2(viewport.Width / 2f - 195f, pinFieldY), Color.White);

        // ---- Pay button ----
        float payBtnY = viewport.Height / 2f + 80f;
        string payLabel = _isProcessing
            ? "Processing..."
            : $"Pay {FormbarSession.LevelCost} Digipogs";
        Color payBg = _isProcessing ? Color.Gray : Color.DarkGreen;
        var paySize = _font.MeasureString(payLabel);
        float payX = viewport.Width / 2f - paySize.X / 2f;
        spriteBatch.Draw(_pixel,
            new Rectangle((int)payX - 10, (int)payBtnY - 5, (int)paySize.X + 20, (int)paySize.Y + 10),
            payBg);
        spriteBatch.DrawString(_font, payLabel, new Vector2(payX, payBtnY), Color.White);

        // ---- Cancel button ----
        float cancelBtnY = payBtnY + 50f;
        string cancelLabel = "Cancel";
        var cancelSize = _font.MeasureString(cancelLabel);
        float cancelX = viewport.Width / 2f - cancelSize.X / 2f;
        spriteBatch.Draw(_pixel,
            new Rectangle((int)cancelX - 10, (int)cancelBtnY - 5, (int)cancelSize.X + 20, (int)cancelSize.Y + 10),
            Color.DarkRed);
        spriteBatch.DrawString(_font, cancelLabel, new Vector2(cancelX, cancelBtnY), Color.White);

        // ---- Status message ----
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            Color statusColor = _statusMessage.StartsWith("Payment failed", StringComparison.OrdinalIgnoreCase)
                ? Color.OrangeRed
                : Color.LightGray;
            DrawCentered(spriteBatch, viewport, _statusMessage,
                cancelBtnY + 60f, statusColor);
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
