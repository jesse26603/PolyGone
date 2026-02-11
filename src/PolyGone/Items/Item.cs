using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace PolyGone.Items
{
    class Item : PolyGone.Sprite
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

        public virtual void Use()
        {
            // Default use behavior (can be overridden by subclasses)
        }

        // Called when item is equipped/activated
        public virtual void Apply(PolyGone.Entities.Player player)
        {
            IsActive = true;
        }

        // Called when item is unequipped/deactivated
        public virtual void Remove(PolyGone.Entities.Player player)
        {
            IsActive = false;
        }

        // Draw the item indicator at a fixed screen position
        public virtual void DrawIndicator(SpriteBatch spriteBatch, Vector2 indicatorPosition, Texture2D itemTexture, Rectangle sourceRect, int itemIndex)
        {
            int indicatorWidth = 20;
            int indicatorHeight = 20;
            
            // Create a rectangle for the indicator
            Rectangle indicatorRect = new Rectangle(
                (int)indicatorPosition.X, 
                (int)indicatorPosition.Y, 
                indicatorWidth, 
                indicatorHeight
            );

            // Determine colors based on item type and status
            Color backgroundColor = IsActive ? GetActiveColor() : GetInactiveColor();

            // Draw the textured background with color tint
            spriteBatch.Draw(itemTexture, indicatorRect, sourceRect, backgroundColor);

            // Draw key number indicator (small rectangle in corner)
            Rectangle keyIndicator = new Rectangle(
                indicatorRect.X + indicatorRect.Width - 8,
                indicatorRect.Y + indicatorRect.Height - 8,
                6, 6
            );
            Color keyColor = itemIndex == 0 ? Color.Red : itemIndex == 1 ? Color.Yellow : Color.Blue;
            spriteBatch.Draw(itemTexture, keyIndicator, srcRect, keyColor);
        }

        // Virtual methods for item-specific colors
        protected virtual Color GetActiveColor()
        {
            return new Color(255, 255, 255, 200); // Default white
        }

        protected virtual Color GetInactiveColor()
        {
            return new Color(127, 127, 127, 150); // Default gray
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }
    }

    // Double Jump Item - allows player to jump again while falling or at peak of jump
    class DoubleJumpItem : Item
    {
        private bool hasDoubleJumped = false;

        public DoubleJumpItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Double Jump", "Allows a second jump in mid-air", srcRect)
        {
        }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            hasDoubleJumped = false; // Reset double jump state when equipped
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            hasDoubleJumped = false; // Reset state when removed
        }

        protected override Color GetActiveColor()
        {
            return new Color(0, 150, 255, 200); // Bright blue
        }

        protected override Color GetInactiveColor()
        {
            return new Color(0, 75, 127, 150); // Dim blue
        }

        // Call this method from Player's HandleInput when checking for jump
        public bool TryDoubleJump(PolyGone.Entities.Player player, bool spacePressed, bool wasOnGround)
        {
            if (!IsActive) return false;

            // Reset double jump when landing
            if (wasOnGround)
            {
                hasDoubleJumped = false;
            }

            // Allow double jump if: space is pressed, player is falling/stationary, and hasn't double jumped yet
            if (spacePressed && player.ChangeY >= 0 && !hasDoubleJumped && !wasOnGround)
            {
                hasDoubleJumped = true;
                return true;
            }

            return false;
        }
    }

    // Speed Boost Item - increases player movement speed by 1.5x
    class SpeedBoostItem : Item
    {
        private const float SPEED_MULTIPLIER = 1.5f;

        public SpeedBoostItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Speed Boost", "Increases movement speed by 50%", srcRect)
        {
        }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            // Speed boost is handled in Player's movement calculations
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            // Speed boost removal is handled in Player's movement calculations
        }

        protected override Color GetActiveColor()
        {
            return new Color(255, 215, 0, 200); // Bright yellow
        }

        protected override Color GetInactiveColor()
        {
            return new Color(127, 107, 0, 150); // Dim yellow
        }

        public float GetSpeedMultiplier()
        {
            return IsActive ? SPEED_MULTIPLIER : 1.0f;
        }
    }

    // Healing Glow Item - heals player and gives green glowing outline
    class HealingGlowItem : Item
    {
        private const int HEAL_AMOUNT = 50;
        private float glowTime = 0f;
        private bool hasHealed = false;

        public HealingGlowItem(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Healing Glow", "Restores health and grants a protective glow", srcRect)
        {
        }

        public override void Apply(PolyGone.Entities.Player player)
        {
            base.Apply(player);
            if (!hasHealed)
            {
                player.health += HEAL_AMOUNT;
                player.health = Math.Min(player.health, player.maxHealth); // Don't exceed max health
                hasHealed = true;
            }
            glowTime = 0f;
        }

        public override void Remove(PolyGone.Entities.Player player)
        {
            base.Remove(player);
            hasHealed = false;
            glowTime = 0f;
        }

        protected override Color GetActiveColor()
        {
            return new Color(0, 255, 100, 200); // Bright green
        }

        protected override Color GetInactiveColor()
        {
            return new Color(0, 127, 50, 150); // Dim green
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (IsActive)
            {
                glowTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
        }

        // Get the glow color for drawing the player outline
        public Color GetGlowColor()
        {
            if (!IsActive) return Color.Transparent;
            
            // Pulsing green glow
            float intensity = (float)(Math.Sin(glowTime * 6) * 0.3f + 0.7f); // Pulse between 0.4 and 1.0
            return new Color(0, (int)(255 * intensity), 0, (int)(200 * intensity));
        }

        // Check if player should have glow outline
        public bool ShouldGlow()
        {
            return IsActive;
        }
    }
}
