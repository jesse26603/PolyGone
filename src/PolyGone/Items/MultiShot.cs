using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PolyGone.Weapons;

namespace PolyGone.Items
{
    /// <summary>
    /// Adds 3 extra pellets to the Shotgun per blast, bringing the total from 5 to 8.
    /// Has no effect when equipped with a non-Shotgun weapon.
    /// </summary>
    public class MultiShotItem : Item
    {
        private const int ExtraPellets = 3;

        public MultiShotItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Multi-Shot", "Adds 3 extra pellets to the Shotgun", srcRect) { }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            if (player.GetBlaster() is Shotgun sg)
                sg.ExtraPelletsPerShot += ExtraPellets;
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            if (player.GetBlaster() is Shotgun sg)
                sg.ExtraPelletsPerShot -= ExtraPellets;
        }

        protected override Color GetActiveColor()   => new Color(255, 80, 80, 200);
        protected override Color GetInactiveColor() => new Color(127, 40, 40, 150);
    }
}
