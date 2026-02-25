using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PolyGone.Weapons;

namespace PolyGone.Items
{
    /// <summary>Reduces the weapon cooldown to 1/3 of its base value.</summary>
    public class RapidFireItem : Item
    {
        private const float CooldownMultiplier = 1f / 3f;

        public RapidFireItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Rapid Fire", "Reduces weapon cooldown to 1/3", srcRect) { }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            if (player.GetBlaster() is Blaster b)
                b.CooldownMultiplier *= CooldownMultiplier;
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            if (player.GetBlaster() is Blaster b)
                b.CooldownMultiplier /= CooldownMultiplier; // undo the reduction
        }

        protected override Color GetActiveColor()   => new Color(255, 140, 0, 200);
        protected override Color GetInactiveColor() => new Color(127, 70, 0, 150);
    }
}
