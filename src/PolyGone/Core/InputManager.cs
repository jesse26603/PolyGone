using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

/// <summary>
/// Centralized input manager to track mouse and keyboard state across scenes.
/// Prevents input events from carrying over between scene transitions.
/// </summary>
public static class InputManager
{
    // Text-input buffer for scenes that need keyboard text entry (login, PIN, etc.)
    private static readonly Queue<char> _typedChars = new();

    /// <summary>
    /// Subscribe this to <c>Game.Window.TextInput</c> in <see cref="Game1"/>.
    /// Enqueues every character (including backspace '\b') for consumption by scenes.
    /// </summary>
    public static void OnTextInput(object? sender, TextInputEventArgs e)
    {
        _typedChars.Enqueue(e.Character);
    }

    /// <summary>
    /// Returns all characters typed since the last call and clears the internal buffer.
    /// Scenes should call this once per <c>Update</c> and process each character.
    /// A '\b' (backspace) character signals a delete-last action.
    /// </summary>
    public static string ConsumeTypedCharacters()
    {
        var sb = new StringBuilder();
        while (_typedChars.Count > 0)
            sb.Append(_typedChars.Dequeue());
        return sb.ToString();
    }

    private static MouseState _currentMouseState;
    private static MouseState _previousMouseState;
    private static KeyboardState _currentKeyboardState;
    private static KeyboardState _previousKeyboardState;
    private static float _mouseClickCooldown = 0f;
    private static float _escapeKeyCooldown = 0f;
    private const float CLICK_COOLDOWN = 0.01f; // 10ms between clicks
    private const float ESCAPE_COOLDOWN = 0.2f; // 200ms between escape presses
    
    public static MouseState CurrentMouseState => _currentMouseState;
    public static MouseState PreviousMouseState => _previousMouseState;
    
    /// <summary>
    /// Updates the input state. Should be called once per frame in Game1.Update()
    /// </summary>
    public static void Update(GameTime gameTime)
    {
        _previousMouseState = _currentMouseState;
        _currentMouseState = Mouse.GetState();
        
        _previousKeyboardState = _currentKeyboardState;
        _currentKeyboardState = Keyboard.GetState();
        
        // Update click cooldown
        if (_mouseClickCooldown > 0f)
            _mouseClickCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            
        // Update escape key cooldown
        if (_escapeKeyCooldown > 0f)
            _escapeKeyCooldown -= (float)gameTime.ElapsedGameTime.TotalSeconds;
    }
    
    /// <summary>
    /// Checks if the left mouse button was just clicked (pressed and released since last frame)
    /// and the cooldown has expired. This prevents double-clicks and scene transition issues.
    /// </summary>
    public static bool IsLeftMouseButtonClicked()
    {
        return _currentMouseState.LeftButton == ButtonState.Pressed 
            && _previousMouseState.LeftButton == ButtonState.Released 
            && _mouseClickCooldown <= 0f;
    }

    /// <summary>
    /// Checks if the left mouse button is currently held down (any frame, no cooldown check).
    /// Use this for automatic weapons that fire while the button is held.
    /// </summary>
    public static bool IsLeftMouseButtonHeld()
    {
        return _currentMouseState.LeftButton == ButtonState.Pressed;
    }
    
    /// <summary>
    /// Consumes the click by starting the cooldown timer.
    /// Call this after handling a click to prevent it from triggering multiple actions.
    /// </summary>
    public static void ConsumeClick()
    {
        _mouseClickCooldown = CLICK_COOLDOWN;
    }
    
    /// <summary>
    /// Forces a reset of the click cooldown. Useful when transitioning between scenes
    /// to ensure old clicks don't carry over.
    /// </summary>
    public static void ResetClickCooldown()
    {
        _mouseClickCooldown = CLICK_COOLDOWN;
    }
    
    /// <summary>
    /// Checks if the Escape key was just pressed (down now, up before).
    /// This prevents the key press from being processed by multiple scenes by tracking state globally.
    /// </summary>
    public static bool IsEscapeKeyPressed()
    {
        return _currentKeyboardState.IsKeyDown(Keys.Escape) 
            && !_previousKeyboardState.IsKeyDown(Keys.Escape);
    }
    
    /// <summary>
    /// Gets the current mouse position.
    /// </summary>
    public static Point GetMousePosition()
    {
        return _currentMouseState.Position;
    }
}
