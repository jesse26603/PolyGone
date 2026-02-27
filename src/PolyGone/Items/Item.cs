using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PolyGone.Items
{
    public class Item : Sprite
    {
        public string Name { get; protected set; }
        public string Description { get; protected set; }
        public bool IsActive { get; set; } = false;

        public Item(Texture2D texture, Vector2 position, int[] size, Color color, string name, string description, Rectangle? srcRect = null)
            : base(texture, position, size, color, srcRect)
        {
            Name = name;
            Description = description;
        }

        public virtual void Use() { }

        /// <summary>Called when the item is equipped / activated.</summary>
        public virtual void Apply(PolyGone.Entities.Player player)
        {
            IsActive = true;
        }

        /// <summary>Called when the item is unequipped / deactivated.</summary>
        public virtual void Remove(PolyGone.Entities.Player player)
        {
            IsActive = false;
        }

        /// <summary>Draws a small indicator icon at a fixed HUD position.</summary>
        public virtual void DrawIndicator(SpriteBatch spriteBatch, Vector2 indicatorPosition, Texture2D itemTexture, Rectangle sourceRect, int itemIndex)
        {
            const int indicatorSize = 20;
            Rectangle indicatorRect = new Rectangle(
                (int)indicatorPosition.X,
                (int)indicatorPosition.Y,
                indicatorSize,
                indicatorSize
            );

            Color backgroundColor = IsActive ? GetActiveColor() : GetInactiveColor();
            spriteBatch.Draw(itemTexture, indicatorRect, sourceRect, backgroundColor);

            Rectangle keyIndicator = new Rectangle(
                indicatorRect.X + indicatorRect.Width - 8,
                indicatorRect.Y + indicatorRect.Height - 8,
                6, 6
            );
            Color keyColor = itemIndex == 0 ? Color.Red : itemIndex == 1 ? Color.Yellow : Color.Blue;
            spriteBatch.Draw(itemTexture, keyIndicator, sourceRect, keyColor);
        }

        protected virtual Color GetActiveColor()   => new Color(255, 255, 255, 200);
        protected virtual Color GetInactiveColor() => new Color(127, 127, 127, 150);

        public override void Update(GameTime gameTime) => base.Update(gameTime);
    }
}
