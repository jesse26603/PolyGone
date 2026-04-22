using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace PolyGone;

public enum InputAction
{
    GameShoot,
    GameJump,
    GameMoveLeft,
    GameMoveRight,
    GameDrop,
    GameInteract,
    Pause,
    MenuUp,
    MenuDown,
    MenuLeft,
    MenuRight,
    MenuConfirm,
    MenuBack,
    LoadoutSectionLeft,
    LoadoutSectionRight,
    LoadoutSectionDown,
    LoadoutSkip,
    DevPaymentBypass,
}

public enum GamepadButtonBinding
{
    A,
    B,
    X,
    Y,
    LeftShoulder,
    RightShoulder,
    Back,
    Start,
    DPadUp,
    DPadDown,
    DPadLeft,
    DPadRight,
    LeftTrigger,
    RightTrigger,
}

/// <summary>
/// Centralized input manager to track mouse, keyboard, and gamepad state across scenes.
/// Prevents input events from carrying over between scene transitions and supports rebinding.
/// </summary>
public static class InputManager
{
    private static readonly Queue<char> _typedChars = new();
    private static readonly Dictionary<InputAction, Keys?> _keyboardBindings = new();
    private static readonly Dictionary<InputAction, GamepadButtonBinding?> _gamepadBindings = new();

    private static readonly InputAction[] _remappableActions =
    [
        InputAction.GameShoot,
        InputAction.GameJump,
        InputAction.GameMoveLeft,
        InputAction.GameMoveRight,
        InputAction.GameDrop,
        InputAction.GameInteract,
        InputAction.Pause,
        InputAction.MenuUp,
        InputAction.MenuDown,
        InputAction.MenuLeft,
        InputAction.MenuRight,
        InputAction.MenuConfirm,
        InputAction.MenuBack,
        InputAction.LoadoutSectionLeft,
        InputAction.LoadoutSectionRight,
        InputAction.LoadoutSectionDown,
        InputAction.LoadoutSkip,
    ];

    private static readonly string SavePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PolyGone",
        "controls.json");

    private static MouseState _currentMouseState;
    private static MouseState _previousMouseState;
    private static KeyboardState _currentKeyboardState;
    private static KeyboardState _previousKeyboardState;
    private static GamePadState _currentGamepadState;
    private static GamePadState _previousGamepadState;
    private static float thumbstickX;
    private static float thumbstickY;
    private static bool _usingController;
    private static Vector2 _rightThumbstick;

    private static float _mouseClickCooldown;
    private static float _escapeKeyCooldown;
    private static float _menuUpHoldTimer;
    private static float _menuDownHoldTimer;
    private static float _menuLeftHoldTimer;
    private static float _menuRightHoldTimer;
    private static float _menuUpAutoRepeatTimer;
    private static float _menuDownAutoRepeatTimer;
    private static float _menuLeftAutoRepeatTimer;
    private static float _menuRightAutoRepeatTimer;

    private const float MENU_AUTO_REPEAT_INTERVAL = 0.25f;
    private const float CLICK_COOLDOWN = 0.01f;
    private const float ESCAPE_COOLDOWN = 0.2f;
    private const float TriggerThreshold = 0.1f;

    public static bool UsingController => _usingController;
    public static Vector2 RightThumbstick => _rightThumbstick;
    public static MouseState CurrentMouseState => _currentMouseState;
    public static MouseState PreviousMouseState => _previousMouseState;
    public static IReadOnlyList<InputAction> RemappableActions => _remappableActions;

    static InputManager()
    {
        ResetBindingsToDefaults();
        LoadBindings();
    }

    public static void OnTextInput(object? sender, TextInputEventArgs e)
    {
        _typedChars.Enqueue(e.Character);
    }

    public static string ConsumeTypedCharacters()
    {
        var sb = new StringBuilder();
        while (_typedChars.Count > 0)
        {
            sb.Append(_typedChars.Dequeue());
        }
        return sb.ToString();
    }

    public static void Update(GameTime gameTime)
    {
        _previousMouseState = _currentMouseState;
        _currentMouseState = Mouse.GetState();

        _previousKeyboardState = _currentKeyboardState;
        _currentKeyboardState = Keyboard.GetState();

        _previousGamepadState = _currentGamepadState;
        _currentGamepadState = GamePad.GetState(PlayerIndex.One);

        thumbstickX = _currentGamepadState.ThumbSticks.Left.X;
        thumbstickY = _currentGamepadState.ThumbSticks.Left.Y;
        _rightThumbstick = new Vector2(_currentGamepadState.ThumbSticks.Right.X, -_currentGamepadState.ThumbSticks.Right.Y);

        bool controllerInput = Math.Abs(_rightThumbstick.X) > 0.3f ||
                               Math.Abs(_rightThumbstick.Y) > 0.3f ||
                               Math.Abs(thumbstickX) > 0.1f ||
                               Math.Abs(thumbstickY) > 0.1f ||
                               _currentGamepadState.IsConnected && _currentGamepadState.Buttons != default ||
                               AnyMappedGamepadInputHeld();
        bool mouseInput = _currentMouseState.Position != _previousMouseState.Position ||
                          _currentMouseState.LeftButton == ButtonState.Pressed;

        if (controllerInput && !mouseInput)
        {
            _usingController = true;
        }
        else if (mouseInput || _currentKeyboardState.GetPressedKeys().Length > 0)
        {
            _usingController = false;
        }

        if (_mouseClickCooldown > 0f)
        {
            _mouseClickCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        if (_escapeKeyCooldown > 0f)
        {
            _escapeKeyCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        UpdateMenuHoldTimers((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    public static void ConsumeClick()
    {
        _mouseClickCooldown = CLICK_COOLDOWN;
    }

    public static bool IsLeftMouseButtonClicked()
    {
        return _currentMouseState.LeftButton == ButtonState.Pressed &&
               _previousMouseState.LeftButton == ButtonState.Released &&
               _mouseClickCooldown <= 0f;
    }

    public static bool IsLeftMouseButtonHeld()
    {
        return _currentMouseState.LeftButton == ButtonState.Pressed;
    }

    public static bool GameShootSingle()
    {
        bool mouseClicked = IsLeftMouseButtonClicked();
        bool gamepadClicked = IsGamepadTriggered(InputAction.GameShoot);
        return mouseClicked || gamepadClicked;
    }

    public static bool GameShootHold()
    {
        return _currentMouseState.LeftButton == ButtonState.Pressed ||
               IsGamepadHeld(InputAction.GameShoot);
    }

    public static bool GameJump()
    {
        return IsKeyboardTriggered(InputAction.GameJump) || IsGamepadTriggered(InputAction.GameJump);
    }

    public static bool GameMoveLeft()
    {
        bool keyboardLeft = IsKeyboardHeld(InputAction.GameMoveLeft);
        bool gamepadLeft = thumbstickX < -0.3f;
        return keyboardLeft || gamepadLeft;
    }

    public static bool GameMoveRight()
    {
        bool keyboardRight = IsKeyboardHeld(InputAction.GameMoveRight);
        bool gamepadRight = thumbstickX > 0.3f;
        return keyboardRight || gamepadRight;
    }

    public static bool GameDrop()
    {
        bool keyboardDrop = IsKeyboardHeld(InputAction.GameDrop);
        bool gamepadDrop = thumbstickY < -0.9f;
        return keyboardDrop || gamepadDrop;
    }

    public static bool GameInteract()
    {
        return IsKeyboardTriggered(InputAction.GameInteract) || IsGamepadTriggered(InputAction.GameInteract);
    }

    public static Vector2 GameAim()
    {
        if (_usingController)
        {
            return _rightThumbstick;
        }

        Point mousePos = GetMousePosition();
        return new Vector2(mousePos.X, mousePos.Y);
    }

    public static bool PauseMenuOpen()
    {
        bool pressed = IsKeyboardTriggered(InputAction.Pause) || IsGamepadTriggered(InputAction.Pause);
        if (!pressed || _escapeKeyCooldown > 0f)
        {
            return false;
        }

        _escapeKeyCooldown = ESCAPE_COOLDOWN;
        return true;
    }

    public static bool PauseMenuClose()
    {
        return PauseMenuOpen();
    }

    public static bool MenuUp()
    {
        bool pressed = IsKeyboardTriggered(InputAction.MenuUp) ||
                       thumbstickY > 0.3f && _previousGamepadState.ThumbSticks.Left.Y <= 0.3f ||
                       IsGamepadTriggered(InputAction.MenuUp);
        bool held = IsKeyboardHeld(InputAction.MenuUp) || thumbstickY > 0.3f || IsGamepadHeld(InputAction.MenuUp);
        return pressed || GetAutoRepeat(ref _menuUpAutoRepeatTimer, _menuUpHoldTimer, held);
    }

    public static bool MenuDown()
    {
        bool pressed = IsKeyboardTriggered(InputAction.MenuDown) ||
                       thumbstickY < -0.3f && _previousGamepadState.ThumbSticks.Left.Y >= -0.3f ||
                       IsGamepadTriggered(InputAction.MenuDown);
        bool held = IsKeyboardHeld(InputAction.MenuDown) || thumbstickY < -0.3f || IsGamepadHeld(InputAction.MenuDown);
        return pressed || GetAutoRepeat(ref _menuDownAutoRepeatTimer, _menuDownHoldTimer, held);
    }

    public static bool MenuLeft()
    {
        bool pressed = IsKeyboardTriggered(InputAction.MenuLeft) ||
                       thumbstickX < -0.3f && _previousGamepadState.ThumbSticks.Left.X >= -0.3f ||
                       IsGamepadTriggered(InputAction.MenuLeft);
        bool held = IsKeyboardHeld(InputAction.MenuLeft) || thumbstickX < -0.3f || IsGamepadHeld(InputAction.MenuLeft);
        return pressed || GetAutoRepeat(ref _menuLeftAutoRepeatTimer, _menuLeftHoldTimer, held);
    }

    public static bool MenuRight()
    {
        bool pressed = IsKeyboardTriggered(InputAction.MenuRight) ||
                       thumbstickX > 0.3f && _previousGamepadState.ThumbSticks.Left.X <= 0.3f ||
                       IsGamepadTriggered(InputAction.MenuRight);
        bool held = IsKeyboardHeld(InputAction.MenuRight) || thumbstickX > 0.3f || IsGamepadHeld(InputAction.MenuRight);
        return pressed || GetAutoRepeat(ref _menuRightAutoRepeatTimer, _menuRightHoldTimer, held);
    }

    public static bool MenuMouseConfirm()
    {
        return IsLeftMouseButtonClicked();
    }

    public static bool MenuNonPointerConfirm()
    {
        return IsKeyboardTriggered(InputAction.MenuConfirm) || IsGamepadTriggered(InputAction.MenuConfirm);
    }

    public static bool MenuConfirm()
    {
        return MenuMouseConfirm() || MenuNonPointerConfirm();
    }

    public static bool MenuConfirmMouseClick()
    {
        return IsLeftMouseButtonClicked();
    }

    public static bool MenuBack()
    {
        bool pressed = IsKeyboardTriggered(InputAction.MenuBack) || IsGamepadTriggered(InputAction.MenuBack);
        if (!pressed || _escapeKeyCooldown > 0f)
        {
            return false;
        }

        _escapeKeyCooldown = ESCAPE_COOLDOWN;
        return true;
    }

    public static bool LoadoutSectionLeft()
    {
        return IsKeyboardTriggered(InputAction.LoadoutSectionLeft) ||
               IsGamepadTriggered(InputAction.LoadoutSectionLeft) ||
               thumbstickX < -0.3f && _previousGamepadState.ThumbSticks.Left.X >= -0.3f;
    }

    public static bool LoadoutSectionRight()
    {
        return IsKeyboardTriggered(InputAction.LoadoutSectionRight) ||
               IsGamepadTriggered(InputAction.LoadoutSectionRight) ||
               thumbstickX > 0.3f && _previousGamepadState.ThumbSticks.Left.X <= 0.3f;
    }

    public static bool LoadoutSectionDown()
    {
        return IsKeyboardTriggered(InputAction.LoadoutSectionDown) ||
               IsGamepadTriggered(InputAction.LoadoutSectionDown) ||
               thumbstickY < -0.3f && _previousGamepadState.ThumbSticks.Left.Y >= -0.3f;
    }

    public static bool LoadoutSkip()
    {
        return IsKeyboardTriggered(InputAction.LoadoutSkip) || IsGamepadTriggered(InputAction.LoadoutSkip);
    }

    public static bool DevPaymentBypass()
    {
        bool keyboardBypass = _currentKeyboardState.IsKeyDown(Keys.LeftControl)
                              && _currentKeyboardState.IsKeyDown(Keys.LeftShift)
                              && _currentKeyboardState.IsKeyDown(Keys.D)
                              && !_previousKeyboardState.IsKeyDown(Keys.LeftControl)
                              && !_previousKeyboardState.IsKeyDown(Keys.LeftShift)
                              && !_previousKeyboardState.IsKeyDown(Keys.D);
        bool gamepadBypass = _currentGamepadState.Buttons.Start == ButtonState.Pressed
                             && _previousGamepadState.Buttons.Start == ButtonState.Released
                             && _currentGamepadState.Buttons.A == ButtonState.Pressed
                             && _previousGamepadState.Buttons.A == ButtonState.Released;
        return keyboardBypass || gamepadBypass;
    }

    public static void ResetClickCooldown()
    {
        _mouseClickCooldown = CLICK_COOLDOWN;
    }

    public static Point GetMousePosition()
    {
        return _currentMouseState.Position;
    }

    public static string GetActionDisplayName(InputAction action)
    {
        return action switch
        {
            InputAction.GameShoot => "Shoot",
            InputAction.GameJump => "Jump",
            InputAction.GameMoveLeft => "Move Left",
            InputAction.GameMoveRight => "Move Right",
            InputAction.GameDrop => "Drop",
            InputAction.GameInteract => "Interact",
            InputAction.Pause => "Pause",
            InputAction.MenuUp => "Menu Up",
            InputAction.MenuDown => "Menu Down",
            InputAction.MenuLeft => "Menu Left",
            InputAction.MenuRight => "Menu Right",
            InputAction.MenuConfirm => "Menu Confirm",
            InputAction.MenuBack => "Menu Back",
            InputAction.LoadoutSectionLeft => "Loadout Left",
            InputAction.LoadoutSectionRight => "Loadout Right",
            InputAction.LoadoutSectionDown => "Loadout Down",
            InputAction.LoadoutSkip => "Loadout Skip",
            InputAction.DevPaymentBypass => "Dev Bypass",
            _ => action.ToString(),
        };
    }

    public static Keys? GetKeyboardBinding(InputAction action)
    {
        if (_keyboardBindings.TryGetValue(action, out var key))
        {
            return key;
        }

        return null;
    }

    public static GamepadButtonBinding? GetGamepadBinding(InputAction action)
    {
        if (_gamepadBindings.TryGetValue(action, out var button))
        {
            return button;
        }

        return null;
    }

    public static bool IsKeyboardRebindable(InputAction action) => action != InputAction.DevPaymentBypass;
    public static bool IsGamepadRebindable(InputAction action) => action != InputAction.DevPaymentBypass;

    public static void SetKeyboardBinding(InputAction action, Keys? key)
    {
        if (!IsKeyboardRebindable(action))
        {
            return;
        }

        _keyboardBindings[action] = key;
        SaveBindings();
    }

    public static void SetGamepadBinding(InputAction action, GamepadButtonBinding? button)
    {
        if (!IsGamepadRebindable(action))
        {
            return;
        }

        _gamepadBindings[action] = button;
        SaveBindings();
    }

    public static void ResetBindingsToDefaults()
    {
        _keyboardBindings.Clear();
        _gamepadBindings.Clear();

        _keyboardBindings[InputAction.GameShoot] = null;
        _keyboardBindings[InputAction.GameJump] = Keys.Space;
        _keyboardBindings[InputAction.GameMoveLeft] = Keys.A;
        _keyboardBindings[InputAction.GameMoveRight] = Keys.D;
        _keyboardBindings[InputAction.GameDrop] = Keys.S;
        _keyboardBindings[InputAction.GameInteract] = Keys.W;
        _keyboardBindings[InputAction.Pause] = Keys.Escape;
        _keyboardBindings[InputAction.MenuUp] = Keys.Up;
        _keyboardBindings[InputAction.MenuDown] = Keys.Down;
        _keyboardBindings[InputAction.MenuLeft] = Keys.Left;
        _keyboardBindings[InputAction.MenuRight] = Keys.Right;
        _keyboardBindings[InputAction.MenuConfirm] = Keys.Enter;
        _keyboardBindings[InputAction.MenuBack] = Keys.Escape;
        _keyboardBindings[InputAction.LoadoutSectionLeft] = Keys.Left;
        _keyboardBindings[InputAction.LoadoutSectionRight] = Keys.Right;
        _keyboardBindings[InputAction.LoadoutSectionDown] = Keys.Down;
        _keyboardBindings[InputAction.LoadoutSkip] = Keys.LeftControl;
        _keyboardBindings[InputAction.DevPaymentBypass] = null;

        _gamepadBindings[InputAction.GameShoot] = GamepadButtonBinding.RightTrigger;
        _gamepadBindings[InputAction.GameJump] = GamepadButtonBinding.A;
        _gamepadBindings[InputAction.GameMoveLeft] = null;
        _gamepadBindings[InputAction.GameMoveRight] = null;
        _gamepadBindings[InputAction.GameDrop] = null;
        _gamepadBindings[InputAction.GameInteract] = GamepadButtonBinding.X;
        _gamepadBindings[InputAction.Pause] = GamepadButtonBinding.Start;
        _gamepadBindings[InputAction.MenuUp] = GamepadButtonBinding.DPadUp;
        _gamepadBindings[InputAction.MenuDown] = GamepadButtonBinding.DPadDown;
        _gamepadBindings[InputAction.MenuLeft] = GamepadButtonBinding.DPadLeft;
        _gamepadBindings[InputAction.MenuRight] = GamepadButtonBinding.DPadRight;
        _gamepadBindings[InputAction.MenuConfirm] = GamepadButtonBinding.A;
        _gamepadBindings[InputAction.MenuBack] = GamepadButtonBinding.B;
        _gamepadBindings[InputAction.LoadoutSectionLeft] = GamepadButtonBinding.DPadLeft;
        _gamepadBindings[InputAction.LoadoutSectionRight] = GamepadButtonBinding.DPadRight;
        _gamepadBindings[InputAction.LoadoutSectionDown] = GamepadButtonBinding.DPadDown;
        _gamepadBindings[InputAction.LoadoutSkip] = GamepadButtonBinding.Back;
        _gamepadBindings[InputAction.DevPaymentBypass] = null;
    }

    public static void SaveBindings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SavePath)!);
            var data = new BindingSaveData
            {
                Keyboard = _keyboardBindings.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value?.ToString()),
                Gamepad = _gamepadBindings.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value?.ToString()),
            };
            File.WriteAllText(SavePath, JsonSerializer.Serialize(data));
        }
        catch
        {
            // no-op
        }
    }

    public static void LoadBindings()
    {
        try
        {
            if (!File.Exists(SavePath))
            {
                return;
            }

            var data = JsonSerializer.Deserialize<BindingSaveData>(File.ReadAllText(SavePath));
            if (data == null)
            {
                return;
            }

            if (data.Keyboard != null)
            {
                foreach (var (actionName, keyName) in data.Keyboard)
                {
                    if (!Enum.TryParse(actionName, out InputAction action))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(keyName))
                    {
                        _keyboardBindings[action] = null;
                    }
                    else if (Enum.TryParse(keyName, out Keys key))
                    {
                        _keyboardBindings[action] = key;
                    }
                }
            }

            if (data.Gamepad != null)
            {
                foreach (var (actionName, buttonName) in data.Gamepad)
                {
                    if (!Enum.TryParse(actionName, out InputAction action))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(buttonName))
                    {
                        _gamepadBindings[action] = null;
                    }
                    else if (Enum.TryParse(buttonName, out GamepadButtonBinding button))
                    {
                        _gamepadBindings[action] = button;
                    }
                }
            }
        }
        catch
        {
            // no-op
        }
    }

    public static Keys[] GetJustPressedKeys()
    {
        var current = _currentKeyboardState.GetPressedKeys();
        return current.Where(k => _previousKeyboardState.IsKeyUp(k)).ToArray();
    }

    public static GamepadButtonBinding? GetJustPressedGamepadBinding()
    {
        foreach (var binding in Enum.GetValues<GamepadButtonBinding>())
        {
            if (IsGamepadBindingPressed(binding, _currentGamepadState) &&
                !IsGamepadBindingPressed(binding, _previousGamepadState))
            {
                return binding;
            }
        }
        return null;
    }

    private static bool IsKeyboardTriggered(InputAction action)
    {
        Keys? key = GetKeyboardBinding(action);
        return key.HasValue && _currentKeyboardState.IsKeyDown(key.Value) && _previousKeyboardState.IsKeyUp(key.Value);
    }

    private static bool IsKeyboardHeld(InputAction action)
    {
        Keys? key = GetKeyboardBinding(action);
        return key.HasValue && _currentKeyboardState.IsKeyDown(key.Value);
    }

    private static bool IsGamepadTriggered(InputAction action)
    {
        GamepadButtonBinding? binding = GetGamepadBinding(action);
        if (!binding.HasValue)
        {
            return false;
        }

        return IsGamepadBindingPressed(binding.Value, _currentGamepadState) &&
               !IsGamepadBindingPressed(binding.Value, _previousGamepadState);
    }

    private static bool IsGamepadHeld(InputAction action)
    {
        GamepadButtonBinding? binding = GetGamepadBinding(action);
        return binding.HasValue && IsGamepadBindingPressed(binding.Value, _currentGamepadState);
    }

    private static bool IsGamepadBindingPressed(GamepadButtonBinding binding, GamePadState state)
    {
        return binding switch
        {
            GamepadButtonBinding.A => state.Buttons.A == ButtonState.Pressed,
            GamepadButtonBinding.B => state.Buttons.B == ButtonState.Pressed,
            GamepadButtonBinding.X => state.Buttons.X == ButtonState.Pressed,
            GamepadButtonBinding.Y => state.Buttons.Y == ButtonState.Pressed,
            GamepadButtonBinding.LeftShoulder => state.Buttons.LeftShoulder == ButtonState.Pressed,
            GamepadButtonBinding.RightShoulder => state.Buttons.RightShoulder == ButtonState.Pressed,
            GamepadButtonBinding.Back => state.Buttons.Back == ButtonState.Pressed,
            GamepadButtonBinding.Start => state.Buttons.Start == ButtonState.Pressed,
            GamepadButtonBinding.DPadUp => state.DPad.Up == ButtonState.Pressed,
            GamepadButtonBinding.DPadDown => state.DPad.Down == ButtonState.Pressed,
            GamepadButtonBinding.DPadLeft => state.DPad.Left == ButtonState.Pressed,
            GamepadButtonBinding.DPadRight => state.DPad.Right == ButtonState.Pressed,
            GamepadButtonBinding.LeftTrigger => state.Triggers.Left > TriggerThreshold,
            GamepadButtonBinding.RightTrigger => state.Triggers.Right > TriggerThreshold,
            _ => false,
        };
    }

    private static bool AnyMappedGamepadInputHeld()
    {
        foreach (var action in _gamepadBindings.Keys)
        {
            if (IsGamepadHeld(action))
            {
                return true;
            }
        }
        return false;
    }

    private static void UpdateMenuHoldTimers(float dt)
    {
        _menuUpHoldTimer = (IsKeyboardHeld(InputAction.MenuUp) || IsGamepadHeld(InputAction.MenuUp) || thumbstickY > 0.3f) ? _menuUpHoldTimer + dt : 0f;
        _menuDownHoldTimer = (IsKeyboardHeld(InputAction.MenuDown) || IsGamepadHeld(InputAction.MenuDown) || thumbstickY < -0.3f) ? _menuDownHoldTimer + dt : 0f;
        _menuLeftHoldTimer = (IsKeyboardHeld(InputAction.MenuLeft) || IsGamepadHeld(InputAction.MenuLeft) || thumbstickX < -0.3f) ? _menuLeftHoldTimer + dt : 0f;
        _menuRightHoldTimer = (IsKeyboardHeld(InputAction.MenuRight) || IsGamepadHeld(InputAction.MenuRight) || thumbstickX > 0.3f) ? _menuRightHoldTimer + dt : 0f;

        _menuUpAutoRepeatTimer += dt;
        _menuDownAutoRepeatTimer += dt;
        _menuLeftAutoRepeatTimer += dt;
        _menuRightAutoRepeatTimer += dt;
    }

    private static bool GetAutoRepeat(ref float timer, float holdTimer, bool held)
    {
        bool autoRepeat = held && holdTimer >= 1.0f && timer >= MENU_AUTO_REPEAT_INTERVAL;
        if (autoRepeat)
        {
            timer = 0f;
        }
        return autoRepeat;
    }

    private sealed class BindingSaveData
    {
        public Dictionary<string, string?>? Keyboard { get; set; }
        public Dictionary<string, string?>? Gamepad { get; set; }
    }
}
