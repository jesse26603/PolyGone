using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace PolyGone
{

    internal class Player : Entity
    {
        // Constants for gap centering nudge strengths
        private const float VERTICAL_GAP_NUDGE_STRENGTH = 20f; // Strong nudge for vertical movement through gaps
        private const float HORIZONTAL_GAP_NUDGE_STRENGTH = 15f; // Medium nudge for horizontal gap funneling
        
        private KeyboardState keyboardState;
        private MouseState mouseState;
        public Blaster blaster { get; private set; }
        private float ffCooldown = 0f;
        public readonly List<Projectile> bullets;
        private float cooldown;
        private float coyoteTime = 0f; // Allows jumping shortly after leaving a platform
        public Player(Texture2D texture, Vector2 position, int[] size, int health, Color color, Rectangle? srcRect, Dictionary<Vector2, int> collisionMap, Blaster blaster, int[]? visualSize = null)
            : base(texture, position, size, health, color, srcRect, collisionMap, visualSize)
        {
            this.blaster = blaster;
            this.bullets = new List<Projectile>();
            this.cooldown = 0f;
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
            keyboardState = Keyboard.GetState();
            int moveDirection = 0;

            // Horizontal movement
            if (keyboardState.IsKeyDown(Keys.A) && !keyboardState.IsKeyDown(Keys.D)) moveDirection = -1;
            else if (keyboardState.IsKeyDown(Keys.D) && !keyboardState.IsKeyDown(Keys.A)) moveDirection = 1; 
            // Apply acceleration
            changeX += moveDirection * 1f;
            changeX = MathHelper.Clamp(changeX, -5f, 5f);

            // Jumping with coyote time (allows jump for a few frames after leaving platform)
            if ((isOnGround || coyoteTime > 0f) && keyboardState.IsKeyDown(Keys.Space))
            {
                changeY = -16f;
                coyoteTime = 0f; // Reset coyote time after jumping
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

        public override void Update(GameTime gameTime)
        {
            HandleInput();
            
            base.Update(gameTime);
            
            // Update coyote time after physics update to use current frame's ground state
            coyoteTime = isOnGround
                ? 6f // 0.1 seconds at 60fps
                : Math.Max(0f, coyoteTime - 1f);

            // Handle shooting
            mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed && cooldown <= 0f)
            {
                bullets.Add(new Projectile(
                    texture: texture,
                    position: new Vector2(blaster.position.X + blaster.size[0] / 2 - 5, blaster.position.Y + blaster.size[1] / 2 - 5),
                    size: new int[2] { 10, 10 },
                    lifetime: 200f,
                    health: 1,
                    color: Color.White,
                    xSpeed: (float)(Math.Cos(blaster.rotation) * 750f),
                    ySpeed: (float)(Math.Sin(blaster.rotation) * 750f),
                    owner: Owner.Player,
                    srcRect: blaster.srcRect,
                    collisionMap: collisionMap
                ));
                cooldown = 12f; // Cooldown of 0.2 seconds at 60fps
            }

            foreach (var bullet in bullets.ToList())
            {
                bullet.lifetime -= 1f;
                if (bullet.lifetime <= 0f)
                {
                    bullets.Remove(bullet);
                }
                bullet.Update(gameTime);
            }
            cooldown = Math.Max(0f, cooldown - 1f);
            blaster.Update(gameTime);
            ffCooldown = Math.Max(0f, ffCooldown - 1f);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
        {
            base.Draw(spriteBatch, offset);
            blaster.Draw(spriteBatch, offset);
            foreach (var bullet in bullets)
            {
                bullet.Draw(spriteBatch, offset);
            }
        }
    }
}
