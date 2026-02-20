using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

public class WinScene : IScene
{
    private readonly ContentManager contentManager;
    private readonly SceneManager sceneManager;
    private readonly GraphicsDeviceManager graphics;
    private SpriteFont font;
    private Texture2D pixel;
    private Rectangle resetButtonBounds;
    private bool isButtonHovered;
    private MouseState previousMouseState;

    public WinScene(ContentManager contentManager, SceneManager sceneManager, GraphicsDeviceManager graphics)
    {
        this.contentManager = contentManager;
        this.sceneManager = sceneManager;
        this.graphics = graphics;
    }

    public void Load()
    {
        // Load font
        font = contentManager.Load<SpriteFont>("DefaultFont");
        
        // Create a simple texture for drawing rectangles and text background
        pixel = new Texture2D(graphics.GraphicsDevice, 1, 1);
        pixel.SetData(new[] { Color.White });
        
        // Define button bounds (centered on screen)
        int buttonWidth = 200;
        int buttonHeight = 60;
        int buttonX = (graphics.PreferredBackBufferWidth - buttonWidth) / 2;
        int buttonY = (graphics.PreferredBackBufferHeight / 2) + 50;
        resetButtonBounds = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);
        
        previousMouseState = Mouse.GetState();
    }

    // Clean up resources to prevent memory leaks
    public void Unload()
    {
        pixel?.Dispose();
        pixel = null;
    }

    public void Update(GameTime gameTime)
    {
        MouseState mouseState = Mouse.GetState();
        
        // Check if mouse is hovering over button
        isButtonHovered = resetButtonBounds.Contains(mouseState.Position);
        
        // Check if button was clicked
        if (isButtonHovered && 
            mouseState.LeftButton == ButtonState.Released && 
            previousMouseState.LeftButton == ButtonState.Pressed)
        {
            // Remove WinScene and create new GameScene
            sceneManager.PopScene(this);
            sceneManager.PopScene(sceneManager.GetCurrentScene()); // Remove old GameScene
            sceneManager.AddScene(new GameScene(contentManager, sceneManager, graphics));
        }
        
        previousMouseState = mouseState;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw "YOU WIN!" text
        string winText = "YOU WIN!";
        Vector2 winTextSize = font.MeasureString(winText);
        Vector2 winTextPosition = new Vector2(
            (graphics.PreferredBackBufferWidth - winTextSize.X) / 2,
            graphics.PreferredBackBufferHeight / 2 - 100
        );
        
        // Draw text background
        Rectangle textBg = new Rectangle(
            (int)winTextPosition.X - 20, 
            (int)winTextPosition.Y - 10, 
            (int)winTextSize.X + 40, 
            (int)winTextSize.Y + 20
        );
        spriteBatch.Draw(pixel, textBg, Color.DarkGreen * 0.8f);
        
        // Draw win text
        spriteBatch.DrawString(font, winText, winTextPosition, Color.Gold);
        
        // Draw button
        Color buttonColor = isButtonHovered ? Color.LightGreen : Color.Green;
        spriteBatch.Draw(pixel, resetButtonBounds, buttonColor);
        
        // Draw button border
        DrawRectangleBorder(spriteBatch, pixel, resetButtonBounds, 3, Color.White);
        
        // Draw button text
        string buttonText = "Restart Level";
        Vector2 buttonTextSize = font.MeasureString(buttonText);
        Vector2 buttonTextPosition = new Vector2(
            resetButtonBounds.X + (resetButtonBounds.Width - buttonTextSize.X) / 2,
            resetButtonBounds.Y + (resetButtonBounds.Height - buttonTextSize.Y) / 2
        );
        
        spriteBatch.DrawString(font, buttonText, buttonTextPosition, Color.White);
    }
    
    private void DrawRectangleBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, int thickness, Color color)
    {
        // Top
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        // Bottom
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);
        // Left
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        // Right
        spriteBatch.Draw(pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);
    }
}
