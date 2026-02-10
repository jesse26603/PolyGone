using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace PolyGone;

class Enemy : Entity
{
    private float patrolSpeed;
    private float patrolDirection = 1f; // 1 for right, -1 for left
    
    public Enemy(Texture2D texture, Vector2 position, int[] size, int health = 100, Color color = default, Rectangle? srcRect = null, Dictionary<Vector2, int>? collisionMap = null, float patrolSpeed = 1f, int[]? visualSize = null)
        : base(texture, position, size, health, color, srcRect, collisionMap, visualSize)
    {
        this.friction = 0.9f; // Enemy has default friction
        this.patrolSpeed = patrolSpeed;
    }

    protected override void OnEntityCollision(Entity other)
    {
        switch (other)
        {
            case Projectile projectile:
                // Only take damage from player projectiles if not invincible
                if (projectile.owner == Owner.Player && invincibilityFrames <= 0f)
                {
                    TakeDamage(projectile.damage, 30f);
                    // Knockback effect
                    float knockbackStrength = 8f;
                    Vector2 projectileVelocity = new Vector2(projectile.xSpeed, projectile.ySpeed);
                    if (projectileVelocity != Vector2.Zero)
                    {
                        projectileVelocity.Normalize();
                        changeX += projectileVelocity.X * knockbackStrength;
                        changeY += projectileVelocity.Y * knockbackStrength;
                    }
                    else
                    {
                        // Fallback: if projectile has no velocity, apply a simple upward knockback
                        changeY -= knockbackStrength;
                    }
                    // Expire the projectile so it cannot damage this or other enemies again
                    projectile.lifetime = 0f;
                }
                break;
        }
    }

    private void PatrolUpdate()
    {
        // Don't patrol during knockback/invincibility
        if (invincibilityFrames > 0f)
        {
            return;
        }
        
        // Only check ahead if we're on the ground
        if (!isOnGround)
        {
            return;
        }
        
        // Check for walls ahead by looking a bit further ahead
        float checkDistance = 10f;
        float nextX = position.X + (patrolDirection * checkDistance);
        Rectangle nextRect = new Rectangle((int)nextX, (int)position.Y, size[0], size[1]);
        var horizontalCollisions = GetIntersectingTiles(nextRect);
        
        // Check if there's ground ahead
        bool groundAhead = IsGroundAhead(patrolDirection);
        
        // Reverse direction if hitting a wall or reaching an edge
        if (horizontalCollisions.Count > 0 || !groundAhead)
        {
            patrolDirection *= -1f;
        }
        
        // Set horizontal velocity (not position directly)
        changeX = patrolDirection * patrolSpeed;
    }


    public override void Update(GameTime gameTime)
    {
        // Update patrol behavior
        PatrolUpdate();
        
        base.Update(gameTime);
    }
}
