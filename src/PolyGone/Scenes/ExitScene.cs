using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

public class ExitScene : IScene
{
    public ExitScene(ContentManager contentManager)
    {
        _ = contentManager;
    }

    public void Load() {}
    public void Update(GameTime gameTime) {}
    public void Draw(SpriteBatch spriteBatch) {}
}
