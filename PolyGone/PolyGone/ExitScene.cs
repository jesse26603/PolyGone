using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

public class ExitScene : IScene
{
    private ContentManager contentManager;
    private Texture2D texture;
    public ExitScene(ContentManager contentManager)
    {
        this.contentManager = contentManager;
    }

    public void Load() {}
    public void Update(GameTime gameTime) {}
    public void Draw(SpriteBatch spriteBatch) {}
}