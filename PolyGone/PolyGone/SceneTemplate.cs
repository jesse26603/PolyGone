using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PolyGone;

public interface IScene
{
    public void Load() {}

    public void Update(GameTime gameTime) {}

    public void Draw(SpriteBatch spriteBatch) {}
}