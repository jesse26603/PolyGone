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
    internal class Player : Sprite
    {
        private Dictionary<Vector2, int> collisionMap;
        private KeyboardState keyboardState;
        private MouseState mouseState;
        private float changeX;
        private float changeY;
        public Blaster blaster;
        public List<Bullet> bullets;

        public Player(Texture2D texture, Vector2 position, int[] size, Color color, Dictionary<Vector2, int> collisionMap, Blaster blaster, Rectangle? srcRect = null)
            : base(texture, position, size, color, srcRect)
        {
            this.collisionMap = collisionMap;
            this.blaster = blaster;
            this.bullets = new List<Bullet>();
        }

        private List<Rectangle> GetIntersectingTiles(Rectangle target)
        {
            List<Rectangle> intersectingTiles = new List<Rectangle>();
            foreach (var tile in collisionMap)
            {
                if (tile.Value == -1) continue; // Skip non-collidable tiles

                Rectangle tileRect = new Rectangle((int)tile.Key.X * 64, (int)tile.Key.Y * 64, 64, 64);

                if (target.Intersects(tileRect))
                {
                    intersectingTiles.Add(tileRect);
                }
            }
            return intersectingTiles;
        }

        // Handle player movement and collisions
        private void Movement(float deltaTime)
        {
            keyboardState = Keyboard.GetState();
            mouseState = Mouse.GetState();



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

            // Apply gravity
            changeY += 0.5f;
            changeY = Math.Min(changeY, 10f);

            // Horizontal collision and movement
            float nextX = position.X + (changeX * deltaTime);
            Rectangle nextRectX = new Rectangle((int)nextX, (int)position.Y, size[0], size[1]);
            List<Rectangle> horizontalCollisions = GetIntersectingTiles(nextRectX);

            if (horizontalCollisions.Count > 0)
            {
                // Resolve horizontal collision
                foreach (Rectangle tileRect in horizontalCollisions)
                {
                    if (changeX > 0)
                    {
                        // Moving right - push to left edge of tile
                        position.X = tileRect.Left - size[0];
                    }
                    else if (changeX < 0)
                    {
                        // Moving left - push to right edge of tile
                        position.X = tileRect.Right;
                    }
                }
                changeX = 0f;
            }
            else
            {
                position.X = nextX;
            }

            // Vertical collision and movement
            bool isOnGround = false;
            float nextY = position.Y + (changeY * deltaTime);
            Rectangle nextRectY = new Rectangle((int)position.X, (int)nextY, size[0], size[1]);
            List<Rectangle> verticalCollisions = GetIntersectingTiles(nextRectY);

            if (verticalCollisions.Count > 0)
            {
                foreach (Rectangle tileRect in verticalCollisions)
                {
                    if (changeY > 0)
                    {
                        // Falling down - land on top of tile
                        isOnGround = true;
                        position.Y = tileRect.Top - size[1];
                    }
                    else if (changeY < 0)
                    {
                        // Moving up - hit ceiling
                        position.Y = tileRect.Bottom;
                    }
                }
                changeY = 0f;
            }
            else
            {
                position.Y = nextY;
            }

            // Round positions to prevent sub-pixel jittering
            position.X = (float)Math.Round(position.X);
            position.Y = (float)Math.Round(position.Y);

            // Jumping
            if (isOnGround && keyboardState.IsKeyDown(Keys.Space))
            {
                changeY = -12f;
            }
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 60f, 3); // Assuming 60 FPS standard
            Movement(deltaTime);
            // Handle shooting (Don't worky yet but it okay)
            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                bullets.Add(new Bullet(
                texture: texture,
                position: new Vector2(blaster.position.X + blaster.size[0] / 2, blaster.position.Y + blaster.size[1] / 2),
                size: [10, 10],
                Lifetime: 90 * deltaTime,
                color: Color.White,
                xSpeed: (float)(Math.Cos(blaster.rotation) * 10f),
                ySpeed: (float)(Math.Sin(blaster.rotation) * 10f)
                ));

                //if (lifetime == 0f { bullets.Remove(bullet); })
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
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
        {
            blaster.Draw(spriteBatch, offset);
            base.Draw(spriteBatch, offset);
            foreach (var bullet in bullets)
            {
                bullet.Draw(spriteBatch, offset);
            }
        }
    }
}