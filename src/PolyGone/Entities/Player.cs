using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using PolyGone.Weapons;
using PolyGone.Items;
using PolyGone.Core;

namespace PolyGone.Entities
{

    public class Player : Entity
    {
        // Constants for gap centering nudge strengths
        private const float VERTICAL_GAP_NUDGE_STRENGTH = 20f; // Strong nudge for vertical movement through gaps
        private const float HORIZONTAL_GAP_NUDGE_STRENGTH = 15f; // Medium nudge for horizontal gap funneling

        private AudioManager audioManager;
        private Item? currentWeapon; // Single selected weapon
        private readonly List<Item> itemInventory = new List<Item>(); // Pre-selected items (max 2)
        public readonly List<Projectile> Bullets = new List<Projectile>(); // Shared projectile list for all weapons
        private float ffCooldown = 0f;
        private float coyoteTime = 0f; // Allows jumping shortly after leaving a platform

        // Public property to access changeY for items
        public new float ChangeY { get => base.ChangeY; set => base.ChangeY = value; }

        // Public property to access current weapon's cooldown for HUD
        public float Cooldown => GetBlaster()?.Cooldown ?? 0f;

        // Public property to control gravity (used by LowGravityItem)
        public new float GravityScale { get => base.GravityScale; set => base.GravityScale = value; }

        // Jump velocity — scaled by LowGravityItem to keep peak height constant
        public float JumpStrength { get; set; } = -16.75f;

        public Player(
            Texture2D texture,
            Vector2 position,
            int[] size,
            int health,
            Color color,
            Rectangle? srcRect,
            Dictionary<Vector2, int> collisionMap,
            Texture2D blasterTexture,
            List<ItemType> selectedItems,
            List<BlasterAttachmentType> selectedAttachments,
            AudioManager audioManager,
            int[]? visualSize = null
        )
            : base(texture, position, audioManager, size, health, color, srcRect, collisionMap, visualSize)
        {
            // Always use the Blaster as the base weapon
            currentWeapon = new Blaster(blasterTexture, Vector2.Zero, audioManager, new int[] { 32, 32 }, Color.White, collisionMap, Bullets, srcRect);

            // Apply blaster attachments to the freshly created blaster
            foreach (var attachmentType in selectedAttachments)
            {
                Item? attachment = null;
                switch (attachmentType)
                {
                    case BlasterAttachmentType.MultiShot:
                        attachment = new PolyGone.Items.MultiShotItem(blasterTexture, Vector2.Zero, new int[] { 32, 32 }, Color.Red, srcRect);
                        break;
                    case BlasterAttachmentType.RapidFire:
                        attachment = new PolyGone.Items.RapidFireItem(blasterTexture, Vector2.Zero, new int[] { 32, 32 }, Color.Orange, srcRect);
                        break;
                    case BlasterAttachmentType.Piercing:
                        attachment = new PolyGone.Items.PiercingAttachment(blasterTexture, Vector2.Zero, new int[] { 32, 32 }, new Color(140, 0, 200), srcRect);
                        break;
                    case BlasterAttachmentType.DamageBoost:
                        attachment = new PolyGone.Items.DamageBoostAttachment(blasterTexture, Vector2.Zero, new int[] { 32, 32 }, new Color(255, 60, 0), srcRect);
                        break;
#if DEBUG
                    case BlasterAttachmentType.DevBlaster:
                        // DEV: Apply all attachment effects at once
                        new PolyGone.Items.MultiShotItem(blasterTexture, Vector2.Zero, new int[] { 32, 32 }, Color.Red, srcRect).Apply(this);
                        new PolyGone.Items.RapidFireItem(blasterTexture, Vector2.Zero, new int[] { 32, 32 }, Color.Orange, srcRect).Apply(this);
                        new PolyGone.Items.PiercingAttachment(blasterTexture, Vector2.Zero, new int[] { 32, 32 }, new Color(140, 0, 200), srcRect).Apply(this);
                        new PolyGone.Items.DamageBoostAttachment(blasterTexture, Vector2.Zero, new int[] { 32, 32 }, new Color(255, 60, 0), srcRect).Apply(this);
                        break;
#endif
                }
                if (attachment != null)
                {
                    itemInventory.Add(attachment);
                    attachment.Apply(this);
                }
            }

            // Create and activate the selected player items
            foreach (var itemType in selectedItems)
            {
                Item? item = null;
                switch (itemType)
                {
                    case ItemType.DoubleJump:
                        item = new DoubleJumpItem(texture, Vector2.Zero, new int[] { 32, 32 }, Color.Blue, srcRect);
                        break;
                    case ItemType.SpeedBoost:
                        item = new SpeedBoostItem(texture, Vector2.Zero, new int[] { 32, 32 }, Color.Yellow, srcRect);
                        break;
                    case ItemType.HealingGlow:
                        item = new HealingGlowItem(texture, Vector2.Zero, new int[] { 32, 32 }, Color.Green, srcRect);
                        break;
                    case ItemType.LowGravity:
                        item = new LowGravityItem(texture, Vector2.Zero, new int[] { 32, 32 }, Color.Purple, srcRect);
                        break;
                    case ItemType.IronWill:
                        item = new IronWillItem(texture, Vector2.Zero, new int[] { 32, 32 }, Color.Gold, srcRect);
                        break;
#if DEBUG
                    case ItemType.DevMode:
                        item = new PolyGone.Items.DevModeItem(texture, Vector2.Zero, new int[] { 32, 32 }, Color.Magenta, srcRect);
                        break;
#endif
                }

                if (item != null)
                {
                    itemInventory.Add(item);
                    item.Apply(this); // Automatically activate all selected items
                }
            }

            this.Friction = 0.8f; // Player has more friction for tighter control
            this.audioManager = audioManager; // Store reference to AudioManager for playing audio
        }

        protected override void HandleVerticalCollision(ref bool onGround, ref float deltaY, List<(Rectangle, CollisionType)> collisions)
        {
            var (tileRect, colType) = collisions[0];
            switch (colType)
            {
                default:
                case CollisionType.Solid:
                case CollisionType.Rough:
                case CollisionType.Slippery:
                    if (deltaY > 0)
                    {
                        position.Y = tileRect.Top - size[1];
                        onGround = true;
                    }
                    else if (deltaY < 0)
                    {
                        position.Y = tileRect.Bottom;
                    }
                    deltaY = 0;
                    break;
                case CollisionType.SemiSolid:
                    if (InputManager.GameDrop())
                    {
                        // Drop through platform
                        position.Y += deltaY;
                        ffCooldown = 4f; // Prevent bouncing back up
                    }
                    else if (deltaY > 0 && (position.Y + size[1]) <= tileRect.Top + 10 && ffCooldown <= 0f)
                    {
                        position.Y = tileRect.Top - size[1];
                        deltaY = 0;
                        onGround = true;
                    }
                    else
                    {
                        position.Y += deltaY;
                    }
                    break;
            }
        }

        // Handle player input and jumping
        private void HandleInput()
        {
            int moveDirection = 0;

            // Horizontal movement with speed boost consideration
            if ((InputManager.GameMoveLeft() && !InputManager.GameMoveRight()))
            {
                moveDirection = -1;
            }
            else if ((InputManager.GameMoveRight() && !InputManager.GameMoveLeft()))
            {
                moveDirection = 1;
            }

            // Apply acceleration with speed boost
            float speedMultiplier = GetSpeedBoostMultiplier();
            ChangeX += moveDirection * 1f * speedMultiplier;
            ChangeX = MathHelper.Clamp(ChangeX, -5f * speedMultiplier, 5f * speedMultiplier);

            // Jumping with coyote time and double jump
            bool JumpTriggered = InputManager.GameJump();
            bool wasOnGroundLastFrame = IsOnGround;

            if ((IsOnGround || coyoteTime > 0f) && JumpTriggered)
            {
                base.ChangeY = JumpStrength;
                audioManager.PlayAudio("jumpSfx", true, "null", false); //Play jump sound effect                 
                coyoteTime = 0f; // Reset coyote time after jumping
                GetActiveDoubleJumpItem()?.Reset(); // Allow double jump in the new air phase
            }
            else
            {
                // Check for double jump
                var doubleJumpItem = GetActiveDoubleJumpItem();
                if (doubleJumpItem != null && doubleJumpItem.TryDoubleJump(this, JumpTriggered, wasOnGroundLastFrame))
                {
                    base.ChangeY = JumpStrength; // Same jump strength for double jump
                    audioManager.PlayAudio("jumpSfx", true, "null", false); //Play jump sound effect
                }
#if DEBUG
                else if (GetDevModeItem()?.IsActive == true && JumpTriggered)
                {
                    base.ChangeY = JumpStrength; // DEV: infinite jumps
                    audioManager.PlayAudio("jumpSfx", true, "null", false); //Play jump sound effect
                }
#endif
            }
        }

        protected override void OnEntityCollision(Entity other)
        {
            switch (other)
            {
                default:
                    break;
                case Projectile projectile:
                    // Only take damage from non-player projectiles
                    if (InvincibilityFrames <= 0f)
                    {
#if DEBUG
                        if (GetDevModeItem()?.IsActive == true)
                            break;

                        audioManager.PlayAudio("collisionSfx", true, "null", false); //Play collision sound effect
#endif
                        Health -= projectile.Damage;
                        if (Health <= 0)
                        {
                            TryAbsorbLethalHit();
                        }

                        // Apply knockback in the direction the projectile was travelling
                        Vector2 projectileVelocity = new Vector2(projectile.XSpeed, projectile.YSpeed);
                        if (projectileVelocity != Vector2.Zero)
                        {
                            projectileVelocity.Normalize();
                            ChangeX = projectileVelocity.X * 10f;
                            base.ChangeY = projectileVelocity.Y * 10f - 5f; // extra upward bias
                        }

                        InvincibilityFrames = 60f;

                        // Despawn the projectile on contact
                        projectile.Lifetime = 0f;
                    }
                    break;
                case BerserkEnemy:
                    // Only take damage if not invincible
                    if (InvincibilityFrames <= 0f)
                    {
#if DEBUG
                        if (GetDevModeItem()?.IsActive == true)
                        {
                            break;
                        }

                        audioManager.PlayAudio("collisionSfx", true, "null", false); //Play collision sound effect
#endif
                        // Take 40 damage
                        Health -= 40;
                        if (Health <= 0)
                        {
                            TryAbsorbLethalHit();
                        }

                        // Calculate knockback direction (away from enemy)
                        float knockbackX = position.X < other.position.X ? -10f : 10f;
                        float knockbackY = -20f;

                        // Apply knockback
                        ChangeX = knockbackX;
                        ChangeY = knockbackY / 2; // Reduced vertical knockback for better feel

                        // Set invincibility frames (roughly 1 second at 60fps)
                        InvincibilityFrames = 60f;
                    }
                    break;
                case Enemy:
                    // Only take damage if not invincible
                    if (InvincibilityFrames <= 0f)
                    {
#if DEBUG
                        if (GetDevModeItem()?.IsActive == true)
                        {
                            break;
                        }

                        audioManager.PlayAudio("collisionSfx", true, "null", false); //Play collision sound effect
#endif
                        // Take 40 damage
                        Health -= 40;
                        if (Health <= 0)
                        {
                            TryAbsorbLethalHit();
                        }

                        // Calculate knockback direction (away from enemy)
                        float knockbackX = position.X < other.position.X ? -10f : 10f;
                        float knockbackY = -20f;

                        // Apply knockback
                        ChangeX = knockbackX;
                        base.ChangeY = knockbackY / 2; // Reduced vertical knockback for better feel

                        // Set invincibility frames (roughly 1 second at 60fps)
                        InvincibilityFrames = 60f;
                    }
                    break;
            }
        }

        protected override void PhysicsUpdate(float deltaTime)
        {
            // Call base physics update for standard collision handling
            base.PhysicsUpdate(deltaTime);
        }

        protected override void OnVerticalMovementComplete(float deltaTime)
        {
            // Player-specific vertical gap centering (stronger than base)
            if (Math.Abs(base.ChangeY) > 0.1f && CollisionMap != null)
            {
                int playerTileX = (int)((position.X + size[0] / 2f) / TILE_SIZE);
                int playerTileY = (int)((position.Y + size[1] / 2f) / TILE_SIZE);

                var keyLeft = new Vector2(playerTileX - 1, playerTileY);
                var keyRight = new Vector2(playerTileX + 1, playerTileY);

                if (IsSolidWall(keyLeft) && IsSolidWall(keyRight))
                {
                    ApplyGapCentering(playerTileX, VERTICAL_GAP_NUDGE_STRENGTH);
                }
            }
        }

        protected override void OnHorizontalMovementComplete(float deltaTime)
        {
            // Gap funneling: when walking over a 1-tile gap and pressing S, funnel down through it
            if ((IsOnGround || coyoteTime > 0f) && InputManager.GameDrop() && CollisionMap != null)
            {
                int playerTileX = (int)((position.X + size[0] / 2f) / TILE_SIZE);
                int playerTileY = (int)((position.Y + size[1] / 2f) / TILE_SIZE);

                // Check if there's a semi-solid platform directly below
                var keyBelow = new Vector2(playerTileX, playerTileY + 1);
                bool semiSolidBelow = CollisionMap.TryGetValue(keyBelow, out int belowTileId) &&
                                      belowTileId != -1 &&
                                      CollisionTypeMapper.GetCollisionType(belowTileId) == CollisionType.SemiSolid;

                if (semiSolidBelow)
                {
                    var keyLeft = new Vector2(playerTileX - 1, playerTileY);
                    var keyRight = new Vector2(playerTileX + 1, playerTileY);

                    if (IsSolidWall(keyLeft) && IsSolidWall(keyRight))
                    {
                        ApplyGapCentering(playerTileX, HORIZONTAL_GAP_NUDGE_STRENGTH);
                    }
                }
            }
        }

        private void ApplyGapCentering(int tileX, float nudgeStrength)
        {
            float tileCenter = (float)tileX * TILE_SIZE + TILE_HALF_SIZE;
            float playerCenter = position.X + size[0] / 2f;
            float offset = tileCenter - playerCenter;

            if (Math.Abs(offset) > 0.1f)
            {
                float nudge = Math.Sign(offset) * nudgeStrength;
                if (Math.Abs(nudge) > Math.Abs(offset))
                {
                    nudge = offset;
                }

                position.X += nudge;
            }
        }

        private float GetSpeedBoostMultiplier()
        {
            var speedBoostItem = itemInventory.OfType<SpeedBoostItem>().FirstOrDefault();
            return speedBoostItem?.GetSpeedMultiplier() ?? 1.0f;
        }

        private DoubleJumpItem? GetActiveDoubleJumpItem()
        {
            return itemInventory.OfType<DoubleJumpItem>().FirstOrDefault(item => item.IsActive);
        }

        private HealingGlowItem? GetActiveHealingGlowItem()
        {
            return itemInventory.OfType<HealingGlowItem>().FirstOrDefault(item => item.IsActive);
        }

        private IronWillItem? GetReadyIronWillItem()
        {
            return itemInventory.OfType<IronWillItem>().FirstOrDefault(item => item.IsReady);
        }

#if DEBUG
        private PolyGone.Items.DevModeItem? GetDevModeItem()
        {
            return itemInventory.OfType<PolyGone.Items.DevModeItem>().FirstOrDefault(i => i.IsActive);
        }
#endif

        /// <summary>If Iron Will is ready, survive the killing blow at 1 HP.</summary>
        private void TryAbsorbLethalHit()
        {
            var ironWill = GetReadyIronWillItem();
            if (ironWill != null && ironWill.TryAbsorbLethalHit())
            {
                Health = 1;
            }
        }

        // Helper method to get the currently equipped blaster/weapon
        public Blaster? GetBlaster()
        {
            return currentWeapon as Blaster;
        }

        // Public property to access current blaster for backward compatibility
        public Blaster? Blaster => GetBlaster();

        // Public method to access item inventory for UI
        public List<Item> GetAllItems()
        {
            return itemInventory;
        }

        public override void Update(GameTime gameTime)
        {
            Update(gameTime, Vector2.Zero);
        }

        public void Update(GameTime gameTime, Vector2 cameraOffset)
        {
            HandleInput();

            base.Update(gameTime);

            // Update coyote time after physics update to use current frame's ground state
            coyoteTime = IsOnGround
                ? 6f // 0.1 seconds at 60fps
                : Math.Max(0f, coyoteTime - 1f);

            // Update only the currently equipped weapon
            var currentBlaster = GetBlaster();
            if (currentBlaster != null)
            {
#if DEBUG
                int bulletCountBefore = Bullets.Count;
#endif
                currentBlaster.Follow(new Rectangle((int)position.X, (int)position.Y, size[0], size[1]), cameraOffset);
                currentBlaster.Use();
                currentBlaster.Update(gameTime);
#if DEBUG
                if (GetDevModeItem()?.IsActive == true)
                {
                    for (int i = bulletCountBefore; i < Bullets.Count; i++)
                    {
                        Bullets[i].IsInstantKill = true;
                    }
                }
#endif
            }

            // Update all bullets (shared across all weapons) - iterate backwards for safe removal
            for (int i = Bullets.Count - 1; i >= 0; i--)
            {
                var bullet = Bullets[i];
                bullet.Lifetime -= 1f;
                if (bullet.Lifetime <= 0f)
                {
                    Bullets.RemoveAt(i);
                }
                else
                {
                    bullet.Update(gameTime);
                }
            }

            // Update active items
            foreach (var item in itemInventory.Where(item => item.IsActive))
            {
                item.Update(gameTime);

                // Apply healing over time from HealingGlowItem
                if (item is HealingGlowItem healingGlow)
                {
                    healingGlow.ApplyHealingOverTime(this, (float)gameTime.ElapsedGameTime.TotalSeconds);
                }
            }

            ffCooldown = Math.Max(0f, ffCooldown - 1f);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
        {
            // Check if we should draw with healing glow
            var healingGlow = GetActiveHealingGlowItem();
            if (healingGlow?.ShouldGlow() == true)
            {
                // Draw glow outline first (behind the player) - 68x68 total size
                Color glowColor = healingGlow.GetGlowColor();
                for (int x = -4; x <= 4; x++)
                {
                    for (int y = -4; y <= 4; y++)
                    {
                        if (x == 0 && y == 0)
                        {
                            continue; // Skip center
                        }

                        Vector2 glowOffset = new Vector2(x, y);
                        spriteBatch.Draw(texture, position - offset + HitboxOffset + glowOffset, srcRect, glowColor);
                    }
                }
            }

            base.Draw(spriteBatch, offset);

            // Draw only the currently equipped weapon
            var currentBlaster = GetBlaster();
            currentBlaster?.Draw(spriteBatch, offset);

            // Draw all bullets (shared across all weapons)
            foreach (var bullet in Bullets)
            {
                bullet.Draw(spriteBatch, offset);
            }
        }

        // Method to draw item indicators at fixed screen positions (called from GameScene)
        public void DrawItemIndicators(SpriteBatch spriteBatch, Texture2D itemTexture, Rectangle itemSrcRect)
        {
            const int MaxDisplayedItems = 3; // Maximum number of items to display

            // Draw item indicators in fixed screen position (not affected by camera)
            Vector2 indicatorStart = new Vector2(20, 20); // Fixed top-left screen position
            int spacing = 25;

            for (int i = 0; i < itemInventory.Count && i < MaxDisplayedItems; i++)
            {
                var item = itemInventory[i];
                Vector2 indicatorPos = indicatorStart + new Vector2(i * (float)spacing, 0);
                item.DrawIndicator(spriteBatch, indicatorPos, itemTexture, itemSrcRect, i);
            }
        }
    }
}
