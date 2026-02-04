using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace PolyGone;

class Enemy : Entity
{
    private float patrolSpeed;
    private float patrolDirection = 1f; // 1 for right, -1 for left
    
    public Enemy(Texture2D texture, Vector2 position, int[] size, int health = 100, Color color = default, Rectangle? srcRect = null, Dictionary<Vector2, int>? collisionMap = null, float patrolSpeed = 1f)
        : base(texture, position, size, health, color, srcRect, collisionMap)
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
                    health -= projectile.damage;
                    invincibilityFrames = 30f; // 0.5 second invincibility at 60fps
                    // Knockback effect
                    float knockbackStrength = 8f;
                    changeX += projectile.xSpeed > 0 ? knockbackStrength : -knockbackStrength;
                    changeY -= knockbackStrength; // Knock upwards
                    
                }
                break;
        }
    }

    private void PatrolUpdate(float deltaTime)
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

    public void SetPatrolSpeed(float speed)
    {
        this.patrolSpeed = speed;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        
        // Update patrol behavior
        PatrolUpdate(deltaTime);
        
        base.Update(gameTime);
    }
}
