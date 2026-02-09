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
            // Don't apply gravity if coyote time is active (prevents falling off ledges during grace period)
            if (coyoteTime <= 0f)
            {
                // Apply gravity
                changeY += 0.7f;
                changeY = Math.Min(changeY, 14f);
            }

            // Vertical collision and movement
            isOnGround = false;
            float nextY = position.Y + (changeY * deltaTime);
            Rectangle nextRectY = new Rectangle((int)position.X, (int)nextY, size[0], size[1]);
            var verticalCollisions = GetIntersectingTiles(nextRectY);
            if (verticalCollisions.Count > 0)
            {
                verticalCollisions = verticalCollisions
                    .OrderBy(c => Math.Abs(changeY > 0 ? c.Item1.Top - (position.Y + size[1]) : c.Item1.Bottom - position.Y))
                    .ThenByDescending(c => c.Item2 == CollisionType.Solid ? 1 : 0)
                    .ToList();
                HandleVerticalCollision(ref isOnGround, ref changeY, verticalCollisions.Take(1).ToList());
            }
            else
            {
                position.Y = nextY;
                
                // Gap centering for vertical movement: center horizontally when moving through a 1-tile wide gap
                if (collisionMap != null && Math.Abs(changeY) > 0.1f)
                {
                    // Get the current tile column the player is in
                    int playerTileX = (int)((position.X + size[0] / 2) / 64);
                    int playerTileY = (int)((position.Y + size[1] / 2) / 64);
                    
                    // Check if there are solid walls directly to the left and right
                    var keyLeft = new Vector2(playerTileX - 1, playerTileY);
                    var keyRight = new Vector2(playerTileX + 1, playerTileY);
                    
                    bool wallLeft = collisionMap.ContainsKey(keyLeft) && collisionMap[keyLeft] != -1;
                    bool wallRight = collisionMap.ContainsKey(keyRight) && collisionMap[keyRight] != -1;
                    
                    // If surrounded by walls on left and right (in a 1-tile wide gap), center strongly
                    if (wallLeft && wallRight)
                    {
                        float tileCenter = playerTileX * 64 + 32;
                        float playerCenter = position.X + size[0] / 2;
                        float offset = tileCenter - playerCenter;
                        
                        // Very strong and immediate centering (up to 20 pixels per frame)
                        if (Math.Abs(offset) > 0.1f)
                        {
                            float nudge = Math.Sign(offset) * 20f;
                            if (Math.Abs(nudge) > Math.Abs(offset))
                                nudge = offset;
                            position.X += nudge;
                        }
                    }
                }
            }

            // Horizontal collision and movement
            float nextX = position.X + (changeX * deltaTime);
            Rectangle nextRectX = new Rectangle((int)nextX, (int)position.Y, size[0], size[1]);
            var horizontalCollisions = GetIntersectingTiles(nextRectX);
            if (horizontalCollisions.Count > 0)
            {
                horizontalCollisions = horizontalCollisions
                    .OrderBy(c => Math.Abs(changeX > 0 ? c.Item1.Left - (position.X + size[0]) : c.Item1.Right - position.X))
                    .ToList();
                HandleHorizontalCollision(ref changeX, horizontalCollisions.Take(1).ToList());
            }
            else
            {
                position.X = nextX;
                
                // Gap funneling: when walking over a 1-tile gap and pressing S, funnel down through it
                if (isOnGround && keyboardState.IsKeyDown(Keys.S) && collisionMap != null)
                {
                    int playerTileX = (int)((position.X + size[0] / 2) / 64);
                    int playerTileY = (int)((position.Y + size[1] / 2) / 64);
                    
                    // Check if there's a semi-solid directly below
                    var keyBelow = new Vector2(playerTileX, playerTileY + 1);
                    bool semiSolidBelow = collisionMap.ContainsKey(keyBelow) && collisionMap[keyBelow] != -1;
                    
                    if (semiSolidBelow)
                    {
                        // Check if there are walls on left and right (1-tile gap scenario)
                        var keyLeft = new Vector2(playerTileX - 1, playerTileY);
                        var keyRight = new Vector2(playerTileX + 1, playerTileY);
                        
                        bool wallLeft = collisionMap.ContainsKey(keyLeft) && collisionMap[keyLeft] != -1;
                        bool wallRight = collisionMap.ContainsKey(keyRight) && collisionMap[keyRight] != -1;
                        
                        if (wallLeft && wallRight)
                        {
                            // Center horizontally in the gap
                            float tileCenter = playerTileX * 64 + 32;
                            float playerCenter = position.X + size[0] / 2;
                            float offset = tileCenter - playerCenter;
                            
                            if (Math.Abs(offset) > 0.1f)
                            {
                                float nudge = Math.Sign(offset) * 15f;
                                if (Math.Abs(nudge) > Math.Abs(offset))
                                    nudge = offset;
                                position.X += nudge;
                            }
                        }
                    }
                }
                
                // Gap centering: when moving through a 1-tile vertical gap, center horizontally
                if (collisionMap != null)
                {
                    // Check for walls above and below to detect a 1-tile gap
                    int tileAbove = (int)((position.Y - 1) / 64);
                    int tileBelow = (int)((position.Y + size[1] + 1) / 64);
                    int currentTileX = (int)((position.X + size[0] / 2) / 64);
                    
                    var keyAbove = new Vector2(currentTileX, tileAbove);
                    var keyBelow = new Vector2(currentTileX, tileBelow);
                    
                    bool wallAbove = collisionMap.ContainsKey(keyAbove) && collisionMap[keyAbove] != -1;
                    bool wallBelow = collisionMap.ContainsKey(keyBelow) && collisionMap[keyBelow] != -1;
                    
                    // If there are walls above and below (1-tile gap), center horizontally in the tile
                    if (wallAbove && wallBelow)
                    {
                        float tileCenter = currentTileX * 64 + 32;
                        float playerCenter = position.X + size[0] / 2;
                        float offset = tileCenter - playerCenter;
                        
                        // Use same nudge logic as horizontal funneling
                        if (Math.Abs(offset) > 0.1f)
                        {
                            float nudge = Math.Sign(offset) * 15f;
                            if (Math.Abs(nudge) > Math.Abs(offset))
                                nudge = offset;
                            position.X += nudge;
                        }
                    }
                }
            }

            // Apply friction to horizontal movement
            if (Math.Abs(changeX) > 0.5f)
                changeX *= friction;
            else
                changeX = 0f;
        }

        public override void Update(GameTime gameTime)
        {
            HandleInput();
            
            // Update coyote time: set to 6 frames when grounded, otherwise decrease
            if (isOnGround)
            {
                coyoteTime = 6f; // 0.1 seconds at 60fps
            }
            else
            {
                coyoteTime = Math.Max(0f, coyoteTime - 1f);
            }
            
            base.Update(gameTime);

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
