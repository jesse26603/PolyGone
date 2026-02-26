using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace PolyGone.Items
{
    /// <summary>
    /// Reduces gravity to 40% of normal. Jump velocity is scaled by sqrt(gravityScale)
    /// so peak height stays identical — the player simply rises and falls more slowly.
    /// </summary>
    public class LowGravityItem : Item
    {
        private const float GravityScale = 0.4f;
        // sqrt(0.4) ≈ 0.6325 — keeps peak height (v²/2g) constant when g is scaled
        private static readonly float JumpScale = (float)Math.Sqrt(GravityScale);
        private const float BaseJumpStrength = -16f;

        public LowGravityItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Low Gravity", "Floatier movement, same jump height", srcRect) { }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            player.GravityScale  = GravityScale;
            player.JumpStrength  = BaseJumpStrength * JumpScale;
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            player.GravityScale = 1.0f;
            player.JumpStrength = BaseJumpStrength;
        }

        protected override Color GetActiveColor()   => new Color(180, 100, 255, 200);
        protected override Color GetInactiveColor() => new Color(90, 50, 127, 150);
    }
}
