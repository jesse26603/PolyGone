using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SceneManager sceneManager;
    private KeyboardState _previousKeyboardState;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        sceneManager = new();

        // Resize window to screen size
        var screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
        var screenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        _graphics.PreferredBackBufferWidth = screenWidth;
        _graphics.PreferredBackBufferHeight = screenHeight;
        _graphics.ApplyChanges();
    }

    protected override void Initialize()
    {
        // Hook up text input so scenes can accept keyboard text entry
        Window.TextInput += InputManager.OnTextInput;

        base.Initialize();
    }


    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Load purchase tracker before any scene that might need it
        PurchaseTracker.Load();

        sceneManager.AddScene(new GameScene(Content, sceneManager, _graphics));
        sceneManager.AddScene(new MenuScene(Content, sceneManager, _graphics));

        // Try to restore a saved session (skips the login screen on subsequent launches)
        bool sessionRestored = FormbarSession.TryLoadSession();

        if (!sessionRestored)
        {
            // First launch or session expired: require login
            sceneManager.AddScene(new FormbarLoginScene(Content, sceneManager, _graphics));
        }
        else if (!PurchaseTracker.HasPurchased(FormbarSession.UserId, FormbarSession.AllLevelsKey))
        {
            // Logged in but hasn't paid yet: require payment before accessing the menu
            sceneManager.AddScene(new PaymentScene(Content, sceneManager, _graphics));
        }
        // else: logged in and paid – menu is immediately accessible
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();
        
        // Update centralized input manager
        InputManager.Update(gameTime);

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        if (IsKeyPressed(Keys.Escape, keyboardState))
        {
            if (sceneManager.GetCurrentScene() is PauseScene)
            {
                sceneManager.PopScene(sceneManager.GetCurrentScene());
            }
            else if (sceneManager.GetCurrentScene() is GameScene gameScene)
            {
                sceneManager.AddScene(new PauseScene(Content, sceneManager, _graphics, gameScene));
                // Reset click cooldown when opening pause menu
                InputManager.ResetClickCooldown();
            }
        }

        // TODO: Add your update logic here
        sceneManager.GetCurrentScene().Update(gameTime);
        _previousKeyboardState = keyboardState;
        base.Update(gameTime);
    }
    private bool IsKeyPressed(Keys key, KeyboardState currentState)
    {
        return currentState.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);

        // TODO: Add your drawing code here
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        sceneManager.GetCurrentScene().Draw(_spriteBatch);

        _spriteBatch.End();


        base.Draw(gameTime);
    }
}