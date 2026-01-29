using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Collections.Generic;

namespace PolyGone
{

    internal class Player : Entity
    {
        private KeyboardState keyboardState;
        private MouseState mouseState;
        public Blaster blaster { get; private set; }
        public readonly List<Projectile> bullets;

        public Player(Texture2D texture, Vector2 position, int[] size, int health, Color color, Rectangle? srcRect, Dictionary<Vector2, int> collisionMap, Blaster blaster)
            : base(texture, position, size, health, color, srcRect, collisionMap)
        {
            this.blaster = blaster;
            this.bullets = new List<Projectile>();
        }

        // Allow dropping through semi-solid platforms when S is pressed
        protected override void HandleVerticalCollision(ref bool isOnGround, ref float changeY, List<(Rectangle, CollisionType)> collisions)
        {
            var (tileRect, colType) = collisions[0];
            switch (colType)
            {
                default:
                case CollisionType.Solid:
                case CollisionType.Rough:
                case CollisionType.Slippery:
                    if (changeY > 0)
                    {
                        position.Y = tileRect.Top - size[1];
                        isOnGround = true;
                    }
                    else if (changeY < 0)
                    {
                        position.Y = tileRect.Bottom;
                    }
                    changeY = 0;
                    break;
                case CollisionType.SemiSolid:
                    if (keyboardState.IsKeyDown(Keys.S))
                    {
                        // Drop through platform
                        position.Y += changeY;
                    }
                    else if (changeY > 0 && (position.Y + size[1]) <= tileRect.Top + 10)
                    {
                        position.Y = tileRect.Top - size[1];
                        changeY = 0;
                        isOnGround = true;
                    }
                    else
                    {
                        position.Y += changeY;
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
                    xSpeed: (float)(Math.Cos(blaster.rotation) * 750f),
                    ySpeed: (float)(Math.Sin(blaster.rotation) * 750f),
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
