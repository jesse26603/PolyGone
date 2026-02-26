using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PolyGone.Items
{
    /// <summary>
    /// Once every 20 seconds, prevents a killing blow — leaving the player at 1 HP instead.
    /// While on cooldown the indicator fades to gray.
    /// </summary>
    public class IronWillItem : Item
    {
        private const float CooldownSeconds = 20f;

        /// <summary>Remaining cooldown in seconds. 0 means ready to trigger.</summary>
        public float CooldownRemaining { get; private set; } = 0f;

        public bool IsReady => IsActive && CooldownRemaining <= 0f;

        public IronWillItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Iron Will", "Once per 20s, survive a killing blow at 1 HP", srcRect) { }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            CooldownRemaining = 0f;
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            CooldownRemaining = 0f;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (IsActive && CooldownRemaining > 0f)
                CooldownRemaining = System.Math.Max(0f, CooldownRemaining - (float)gameTime.ElapsedGameTime.TotalSeconds);
        }

        /// <summary>
        /// Call from Player damage handling. Returns true if Iron Will absorbed the lethal hit
        /// (player should be set to 1 HP). Can only trigger once per cooldown period.
        /// </summary>
        public bool TryAbsorbLethalHit()
        {
            if (!IsReady) return false;
            CooldownRemaining = CooldownSeconds;
            return true;
        }

        protected override Color GetActiveColor()
        {
            // Fade from bright gold → gray based on remaining cooldown
            float t = CooldownRemaining / CooldownSeconds; // 0 = ready, 1 = just used
            return new Color((int)(255 * (1f - t * 0.5f)), (int)(215 * (1f - t)), 0, 200);
        }

        protected override Color GetInactiveColor() => new Color(100, 80, 0, 150);
    }
}
