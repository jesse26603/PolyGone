using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PolyGone
{
    internal class Sprite
    {
        public Texture2D texture;
        public Vector2 position;
        public int[] size;
        public Color color;
        public Rectangle? srcRect;
        public Rectangle Rectangle 
        { 
            get 
            { 
                return new Rectangle((int)position.X, (int)position.Y, (int)size[0], (int)size[1]); 
            }
        }
        public Sprite(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null) 
        {
            this.texture = texture;
            this.position = position;
            this.size = size;
            this.color = color;
            this.srcRect = srcRect;
        }

        public virtual void Update(GameTime gameTime) 
        {
            // Default update logic (if any) goes here
        }

        public virtual void Draw(SpriteBatch spriteBatch, Vector2 offset) 
        {
            // Draw the sprite using the provided SpriteBatch
            Rectangle adjustedRectangle = new Rectangle(
                (int)(position.X - offset.X),
                (int)(position.Y - offset.Y),
                size[0],
                size[1]
            );
            spriteBatch.Draw(texture, adjustedRectangle, srcRect, color);
        }
    }
}
