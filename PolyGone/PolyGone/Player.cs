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

        public Player(Texture2D texture, Vector2 position, int[] size, int health, Color color, Rectangle? srcRect, Dictionary<Vector2, int> collisionMap, Blaster blaster)
            : base(texture, position, size, health, color, srcRect, collisionMap)
        {
            this.blaster = blaster;
            this.bullets = new List<Projectile>();
        }

        // Allow dropping through semi-solid platforms when S is pressed
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
                        ffCooldown = 8; // Prevent bouncing back up
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

            // Horizontal movement
            if (keyboardState.IsKeyDown(Keys.A))
            {
                changeX -= 1f;
                changeX = Math.Max(changeX, -5f);
            }
            else if (keyboardState.IsKeyDown(Keys.D))
            {
                changeX += 1f;
                changeX = Math.Min(changeX, 5f);
            }
            else
            {
                if (Math.Abs(changeX) > 0.5f)
                    changeX *= 0.8f;
                else
                    changeX = 0f;
            }

            // Jumping
            if (isOnGround && keyboardState.IsKeyDown(Keys.Space))
            {
                changeY = -16f;
            }
            // Fast-fall
            // if (!isOnGround && keyboardState.IsKeyDown(Keys.S) && changeY >= 0 && ffCooldown <= 0f)
            // {
            //     changeY += 0.3f;
            //     changeY = Math.Max(changeY, 25f);
            // }
            // else
            // {
            //     // Apply gravity
            //     return;
            // }
        }


        private float cooldown = 0f;
        public override void Update(GameTime gameTime)
        {
            HandleInput();
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
                    xSpeed: Math.Cos(blaster.rotation) * 850d,
                    ySpeed: Math.Sin(blaster.rotation) * 850d,
                    srcRect: blaster.srcRect,
                    collisionMap: collisionMap
                ));
                cooldown = 12f;
            }

            foreach (var bullet in bullets.ToList())
            {
                bullet.Lifetime -= 1f;
                if (bullet.Lifetime <= 0f)
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
