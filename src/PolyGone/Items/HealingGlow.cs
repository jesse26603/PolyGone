using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace PolyGone.Items
{
    /// <summary>Heals the player 10 HP every 2 seconds and draws a pulsing green glow outline.</summary>
    public class HealingGlowItem : Item
    {
        private const int   HealAmount   = 10;
        private const float HealInterval = 2.0f;

        private float glowTime  = 0f;
        private float healTimer = 0f;

        public HealingGlowItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Healing Glow", "Heals 10 HP every 2 seconds", srcRect) { }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            glowTime = healTimer = 0f;
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            glowTime = healTimer = 0f;
        }

        protected override Color GetActiveColor()   => new Color(0, 255, 100, 200);
        protected override Color GetInactiveColor() => new Color(0, 127, 50, 150);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (IsActive)
                glowTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        /// <summary>Call from Player.Update each frame to apply regeneration.</summary>
        public void ApplyHealingOverTime(PolyGone.Entities.Player player, float deltaTime)
        {
            if (!IsActive) return;
            healTimer += deltaTime;
            if (healTimer >= HealInterval)
            {
                healTimer -= HealInterval;
                player.health = Math.Min(player.health + HealAmount, player.maxHealth);
            }
        }

        public Color GetGlowColor()
        {
            if (!IsActive) return Color.Transparent;
            float intensity = (float)(Math.Sin(glowTime * 6) * 0.3f + 0.7f);
            return new Color(0, (int)(255 * intensity), 0, (int)(200 * intensity));
        }

        public bool ShouldGlow() => IsActive;
    }
}
