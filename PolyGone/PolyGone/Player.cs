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
        private readonly Dictionary<Vector2, int> collisionMap;
        private KeyboardState keyboardState;
        private MouseState mouseState;
        private float changeX;
        private float changeY;
        public Blaster blaster { get; private set; }
        public readonly List<Bullet> bullets;

        public Player(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect, Dictionary<Vector2, int> collisionMap, Blaster blaster)
            : base(texture, position, size, color, srcRect)
        {
            this.collisionMap = collisionMap;
            this.blaster = blaster;
            this.bullets = new List<Bullet>();
        }

        private List<(Rectangle, CollisionType)> GetIntersectingTiles(Rectangle target)
        {
            List<(Rectangle, CollisionType)> intersectingTiles = new List<(Rectangle, CollisionType)>();
            foreach (var tile in collisionMap)
            {
                if (tile.Value == -1) continue; // Skip non-collidable tiles

                Rectangle tileRect = new Rectangle((int)tile.Key.X * 64, (int)tile.Key.Y * 64, 64, 64);
                CollisionType colType = CollisionTypeMapper.GetCollisionType(tile.Value);

                if (target.Intersects(tileRect))
                {
                    intersectingTiles.Add((tileRect, colType));
                }
            }
            return intersectingTiles;
        }

        // Handle vertical collisions
        private void HandleVerticalCollision(ref bool isOnGround, ref float changeY, List<(Rectangle, CollisionType)> collisions)
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
                        // Drop through platform
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

        private void HandleHorizontalCollision(ref float changeX, List<(Rectangle, CollisionType)> collisions)
        {
            var (tileRect, colType) = collisions[0];
            switch (colType)
            {
                default:
                case CollisionType.Solid:
                case CollisionType.Rough:
                case CollisionType.Slippery:
                    if (changeX > 0)
                    {
                        // Moving right - hit left side of tile
                        position.X = tileRect.Left - size[0];
                    }
                    else if (changeX < 0)
                    {
                        // Moving left - hit right side of tile
                        position.X = tileRect.Right;
                    }
                    changeX = 0;
                    break;
                case CollisionType.SemiSolid:
                    // Ignore horizontal collisions with semi-solid tiles
                    position.X += changeX;
                    break;
            }
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
            changeY += 0.7f;
            changeY = Math.Min(changeY, 14f); // Terminal velocity

            // Vertical collision and movement
            bool isOnGround = false;
            float nextY = position.Y + (changeY * deltaTime);
            Rectangle nextRectY = new Rectangle((int)position.X, (int)nextY, size[0], size[1]);
            List<(Rectangle, CollisionType)> verticalCollisions = GetIntersectingTiles(nextRectY);

            if (verticalCollisions.Count > 0)
            {
                // Determine closest collision tile and prioritize handling. Always prioritize solid tiles first.
                verticalCollisions = verticalCollisions
                    .OrderBy(c => Math.Abs(changeY > 0 ? c.Item1.Top - (position.Y + size[1]) : c.Item1.Bottom - position.Y))
                    .ThenByDescending(c => c.Item2 == CollisionType.Solid ? 1 : 0)
                    .ToList();
                HandleVerticalCollision(ref isOnGround, ref changeY, verticalCollisions.Take(1).ToList());
            }
            else
            {
                position.Y = nextY;
            }

            // Horizontal collision and movement
            float nextX = position.X + (changeX * deltaTime);
            Rectangle nextRectX = new Rectangle((int)nextX, (int)position.Y, size[0], size[1]);
            List<(Rectangle, CollisionType)> horizontalCollisions = GetIntersectingTiles(nextRectX);

            if (horizontalCollisions.Count > 0)
            {
                // Determine closest collision tile
                horizontalCollisions = horizontalCollisions
                    .OrderBy(c => Math.Abs(changeX > 0 ? c.Item1.Left - (position.X + size[0]) : c.Item1.Right - position.X))
                    .ToList();
                HandleHorizontalCollision(ref changeX, horizontalCollisions.Take(1).ToList());
            }
            else
            {
                position.X = nextX;
            }

            // Round positions to prevent sub-pixel jittering
            position.X = (float)Math.Round(position.X);
            position.Y = (float)Math.Round(position.Y);

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
            // Handle shooting (To be implemented)
            mouseState = Mouse.GetState();
            if (mouseState.LeftButton == ButtonState.Pressed && cooldown <= 0f)
            {
                bullets.Add(new Bullet(
                    texture: texture,
                    position: new Vector2(blaster.position.X + blaster.size[0] / 2 - 5, blaster.position.Y + blaster.size[1] / 2 - 5),
                    size: new int[2] { 10, 10 },
                    Lifetime: 90f,
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
