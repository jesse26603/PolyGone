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

namespace PolyGone.Entities
{

    internal class Player : Entity
    {
        // Constants for gap centering nudge strengths
        private const float VERTICAL_GAP_NUDGE_STRENGTH = 20f; // Strong nudge for vertical movement through gaps
        private const float HORIZONTAL_GAP_NUDGE_STRENGTH = 15f; // Medium nudge for horizontal gap funneling
        
        private KeyboardState keyboardState;
        private KeyboardState previousKeyboardState;
        private readonly List<Item> weaponInventory = new List<Item>(); // Weapons (blasters/shotguns)
        private readonly List<Item> itemInventory = new List<Item>(); // Regular items (double jump, speed boost, etc.)
        private int currentWeaponIndex = 0; // Index of currently equipped weapon
        public readonly List<Projectile> bullets = new List<Projectile>(); // Shared projectile list for all weapons
        private float ffCooldown = 0f;
        private float coyoteTime = 0f; // Allows jumping shortly after leaving a platform
        
        // Public property to access changeY for items
        public float ChangeY => changeY;
        
        public Player(Texture2D texture, Vector2 position, int[] size, int health, Color color, Rectangle? srcRect, Dictionary<Vector2, int> collisionMap, Texture2D blasterTexture, int[]? visualSize = null)
            : base(texture, position, size, health, color, srcRect, collisionMap, visualSize)
        {
            // Create both weapon types and add to weapon inventory
            blaster = new Blaster(blasterTexture, Vector2.Zero, new int[] { 32, 32 }, Color.White, collisionMap, bullets, srcRect);
            var shotgun = new Shotgun(blasterTexture, Vector2.Zero, new int[] { 32, 32 }, Color.Red, collisionMap, bullets, srcRect);
            weaponInventory.Add(blaster);
            weaponInventory.Add(shotgun);
            
            // Create sample items and add to item inventory
            var doubleJump = new DoubleJumpItem(texture, Vector2.Zero, new int[] { 32, 32 }, Color.Blue, srcRect);
            var speedBoost = new SpeedBoostItem(texture, Vector2.Zero, new int[] { 32, 32 }, Color.Yellow, srcRect);
            var healingGlow = new HealingGlowItem(texture, Vector2.Zero, new int[] { 32, 32 }, Color.Green, srcRect);
            itemInventory.Add(doubleJump);
            itemInventory.Add(speedBoost);
            itemInventory.Add(healingGlow);
            
            this.friction = 0.8f; // Player has more friction for tighter control
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
                    if (keyboardState.IsKeyDown(Keys.S))
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
            previousKeyboardState = keyboardState;
            keyboardState = Keyboard.GetState();
            int moveDirection = 0;

            // Weapon switching
            if (keyboardState.IsKeyDown(Keys.D1) && !previousKeyboardState.IsKeyDown(Keys.D1))
            {
                currentWeaponIndex = 0; // Switch to blaster
            }
            else if (keyboardState.IsKeyDown(Keys.D2) && !previousKeyboardState.IsKeyDown(Keys.D2))
            {
                currentWeaponIndex = 1; // Switch to shotgun
            }

            // Item management (3, 4, 5 keys for items 0, 1, 2)
            if (keyboardState.IsKeyDown(Keys.D3) && !previousKeyboardState.IsKeyDown(Keys.D3))
            {
                ToggleItem(0);
            }
            else if (keyboardState.IsKeyDown(Keys.D4) && !previousKeyboardState.IsKeyDown(Keys.D4))
            {
                ToggleItem(1);
            }
            else if (keyboardState.IsKeyDown(Keys.D5) && !previousKeyboardState.IsKeyDown(Keys.D5))
            {
                ToggleItem(2);
            }

            // Horizontal movement with speed boost consideration
            if (keyboardState.IsKeyDown(Keys.A) && !keyboardState.IsKeyDown(Keys.D)) moveDirection = -1;
            else if (keyboardState.IsKeyDown(Keys.D) && !keyboardState.IsKeyDown(Keys.A)) moveDirection = 1; 
            
            // Apply acceleration with speed boost
            float speedMultiplier = GetSpeedBoostMultiplier();
            changeX += moveDirection * 1f * speedMultiplier;
            changeX = MathHelper.Clamp(changeX, -5f * speedMultiplier, 5f * speedMultiplier);

            // Jumping with coyote time and double jump
            bool spacePressed = keyboardState.IsKeyDown(Keys.Space);
            bool wasOnGroundLastFrame = isOnGround;
            
            if ((isOnGround || coyoteTime > 0f) && spacePressed)
            {
                changeY = -16f;
                coyoteTime = 0f; // Reset coyote time after jumping
            }
            else
            {
                // Check for double jump
                var doubleJumpItem = GetActiveDoubleJumpItem();
                if (doubleJumpItem != null && doubleJumpItem.TryDoubleJump(this, spacePressed, wasOnGroundLastFrame))
                {
                    changeY = -16f; // Same jump strength for double jump
                }
            }
        }

        protected override void OnEntityCollision(Entity other)
        {
            switch (other)
            {
                default:
                    break;
                case Projectile projectile:
                    // Only take damage from enemy projectiles
                    if (projectile.owner == Owner.Enemy && invincibilityFrames <= 0f)
                    {
                        health -= projectile.damage;
                        invincibilityFrames = 60f;
                    }
                    break;
                case Enemy:
                    // Only take damage if not invincible
                    if (invincibilityFrames <= 0f)
                    {
                        // Take 40 damage
                        health -= 40;
                        
                        // Calculate knockback direction (away from enemy)
                        float knockbackX = position.X < other.position.X ? -10f : 10f;
                        float knockbackY = -20f;
                        
                        // Apply knockback
                        changeX = knockbackX;
                        changeY = knockbackY / 2; // Reduced vertical knockback for better feel
                        
                        // Set invincibility frames (roughly 1 second at 60fps)
                        invincibilityFrames = 60f;
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
            if (Math.Abs(changeY) > 0.1f && collisionMap != null)
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
            if (isOnGround && keyboardState.IsKeyDown(Keys.S) && collisionMap != null)
            {
                int playerTileX = (int)((position.X + size[0] / 2f) / TILE_SIZE);
                int playerTileY = (int)((position.Y + size[1] / 2f) / TILE_SIZE);
                
                // Check if there's a semi-solid platform directly below
                var keyBelow = new Vector2(playerTileX, playerTileY + 1);
                bool semiSolidBelow = collisionMap.TryGetValue(keyBelow, out int belowTileId) &&
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
            float tileCenter = tileX * TILE_SIZE + TILE_HALF_SIZE;
            float playerCenter = position.X + size[0] / 2f;
            float offset = tileCenter - playerCenter;
            
            if (Math.Abs(offset) > 0.1f)
            {
                float nudge = Math.Sign(offset) * nudgeStrength;
                if (Math.Abs(nudge) > Math.Abs(offset))
                    nudge = offset;
                position.X += nudge;
            }
        }

        // Item management methods
        private void ToggleItem(int itemIndex)
        {
            if (itemIndex >= 0 && itemIndex < itemInventory.Count)
            {
                var item = itemInventory[itemIndex];
                if (item.IsActive)
                {
                    item.Remove(this);
                }
                else
                {
                    item.Apply(this);
                }
            }
        }

        private float GetSpeedBoostMultiplier()
        {
            var speedBoostItem = itemInventory.OfType<SpeedBoostItem>().FirstOrDefault();
            return speedBoostItem?.GetSpeedMultiplier() ?? 1.0f;
        }

        private DoubleJumpItem GetActiveDoubleJumpItem()
        {
            return itemInventory.OfType<DoubleJumpItem>().FirstOrDefault(item => item.IsActive);
        }

        private HealingGlowItem GetActiveHealingGlowItem()
        {
            return itemInventory.OfType<HealingGlowItem>().FirstOrDefault(item => item.IsActive);
        }

        // Helper method to get the currently equipped blaster from weapon inventory
        public Blaster GetBlaster()
        {
            Blaster firstBlaster = null;
            int blasterIndex = 0;

            foreach (var blasterWeapon in weaponInventory.OfType<Blaster>())
            {
                if (firstBlaster == null)
                {
                    firstBlaster = blasterWeapon;
                }

                if (blasterIndex == currentWeaponIndex)
                {
                    return blasterWeapon;
                }

                blasterIndex++;
            }

            return firstBlaster;
        }

        // Public property to access current blaster for backward compatibility
        public Blaster blaster => GetBlaster();

        public override void Update(GameTime gameTime)
        {
            Update(gameTime, Vector2.Zero);
        }

        public void Update(GameTime gameTime, Vector2 cameraOffset)
        {
            HandleInput();
            
            base.Update(gameTime);
            
            // Update coyote time after physics update to use current frame's ground state
            coyoteTime = isOnGround
                ? 6f // 0.1 seconds at 60fps
                : Math.Max(0f, coyoteTime - 1f);

            // Update only the currently equipped weapon
            var currentBlaster = GetBlaster();
            if (currentBlaster != null)
            {
                currentBlaster.Follow(new Rectangle((int)position.X, (int)position.Y, size[0], size[1]), cameraOffset);
                currentBlaster.Use();
                currentBlaster.Update(gameTime);
            }
            
            // Update all bullets (shared across all weapons) - iterate backwards for safe removal
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var bullet = bullets[i];
                bullet.lifetime -= 1f;
                if (bullet.lifetime <= 0f)
                {
                    bullets.RemoveAt(i);
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
            }
            
            ffCooldown = Math.Max(0f, ffCooldown - 1f);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
        {
            // Check if we should draw with healing glow
            var healingGlow = GetActiveHealingGlowItem();
            bool shouldGlow = healingGlow?.ShouldGlow() ?? false;
            
            if (shouldGlow)
            {
                // Draw glow outline first (behind the player) - 68x68 total size
                Color glowColor = healingGlow.GetGlowColor();
                for (int x = -4; x <= 4; x++)
                {
                    for (int y = -4; y <= 4; y++)
                    {
                        if (x == 0 && y == 0) continue; // Skip center
                        Vector2 glowOffset = new Vector2(x, y);
                        spriteBatch.Draw(texture, position - offset + hitboxOffset + glowOffset, srcRect, glowColor);
                    }
                }
            }
            
            base.Draw(spriteBatch, offset);
            
            // Draw only the currently equipped weapon
            var currentBlaster = GetBlaster();
            currentBlaster?.Draw(spriteBatch, offset);
            
            // Draw all bullets (shared across all weapons)
            foreach (var bullet in bullets)
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
                Vector2 indicatorPos = indicatorStart + new Vector2(i * spacing, 0);
                item.DrawIndicator(spriteBatch, indicatorPos, itemTexture, itemSrcRect, i);
            }
        }
    }
}
