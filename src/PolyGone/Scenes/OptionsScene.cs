using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using PolyGone.Core;

namespace PolyGone;

internal class OptionsScene : IScene
{
    private Texture2D? _pixel;
    private SpriteFont? _font;
    private readonly ContentManager _content;
    private readonly SceneManager _sceneManager;
    private readonly AudioManager _audioManager;
    private readonly GraphicsDeviceManager _graphics;
    private int _selectedIndex;
    private readonly (int Width, int Height)[] _availableResolutions;
    private int _pendingResolutionIndex;
    private int _appliedResolutionIndex;
    private int _deferredCenterWidth;
    private int _deferredCenterHeight;
    private bool _pendingIsFullScreen;
    private bool _appliedIsFullScreen;
    private bool _confirmingDiscard;
    private int _confirmSelectedIndex;
    private int _buttonIndex; // 0 = Apply, 1 = Discard
    private int _resetConfirmStep;           // 0 = off, 1 = first prompt, 2 = second prompt
    private int _resetConfirmSelectedIndex;
    private int _resetProgressConfirmStep;   // 0 = off, 1 = first prompt, 2 = second prompt
    private int _resetProgressConfirmSelectedIndex;
    private int _volume;
    private float _volumeKeyRepeatTimer;
    private float _mouseInputBlockTimer;

    private const float RowSpacing = 50f;
    private const float ButtonWidth = 200f;
    private const float ButtonHeight = 45f;
    private const float ButtonGap = 24f;
    private const float VolumeSliderWidth = 240f;
    private const float VolumeSliderHeight = 8f;
    private const float VolumeSliderKnobRadius = 10f;
    private const float VolumeKeyInitialDelay = 0.28f;
    private const float VolumeKeyRepeatDelay = 0.06f;
    private const float MouseInputBlockDuration = 0.2f;

    // Option labels are dynamic — they reflect current pending state
    private bool HasPendingChanges =>
        _pendingIsFullScreen != _appliedIsFullScreen ||
        _pendingResolutionIndex != _appliedResolutionIndex;

    private string ResolutionLabel()
    {
        if (_pendingIsFullScreen)
        { return "Resolution: (fullscreen)"; }
        var r = _availableResolutions[_pendingResolutionIndex];
        return $"Resolution: < {r.Width}x{r.Height} >";
    }

    // Row 0=Display, Row 1=Resolution, Row 2=Buttons (drawn separately),
    // Row 3=Volume, then reset/dev/back rows.
    private string[] GetRowLabels() =>
    [
        _pendingIsFullScreen ? "Display: Fullscreen" : "Display: Windowed",
        ResolutionLabel(),
        "",   // placeholder — button row is drawn separately
        "",   // volume row is drawn as a slider
        "Controls",
#if DEBUG
        "Reset Purchases",
#endif
        "Reset Progress",
#if DEBUG
        "Dev Menu",
#endif
        "Back",
    ];

    public OptionsScene(ContentManager content, SceneManager sceneManager, AudioManager audioManager, GraphicsDeviceManager graphics)
    {
        _content = content;
        _sceneManager = sceneManager;
        _audioManager = audioManager;
        _graphics = graphics;
        _selectedIndex = 0;
        _availableResolutions = DisplaySettings.GetAvailableResolutions();
        var currentRes = (DisplaySettings.WindowedWidth, DisplaySettings.WindowedHeight);
        _pendingResolutionIndex = Array.IndexOf(_availableResolutions, currentRes);
        if (_pendingResolutionIndex < 0)
        { _pendingResolutionIndex = 0; }
        _appliedResolutionIndex = _pendingResolutionIndex;
        _pendingIsFullScreen = DisplaySettings.IsFullScreen;
        _appliedIsFullScreen = DisplaySettings.IsFullScreen;
        _confirmingDiscard = false;
        _confirmSelectedIndex = 1;
        _buttonIndex = 0;
        _resetConfirmStep = 0;
        _resetConfirmSelectedIndex = 1;
        _resetProgressConfirmStep = 0;
        _resetProgressConfirmSelectedIndex = 1;
        _volume = (int)MathF.Round(_audioManager.MasterVolume * 100f);
        _volumeKeyRepeatTimer = 0f;
        _mouseInputBlockTimer = MouseInputBlockDuration;
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
        if (_volumeKeyRepeatTimer > 0f)
        {
            _volumeKeyRepeatTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        if (_mouseInputBlockTimer > 0f)
        {
            _mouseInputBlockTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        // Apply deferred window centering (must happen on the frame after ApplyChanges)
        if (_deferredCenterWidth > 0)
        {
            DisplaySettings.CenterWindowOnPrimaryDisplay(_deferredCenterWidth, _deferredCenterHeight);
            _deferredCenterWidth = 0;
            _deferredCenterHeight = 0;
        }

        // Handle the confirm-discard overlay independently
        if (_confirmingDiscard)
        {
            if (_font != null)
            {
                string[] confirmOpts = ["Discard & Go Back", "Keep Editing"];
                var viewport = _graphics.GraphicsDevice.Viewport;
                var confirmStartY = viewport.Height / 2f - 20f;
                for (var i = 0; i < confirmOpts.Length; i++)
                {
                    var textSize = _font.MeasureString(confirmOpts[i]);
                    var pos = new Vector2(viewport.Width / 2f - textSize.X / 2f, confirmStartY + i * 50f);
                    var bounds = new Rectangle((int)pos.X, (int)pos.Y, (int)textSize.X, (int)textSize.Y);
                    if (CanProcessMouseInput() && bounds.Contains(InputManager.GetMousePosition()))
                    {
                        _confirmSelectedIndex = i;
                        if (InputManager.MenuConfirm())
                        {
                            ExecuteConfirm();
                            InputManager.ConsumeClick();
                        }
                    }
                }
            }
            if (InputManager.MenuUp() || InputManager.MenuLeft())
            {
                _confirmSelectedIndex = (_confirmSelectedIndex - 1 + 2) % 2;
            }
            if (InputManager.MenuDown() || InputManager.MenuRight())
            {
                _confirmSelectedIndex = (_confirmSelectedIndex + 1) % 2;
            }
            if (InputManager.MenuConfirm())
            {
                ExecuteConfirm();
            }
            if (InputManager.MenuBack())
            {
                _confirmingDiscard = false;
            }
            return;
        }

        if (_resetConfirmStep > 0)
        {
            HandleResetConfirmInput();
            return;
        }

        if (_resetProgressConfirmStep > 0)
        {
            HandleResetProgressConfirmInput();
            return;
        }

        _selectedIndex = Math.Clamp(_selectedIndex, 0, GetRowLabels().Length - 1);

        if (_font != null)
        {
            var labels = GetRowLabels();
            var viewport = _graphics.GraphicsDevice.Viewport;
            var startY = viewport.Height / 2f - labels.Length * RowSpacing / 2f;

            // Text rows — skip row 2 (button row)
            for (var i = 0; i < labels.Length; i++)
            {
                if (i == 2)
                { continue; }
                var textSize = _font.MeasureString(labels[i]);
                var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, startY + i * RowSpacing);
                var bounds = new Rectangle((int)position.X, (int)position.Y, (int)textSize.X, (int)textSize.Y);
                if (i == 3)
                {
                    var sliderRect = GetVolumeSliderRect(viewport, startY);
                    var volumeRowBounds = new Rectangle(
                        sliderRect.X - 120,
                        sliderRect.Y - 18,
                        sliderRect.Width + 240,
                        sliderRect.Height + 36);

                    if (CanProcessMouseInput() && volumeRowBounds.Contains(InputManager.GetMousePosition()))
                    {
                        _selectedIndex = i;
                        if (InputManager.IsLeftMouseButtonClicked() || InputManager.IsLeftMouseButtonHeld())
                        {
                            SetVolumeFromMouseX(InputManager.GetMousePosition().X, sliderRect);
                            if (InputManager.IsLeftMouseButtonClicked())
                            {
                                InputManager.ConsumeClick();
                            }
                        }
                    }
                }
                else if (CanProcessMouseInput() && bounds.Contains(InputManager.GetMousePosition()))
                {
                    _selectedIndex = i;
                    if (InputManager.MenuConfirm())
                    {
                        if (i == 1 && !_pendingIsFullScreen)
                        {
                            var midX = position.X + textSize.X / 2f;
                            CycleResolution(InputManager.GetMousePosition().X < midX ? -1 : 1);
                        }
                        else
                        {
                            ExecuteSelection();
                        }
                        InputManager.ConsumeClick();
                    }
                }
            }

            // Button row hit detection
            var buttonRowY = startY + 2 * RowSpacing;
            var applyRect = new Rectangle((int)(viewport.Width / 2f - ButtonGap / 2f - ButtonWidth), (int)buttonRowY, (int)ButtonWidth, (int)ButtonHeight);
            var discardRect = new Rectangle((int)(viewport.Width / 2f + ButtonGap / 2f), (int)buttonRowY, (int)ButtonWidth, (int)ButtonHeight);

            if (CanProcessMouseInput() && applyRect.Contains(InputManager.GetMousePosition()))
            {
                _selectedIndex = 2; _buttonIndex = 0;
                if (InputManager.MenuConfirm()) { ExecuteSelection(); InputManager.ConsumeClick(); }
            }
            else if (CanProcessMouseInput() && discardRect.Contains(InputManager.GetMousePosition()))
            {
                _selectedIndex = 2; _buttonIndex = 1;
                if (InputManager.MenuConfirm()) { ExecuteSelection(); InputManager.ConsumeClick(); }
            }
        }

        // Keyboard navigation
        int rowCount = GetRowLabels().Length;
        if (InputManager.MenuUp())   { _selectedIndex = (_selectedIndex - 1 + rowCount) % rowCount; _mouseInputBlockTimer = MouseInputBlockDuration; }
        if (InputManager.MenuDown()) { _selectedIndex = (_selectedIndex + 1) % rowCount; _mouseInputBlockTimer = MouseInputBlockDuration; }

        if (_selectedIndex == 1 && !_pendingIsFullScreen)
        {
            if (InputManager.MenuLeft())  { CycleResolution(-1); _mouseInputBlockTimer = MouseInputBlockDuration; }
            if (InputManager.MenuRight()) { CycleResolution(1); _mouseInputBlockTimer = MouseInputBlockDuration; }
        }

        if (_selectedIndex == 3)
        {
            if (InputManager.MenuLeft() && _volumeKeyRepeatTimer <= 0f)
            {
                AdjustVolume(-5);
                _mouseInputBlockTimer = MouseInputBlockDuration;
            }
            if (InputManager.MenuRight() && _volumeKeyRepeatTimer <= 0f)
            {
                AdjustVolume(5);
                _mouseInputBlockTimer = MouseInputBlockDuration;
            }
        }

        if (_selectedIndex == 2)
        {
            if (InputManager.MenuLeft())  { _buttonIndex = 0; _mouseInputBlockTimer = MouseInputBlockDuration; }
            if (InputManager.MenuRight()) { _buttonIndex = 1; _mouseInputBlockTimer = MouseInputBlockDuration; }
        }

        if (InputManager.MenuConfirm()) { ExecuteSelection(); _mouseInputBlockTimer = MouseInputBlockDuration; }
        if (InputManager.MenuBack())
        {
            _mouseInputBlockTimer = MouseInputBlockDuration;
            if (HasPendingChanges)
            { _confirmingDiscard = true; _confirmSelectedIndex = 1; }
            else
            { _sceneManager.PopScene(this); }
        }
    }

    // Only updates the pending index — does not touch graphics until Apply is pressed
    private void CycleResolution(int dir)
    {
        var count = _availableResolutions.Length;
        _pendingResolutionIndex = (_pendingResolutionIndex + dir + count) % count;
    }

    private void ApplyChanges()
    {
        if (_pendingIsFullScreen)
        {
            var dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = dm.Width;
            _graphics.PreferredBackBufferHeight = dm.Height;
            _graphics.IsFullScreen = true;
            DisplaySettings.IsFullScreen = true;
        }
        else
        {
            var r = _availableResolutions[_pendingResolutionIndex];
            DisplaySettings.ResolutionIndex = Array.IndexOf(DisplaySettings.Resolutions, r);
            _graphics.IsFullScreen = false;
            DisplaySettings.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = r.Width;
            _graphics.PreferredBackBufferHeight = r.Height;
            _deferredCenterWidth = r.Width;
            _deferredCenterHeight = r.Height;
        }
        _graphics.ApplyChanges();
        DisplaySettings.Save();
        _appliedIsFullScreen = _pendingIsFullScreen;
        _appliedResolutionIndex = _pendingResolutionIndex;
    }

    private void DiscardChanges()
    {
        _pendingIsFullScreen = _appliedIsFullScreen;
        _pendingResolutionIndex = _appliedResolutionIndex;
    }

    private void ExecuteConfirm()
    {
        if (_confirmSelectedIndex == 0)
        {
            _sceneManager.PopScene(this); // Discard & Go Back
        }
        else
        {
            _confirmingDiscard = false;   // Keep Editing
        }
    }

    private void ExecuteSelection()
    {
        switch (_selectedIndex)
        {
            case 0:
                _pendingIsFullScreen = !_pendingIsFullScreen;
                break;
            case 1:
                if (!_pendingIsFullScreen)
                { CycleResolution(1); }
                break;
            case 2:
                if (_buttonIndex == 0 && HasPendingChanges)
                { ApplyChanges(); }
                else if (_buttonIndex == 1 && HasPendingChanges)
                { DiscardChanges(); }
                break;
            case 3:
                // Volume is adjusted with left/right input and mouse drag.
                break;
            case 4:
                _sceneManager.AddScene(new ControlsScene(_content, _sceneManager, _graphics));
                break;
#if DEBUG
            case 5:
                _resetConfirmStep = 1;
                _resetConfirmSelectedIndex = 1; // default cursor on Cancel
                break;
            case 6:
                _resetProgressConfirmStep = 1;
                _resetProgressConfirmSelectedIndex = 1; // default cursor on Cancel
                break;
            case 7:
                _sceneManager.AddScene(new DevMenuScene(_content, _sceneManager, _graphics));
                break;
#else
            case 5:
                _resetProgressConfirmStep = 1;
                _resetProgressConfirmSelectedIndex = 1; // default cursor on Cancel
                break;
#endif
            default: // Back — always the last row
                if (HasPendingChanges)
                { _confirmingDiscard = true; _confirmSelectedIndex = 1; }
                else
                { _sceneManager.PopScene(this); }
                break;
        }
    }

    // ── Reset-purchases double-confirm ──────────────────────────────────────

    private void HandleResetConfirmInput()
    {
        if (_font != null)
        {
            var viewport = _graphics.GraphicsDevice.Viewport;
            string[] opts = _resetConfirmStep == 1
                ? ["Yes, continue", "Cancel"]
                : ["Confirm Reset", "Cancel"];
            var confirmStartY = viewport.Height / 2f - 20f;
            for (var i = 0; i < opts.Length; i++)
            {
                var textSize = _font.MeasureString(opts[i]);
                var pos = new Vector2(viewport.Width / 2f - textSize.X / 2f, confirmStartY + i * 50f);
                var bounds = new Rectangle((int)pos.X, (int)pos.Y, (int)textSize.X, (int)textSize.Y);
                if (bounds.Contains(InputManager.GetMousePosition()))
                {
                    _resetConfirmSelectedIndex = i;
                    if (InputManager.MenuConfirm())
                    {
                        ExecuteResetConfirm();
                        InputManager.ConsumeClick();
                    }
                }
            }
        }

        if (InputManager.MenuUp()   || InputManager.MenuLeft())  { _resetConfirmSelectedIndex = (_resetConfirmSelectedIndex - 1 + 2) % 2; }
        if (InputManager.MenuDown() || InputManager.MenuRight()) { _resetConfirmSelectedIndex = (_resetConfirmSelectedIndex + 1) % 2; }
        if (InputManager.MenuConfirm())  { ExecuteResetConfirm(); }
        if (InputManager.MenuBack()) { _resetConfirmStep = 0; _resetConfirmSelectedIndex = 1; }
    }

    private void ExecuteResetConfirm()
    {
        if (_resetConfirmSelectedIndex == 1) // Cancel
        {
            _resetConfirmStep = 0;
            _resetConfirmSelectedIndex = 1;
            return;
        }
        // Selected index 0 = Yes / Confirm
        if (_resetConfirmStep == 1)
        {
            _resetConfirmStep = 2;   // advance to second prompt
            _resetConfirmSelectedIndex = 1;   // keep cursor on Cancel for safety
        }
        else
        {
            PurchaseTracker.Reset();
            _resetConfirmStep = 0;
            _resetConfirmSelectedIndex = 1;
            _sceneManager.PopScene(this);
            _sceneManager.AddScene(new PaymentScene(_content, _sceneManager, _audioManager, _graphics));
        }
    }

    private bool CanProcessMouseInput()
    {
        return _mouseInputBlockTimer <= 0f && !InputManager.UsingController;
    }

    private Rectangle GetVolumeSliderRect(Viewport viewport, float startY)
    {
        float rowY = startY + 3 * RowSpacing;
        return new Rectangle(
            (int)(viewport.Width / 2f - VolumeSliderWidth / 2f),
            (int)(rowY + 14f),
            (int)VolumeSliderWidth,
            (int)VolumeSliderHeight);
    }

    private void SetVolumeFromMouseX(int mouseX, Rectangle sliderRect)
    {
        float t = Math.Clamp((mouseX - sliderRect.X) / (float)sliderRect.Width, 0f, 1f);
        _volume = (int)MathF.Round(t * 100f);
        _audioManager.SetMasterVolume(_volume / 100f);
    }

    private void AdjustVolume(int amount)
    {
        _volume = Math.Clamp(_volume + amount, 0, 100);
        _audioManager.SetMasterVolume(_volume / 100f);
        _volumeKeyRepeatTimer = _volumeKeyRepeatTimer <= 0f ? VolumeKeyInitialDelay : VolumeKeyRepeatDelay;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_pixel == null)
        {
            _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        spriteBatch.Draw(_pixel, new Rectangle(0, 0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height), Color.Gray);

        if (_font != null)
        {
            var labels = GetRowLabels();
            var viewport = spriteBatch.GraphicsDevice.Viewport;
            var startY = viewport.Height / 2f - labels.Length * RowSpacing / 2f;

            // Title
            var title = "Options";
            var titleSize = _font.MeasureString(title);
            spriteBatch.DrawString(_font, title,
                new Vector2(viewport.Width / 2f - titleSize.X / 2f, startY - 60f),
                (_confirmingDiscard || _resetConfirmStep > 0 || _resetProgressConfirmStep > 0) ? Color.DimGray : Color.LightGray);

            // Text rows — skip row 2 (button row)
            for (var i = 0; i < labels.Length; i++)
            {
                if (i == 2)
                { continue; }
                Color color;
                if (_confirmingDiscard || _resetConfirmStep > 0 || _resetProgressConfirmStep > 0)
                { color = Color.DimGray; }
                else if (i == 1 && _pendingIsFullScreen)
                { color = Color.DarkGray; }
#if DEBUG
                else if (i == 5 || i == 6)
                { color = i == _selectedIndex ? Color.OrangeRed : new Color(180, 80, 60); }
                else if (i == 7)
                { color = i == _selectedIndex ? Color.Cyan : Color.DarkCyan; }
#else
                else if (i == 5)                                     { color = i == _selectedIndex ? Color.OrangeRed : new Color(180, 80, 60); }
#endif
                else
                { color = i == _selectedIndex ? Color.Yellow : Color.White; }
                if (i == 3)
                {
                    var sliderRect = GetVolumeSliderRect(viewport, startY);
                    var sliderY = sliderRect.Y + sliderRect.Height / 2f;
                    var fillWidth = sliderRect.Width * (_volume / 100f);

                    var label = "Volume";
                    var labelSize = _font.MeasureString(label);
                    var labelPosition = new Vector2(sliderRect.X - labelSize.X - 16f, sliderY - labelSize.Y / 2f);
                    spriteBatch.DrawString(_font, label, labelPosition, color);

                    spriteBatch.Draw(_pixel, sliderRect, new Color(65, 65, 65));
                    spriteBatch.Draw(_pixel, new Rectangle(sliderRect.X, sliderRect.Y, (int)fillWidth, sliderRect.Height), new Color(90, 180, 90));

                    var knobX = sliderRect.X + fillWidth;
                    var knobRect = new Rectangle(
                        (int)(knobX - VolumeSliderKnobRadius),
                        (int)(sliderY - VolumeSliderKnobRadius),
                        (int)(VolumeSliderKnobRadius * 2f),
                        (int)(VolumeSliderKnobRadius * 2f));

                    spriteBatch.Draw(_pixel, knobRect, i == _selectedIndex ? Color.Yellow : Color.White);

                    var volumeText = $"{_volume}%";
                    var volumeTextSize = _font.MeasureString(volumeText);
                    spriteBatch.DrawString(_font, volumeText,
                        new Vector2(sliderRect.Right + 14f, sliderY - volumeTextSize.Y / 2f),
                        color);
                }
                else
                {
                    var textSize = _font.MeasureString(labels[i]);
                    var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, startY + i * RowSpacing);
                    spriteBatch.DrawString(_font, labels[i], position, color);
                }
            }

            // Button row
            var buttonRowY = startY + 2 * RowSpacing;
            var applyRect = new Rectangle((int)(viewport.Width / 2f - ButtonGap / 2f - ButtonWidth), (int)buttonRowY, (int)ButtonWidth, (int)ButtonHeight);
            var discardRect = new Rectangle((int)(viewport.Width / 2f + ButtonGap / 2f), (int)buttonRowY, (int)ButtonWidth, (int)ButtonHeight);

            void DrawButton(Rectangle rect, string text, bool isSelected, bool enabled, bool isDanger)
            {
                Color fill, border, textColor;
                if (_confirmingDiscard || _resetConfirmStep > 0 || _resetProgressConfirmStep > 0 || !enabled)
                {
                    fill = new Color(55, 55, 55);
                    border = new Color(85, 85, 85);
                    textColor = new Color(110, 110, 110);
                }
                else if (isDanger)
                {
                    fill = isSelected ? new Color(130, 40, 40) : new Color(85, 25, 25);
                    border = isSelected ? Color.Tomato : new Color(160, 60, 60);
                    textColor = isSelected ? Color.Yellow : Color.Tomato;
                }
                else
                {
                    fill = isSelected ? new Color(40, 110, 40) : new Color(25, 70, 25);
                    border = isSelected ? Color.LightGreen : new Color(55, 130, 55);
                    textColor = isSelected ? Color.Yellow : Color.White;
                }
                // Border rect (2px on each side)
                spriteBatch.Draw(_pixel, new Rectangle(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4), border);
                spriteBatch.Draw(_pixel, rect, fill);
                var ts = _font.MeasureString(text);
                spriteBatch.DrawString(_font, text,
                    new Vector2(rect.X + (rect.Width - ts.X) / 2f, rect.Y + (rect.Height - ts.Y) / 2f),
                    textColor);
            }

            DrawButton(applyRect, "Apply", _selectedIndex == 2 && _buttonIndex == 0, HasPendingChanges, false);
            DrawButton(discardRect, "Discard", _selectedIndex == 2 && _buttonIndex == 1, HasPendingChanges, true);

            // Confirm-discard overlay
            if (_confirmingDiscard)
            {
                spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.6f);

                var warning = "Unsaved changes will be lost.";
                var warningSize = _font.MeasureString(warning);
                spriteBatch.DrawString(_font, warning,
                    new Vector2(viewport.Width / 2f - warningSize.X / 2f, viewport.Height / 2f - 80f),
                    Color.Red);

                string[] confirmOpts = ["Discard & Go Back", "Keep Editing"];
                var confirmStartY = viewport.Height / 2f - 20f;
                for (var i = 0; i < confirmOpts.Length; i++)
                {
                    var color = i == _confirmSelectedIndex ? Color.Yellow : Color.White;
                    var textSize = _font.MeasureString(confirmOpts[i]);
                    var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, confirmStartY + i * 50f);
                    spriteBatch.DrawString(_font, confirmOpts[i], position, color);
                }
            }

            // Reset-purchases confirm overlay
            if (_resetConfirmStep > 0)
            {
                spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.6f);

                var overlayTitle = "Reset Purchases";
                var overlayTitleSize = _font.MeasureString(overlayTitle);
                spriteBatch.DrawString(_font, overlayTitle,
                    new Vector2(viewport.Width / 2f - overlayTitleSize.X / 2f, viewport.Height / 2f - 120f),
                    Color.OrangeRed);

                string resetWarning = _resetConfirmStep == 1
                    ? "All purchase records will be permanently deleted."
                    : "This cannot be undone!";
                var resetWarningSize = _font.MeasureString(resetWarning);
                spriteBatch.DrawString(_font, resetWarning,
                    new Vector2(viewport.Width / 2f - resetWarningSize.X / 2f, viewport.Height / 2f - 70f),
                    Color.Red);

                string[] resetOpts = _resetConfirmStep == 1
                    ? ["Yes, continue", "Cancel"]
                    : ["Confirm Reset", "Cancel"];
                var resetStartY = viewport.Height / 2f - 10f;
                for (var i = 0; i < resetOpts.Length; i++)
                {
                    var color = i == _resetConfirmSelectedIndex ? Color.Yellow : Color.White;
                    var textSize = _font.MeasureString(resetOpts[i]);
                    var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, resetStartY + i * 50f);
                    spriteBatch.DrawString(_font, resetOpts[i], position, color);
                }
            }

            // Reset-progress confirm overlay
            if (_resetProgressConfirmStep > 0)
            {
                spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), Color.Black * 0.6f);

                var overlayTitle = "Reset Progress";
                var overlayTitleSize = _font.MeasureString(overlayTitle);
                spriteBatch.DrawString(_font, overlayTitle,
                    new Vector2(viewport.Width / 2f - overlayTitleSize.X / 2f, viewport.Height / 2f - 120f),
                    Color.OrangeRed);

                string resetWarning = _resetProgressConfirmStep == 1
                    ? "All level progress and item unlocks will be permanently deleted."
                    : "This cannot be undone!";
                var resetWarningSize = _font.MeasureString(resetWarning);
                spriteBatch.DrawString(_font, resetWarning,
                    new Vector2(viewport.Width / 2f - resetWarningSize.X / 2f, viewport.Height / 2f - 70f),
                    Color.Red);

                string[] resetOpts = _resetProgressConfirmStep == 1
                    ? ["Yes, continue", "Cancel"]
                    : ["Confirm Reset", "Cancel"];
                var resetStartY = viewport.Height / 2f - 10f;
                for (var i = 0; i < resetOpts.Length; i++)
                {
                    var color = i == _resetProgressConfirmSelectedIndex ? Color.Yellow : Color.White;
                    var textSize = _font.MeasureString(resetOpts[i]);
                    var position = new Vector2(viewport.Width / 2f - textSize.X / 2f, resetStartY + i * 50f);
                    spriteBatch.DrawString(_font, resetOpts[i], position, color);
                }
            }
        }
    }

    // ── Reset-progress double-confirm ───────────────────────────────────────

    private void HandleResetProgressConfirmInput()
    {
        if (_font != null)
        {
            var viewport = _graphics.GraphicsDevice.Viewport;
            string[] opts = _resetProgressConfirmStep == 1
                ? ["Yes, continue", "Cancel"]
                : ["Confirm Reset", "Cancel"];
            var confirmStartY = viewport.Height / 2f - 20f;
            for (var i = 0; i < opts.Length; i++)
            {
                var textSize = _font.MeasureString(opts[i]);
                var pos = new Vector2(viewport.Width / 2f - textSize.X / 2f, confirmStartY + i * 50f);
                var bounds = new Rectangle((int)pos.X, (int)pos.Y, (int)textSize.X, (int)textSize.Y);
                if (bounds.Contains(InputManager.GetMousePosition()))
                {
                    _resetProgressConfirmSelectedIndex = i;
                    if (InputManager.MenuConfirm())
                    {
                        ExecuteResetProgressConfirm();
                        InputManager.ConsumeClick();
                    }
                }
            }
        }

        if (InputManager.MenuUp()   || InputManager.MenuLeft())  { _resetProgressConfirmSelectedIndex = (_resetProgressConfirmSelectedIndex - 1 + 2) % 2; }
        if (InputManager.MenuDown() || InputManager.MenuRight()) { _resetProgressConfirmSelectedIndex = (_resetProgressConfirmSelectedIndex + 1) % 2; }
        if (InputManager.MenuConfirm())  { ExecuteResetProgressConfirm(); }
        if (InputManager.MenuBack()) { _resetProgressConfirmStep = 0; _resetProgressConfirmSelectedIndex = 1; }
    }

    private void ExecuteResetProgressConfirm()
    {
        if (_resetProgressConfirmSelectedIndex == 1) // Cancel
        {
            _resetProgressConfirmStep = 0;
            _resetProgressConfirmSelectedIndex = 1;
            return;
        }
        if (_resetProgressConfirmStep == 1)
        {
            _resetProgressConfirmStep = 2;
            _resetProgressConfirmSelectedIndex = 1;
        }
        else
        {
            UnlockTracker.Reset();
            InventoryManagement.ResetSavedLoadout();
            _resetProgressConfirmStep = 0;
            _resetProgressConfirmSelectedIndex = 1;
        }
    }
}
