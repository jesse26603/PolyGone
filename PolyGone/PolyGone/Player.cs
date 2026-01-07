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
        private float changeX;
        private float changeY;
        public Player(Texture2D texture, Vector2 position, int[] size, Color color, Dictionary<Vector2, int> collisionMap, Rectangle? srcRect = null) 
            : base(texture, position, size, color, srcRect)
        {
            this.collisionMap = collisionMap;
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
            
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
        {
            // Player-specific draw logic can go here

            base.Draw(spriteBatch, offset);
        }
    }
}