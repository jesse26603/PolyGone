using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PolyGone.Weapons;

namespace PolyGone.Items
{
    /// <summary>
    /// Adds 2 extra bullets per shot to every weapon.
    /// The extra bullets fan out in a small spread around the main shot.
    /// </summary>
    public class MultiShotItem : Item
    {
        private const int ExtraBullets = 2;

        public MultiShotItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Multi-Shot", "Adds 2 extra bullets per shot to every weapon", srcRect) { }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            if (player.GetBlaster() is Blaster b)
            {
                b.ExtraBulletsPerShot += ExtraBullets;
            }
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            if (player.GetBlaster() is Blaster b)
            {
                b.ExtraBulletsPerShot -= ExtraBullets;
            }
        }

        protected override Color GetActiveColor()   => new Color(255, 80, 80, 200);
        protected override Color GetInactiveColor() => new Color(127, 40, 40, 150);
    }
}
