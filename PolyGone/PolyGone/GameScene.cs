using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

public class GameScene : IScene
{
    private ContentManager contentManager;
    private Texture2D texture;
    private SceneManager sceneManager;
    public GameScene(ContentManager contentManager, SceneManager sceneManager)
    {
        this.contentManager = contentManager;
        this.sceneManager = sceneManager;
    }

    public void Load()
    {
        texture = contentManager.Load<Texture2D>("player");
    }
    public void Update(GameTime gameTime)
    {
        if (Keyboard.GetState().IsKeyDown(Keys.Space))
        {
            sceneManager.AddScene(new ExitScene(contentManager));
        }
    }
    public void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(texture, new Vector2(100, 100), Color.White);
    }
}