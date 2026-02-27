using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PolyGone.Items
{
    /// <summary>Increases the player's movement speed by 50%.</summary>
    public class SpeedBoostItem : Item
    {
        private const float SpeedMultiplier = 1.5f;

        public SpeedBoostItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Speed Boost", "Increases movement speed by 50%", srcRect) { }

        protected override Color GetActiveColor()   => new Color(255, 215, 0, 200);
        protected override Color GetInactiveColor() => new Color(127, 107, 0, 150);

        public float GetSpeedMultiplier() => IsActive ? SpeedMultiplier : 1.0f;
    }
}
