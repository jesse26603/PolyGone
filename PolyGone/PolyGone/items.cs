using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
namespace PolyGone
{

    class Item : Sprite
    {
        public readonly string ItemType;
        public Item(Texture2D texture, Vector2 position, int[] size, Color color, string itemType, Rectangle? srcRect = null)
            : base(texture, position, size, color, srcRect)
        {
            this.ItemType = itemType;
        }
        class Blaster : Item
        {
            public Blaster(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
                : base(texture, position, size, color, "Blaster", srcRect)
            {
            }
        }
        class BetterJump : Item
        {
            public BetterJump(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
                : base(texture, position, size, color, "BetterJump", srcRect)
            {
            }
        }
    }
    
    
}
