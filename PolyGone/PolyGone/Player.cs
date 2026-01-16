using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PolyGone
{
    internal class Player : Entity
    {
        private readonly Dictionary<Vector2, int> collisionMap;
        private KeyboardState keyboardState;
        private MouseState mouseState;
        public Blaster blaster { get; private set; }
        public readonly List<Bullet> bullets;

        public Player(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect, Dictionary<Vector2, int> collisionMap, Blaster blaster)
            : base(texture, position, size, color, srcRect, collisionMap)
        {
            this.collisionMap = collisionMap;
            this.blaster = blaster;
            this.bullets = new List<Bullet>();
        }

        public override void HandleVerticalCollision(ref bool isOnGround, ref float changeY, List<(Rectangle, CollisionType)> collisions)
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
                        // Falling down - land on top of tile
                        position.Y = tileRect.Top - size[1];
                        isOnGround = true;
                    }
                    else if (changeY < 0)
                    {
                        // Moving up - hit ceiling
                        position.Y = tileRect.Bottom;
                    }
                    changeY = 0;
                    break;
                case CollisionType.SemiSolid:
                    if (keyboardState.IsKeyDown(Keys.S))
                    {
                        // Drop down through platform
                        position.Y += changeY;
                    }
                    else if (changeY > 0 && (position.Y + size[1]) <= tileRect.Top + 10)
                    {
                        // Falling down - land on top of tile
                        position.Y = tileRect.Top - size[1];
                        changeY = 0;
                        isOnGround = true;
                    }
                    else
                    {
                        // Moving up - ignore platform
                        position.Y += changeY;
                    }
                    break;
            }
        }

        // Handle player movement based on input
        protected void Movement(float deltaTime)
        {
            keyboardState = Keyboard.GetState();

            // Handle horizontal input
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
                // Apply friction only when no input
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
            float deltaTime = (float)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 60f, 3); // Assuming 60 FPS standard
            Movement(deltaTime);
            Physics(deltaTime);

            // Handle shooting (To be implemented)
            mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed && cooldown <= 0f)
            {
                bullets.Add(new Bullet(
                    texture: texture,
                    position: new Vector2(blaster.position.X + blaster.size[0] / 2 - 5, blaster.position.Y + blaster.size[1] / 2 - 5),
                    size: new int[2] { 10, 10 },
                    lifetime: 90f,
                    color: Color.White,
                    xSpeed: (float)(Math.Cos(blaster.rotation) * 10f),
                    ySpeed: (float)(Math.Sin(blaster.rotation) * 10f),
                    srcRect: blaster.srcRect
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
            base.Update(gameTime);
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
