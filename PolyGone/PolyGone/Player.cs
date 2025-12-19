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
        public Player(Texture2D texture, Vector2 position, int[] size, Color color, Dictionary<Vector2, int> collisionMap, Rectangle? srcRect = null) : base(texture, position, size, color, srcRect)
        {
            this.collisionMap = collisionMap;
        }

        private List<Rectangle>[] GetIntersectingTiles(Rectangle target)
        {
            List<Rectangle>[] intersectingTiles = new List<Rectangle>[2];
            intersectingTiles[0] = new List<Rectangle>(); // Horizontal collisions
            intersectingTiles[1] = new List<Rectangle>(); // Vertical collisions
            foreach (var tile in collisionMap)
            {
                if (tile.Value == -1) continue; // Skip non-collidable tiles

                Rectangle tileRect = new Rectangle((int)tile.Key.X * 64, (int)tile.Key.Y * 64, 64, 64);

                if (target.Intersects(tileRect))
                {
                    // Determine if the collision is primarily horizontal or vertical
                    float overlapX = Math.Min(target.Right, tileRect.Right) - Math.Max(target.Left, tileRect.Left);
                    float overlapY = Math.Min(target.Bottom, tileRect.Bottom) - Math.Max(target.Top, tileRect.Top);

                    if (overlapX < overlapY)
                    {
                        intersectingTiles[0].Add(tileRect); // Horizontal collision
                    }
                    else
                    {
                        intersectingTiles[1].Add(tileRect); // Vertical collision
                    }
                }
            }
            return intersectingTiles;
        }

        // Handle player movement and collisions
        private void Movement()
        {
            keyboardState = Keyboard.GetState();

            // Handle horizontal input
            if (keyboardState.IsKeyDown(Keys.A))
            {
                changeX -= 1f;
                if (changeX < -5f) changeX = -5f;
            }
            else if (keyboardState.IsKeyDown(Keys.D))
            {
                changeX += 1f;
                if (changeX > 5f) changeX = 5f;
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
            if (changeY > 10f) changeY = 10f;

            // Horizontal collision and movement
            bool horizontalCollision = false;
            float nextX = position.X + changeX;
            
            Rectangle nextRect = new Rectangle((int)nextX, (int)position.Y, size[0], size[1]);
            List<Rectangle>[] intersectingTiles = GetIntersectingTiles(nextRect);
            
            if (intersectingTiles[0].Count > 0)
            {
                horizontalCollision = true;
                changeX = 0f;
            }
            
            if (!horizontalCollision)
            {
                position.X = nextX;
            }

            // Vertical collision and movement
            bool verticalCollision = false;
            bool isOnGround = false;
            float nextY = position.Y + changeY;
            
            nextRect = new Rectangle((int)position.X, (int)nextY, size[0], size[1]);
            intersectingTiles = GetIntersectingTiles(nextRect);
            
            if (intersectingTiles[1].Count > 0)
            {
                verticalCollision = true;
                
                foreach (Rectangle tileRect in intersectingTiles[1])
                {
                    if (changeY > 0 && (int)position.Y + size[1] <= tileRect.Top + 5)
                    {
                        // Landing on top
                        isOnGround = true;
                        position.Y = tileRect.Top - size[1];
                    }
                    else if (changeY < 0)
                    {
                        // Hit ceiling
                        position.Y = tileRect.Bottom;
                    }
                }
                
                changeY = 0f;
            }
            
            if (!verticalCollision)
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
            Movement();
            
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
        {
            // Player-specific draw logic can go here

            base.Draw(spriteBatch, offset);
        }
    }
}
