using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PolyGone.Items
{
    /// <summary>Allows the player to jump a second time while in the air.</summary>
    public class DoubleJumpItem : Item
    {
        private bool hasDoubleJumped = false;

        public DoubleJumpItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Double Jump", "Allows a second jump in mid-air", srcRect) { }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            hasDoubleJumped = false;
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            hasDoubleJumped = false;
        }

        protected override Color GetActiveColor()   => new Color(0, 150, 255, 200);
        protected override Color GetInactiveColor() => new Color(0, 75, 127, 150);

        /// <summary>
        /// Call from Player.HandleInput when checking for a jump.
        /// Returns true if a double jump should be applied this frame.
        /// </summary>
        public bool TryDoubleJump(PolyGone.Entities.Player player, bool spacePressed, bool wasOnGround)
        {
            if (!IsActive) return false;

            if (wasOnGround)
                hasDoubleJumped = false;

            if (spacePressed && player.ChangeY >= 0 && !hasDoubleJumped && !wasOnGround)
            {
                hasDoubleJumped = true;
                return true;
            }

            return false;
        }
    }
}
