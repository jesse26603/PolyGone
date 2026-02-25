using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PolyGone.Weapons;

namespace PolyGone.Items
{
    /// <summary>
    /// Fires 2 extra projectiles per shot in a slight spread pattern,
    /// bringing the total to 3 bullets per click.
    /// </summary>
    public class MultiShotItem : Item
    {
        private const int ExtraBullets = 2;

        public MultiShotItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Multi-Shot", "Fires 3 projectiles per shot", srcRect) { }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            if (player.GetBlaster() is Blaster b)
                b.ExtraBulletsPerShot += ExtraBullets;
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            if (player.GetBlaster() is Blaster b)
                b.ExtraBulletsPerShot -= ExtraBullets;
        }

        protected override Color GetActiveColor()   => new Color(255, 80, 80, 200);
        protected override Color GetInactiveColor() => new Color(127, 40, 40, 150);
    }
}
