using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace PolyGone
{
    public enum Owner
    {
        Player,
        Enemy
    }

    class Projectile : Entity
    {
        public readonly float xSpeed;
        public readonly float ySpeed;
        public float lifetime; // Separate from Entity health for projectiles
        public readonly Owner owner; // Who fired this projectile
        public int damage => 40; // Fixed damage for now
        public Projectile(Texture2D texture, Vector2 position, int[] size, float lifetime, int health, Color color, float xSpeed, float ySpeed, Owner owner, Rectangle? srcRect = null, Dictionary<Vector2, int>? collisionMap = null)
            : base(texture, position, size, health, color, srcRect, collisionMap)
        {
            this.xSpeed = xSpeed;
            this.ySpeed = ySpeed;
            this.lifetime = lifetime;
            this.owner = owner;
        }

        // Fire projectiles from blaster to global mouse position
        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            position.X += (float)(xSpeed * deltaTime);
            position.Y += (float)(ySpeed * deltaTime);

            // Check for tile collision and destroy projectile if hit solid tile
            if (collisionMap != null)
            {
                Rectangle projectileRect = new Rectangle((int)position.X, (int)position.Y, size[0], size[1]);
                var collisions = GetIntersectingTiles(projectileRect);
                // Only destroy if hit a solid collision type (not SemiSolid or None)
                if (collisions.Any(c => c.Item2 != CollisionType.None && c.Item2 != CollisionType.SemiSolid))
                {
                    lifetime = 0;
                }
            }
        }
    }
}
