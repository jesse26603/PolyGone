using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PolyGone
{
    class Item : Sprite
    {
        public Item(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, srcRect)
        {
        }

        public virtual void Use()
        {
            // Default use behavior (can be overridden by subclasses)
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }
    }
}