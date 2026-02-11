using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace PolyGone;

class Enemy : Entity
{
    private float patrolSpeed;
    private float patrolDirection = 1f; // 1 for right, -1 for left
    
    // Multi-hit damage system
    private float damageWindow = 0f; // Frames remaining in damage window
    private int accumulatedDamage = 0; // Damage accumulated during current window
    private readonly List<Projectile> hitProjectiles = new List<Projectile>(); // Track projectiles that hit during window
    private const float DAMAGE_WINDOW_DURATION = 2f; // 2 frames to accumulate damage
    
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
                // Only take damage from player projectiles
                if (projectile.owner == Owner.Player)
                {
                    HandleProjectileHit(projectile);
                }
                break;
        }
    }

    private void HandleProjectileHit(Projectile projectile)
    {
        // Skip if already hit by this projectile or if in invincibility frames
        if (hitProjectiles.Contains(projectile) || invincibilityFrames > 0f)
            return;

        // If no damage window is active, start a new one
        if (damageWindow <= 0f)
        {
            damageWindow = DAMAGE_WINDOW_DURATION;
            accumulatedDamage = 0;
            hitProjectiles.Clear();
        }

        // Add this projectile's damage to accumulated damage
        accumulatedDamage += projectile.damage;
        hitProjectiles.Add(projectile);

        // Expire the projectile so it doesn't hit other enemies
        projectile.lifetime = 0f;

        // Apply knockback from the first projectile only (to prevent excessive knockback)
        if (hitProjectiles.Count == 1)
        {
            ApplyKnockback(projectile);
        }
    }

    private void ApplyKnockback(Projectile projectile)
    {
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
    }

    private void UpdateDamageWindow()
    {
        if (damageWindow > 0f)
        {
            damageWindow -= 1f;
            
            // When damage window closes, apply accumulated damage and start invincibility
            if (damageWindow <= 0f && accumulatedDamage > 0)
            {
                health -= accumulatedDamage; // Apply accumulated damage
                invincibilityFrames = 30f; // 30 frame invincibility after damage window
                accumulatedDamage = 0;
                hitProjectiles.Clear();
            }
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
        // Update damage window system
        UpdateDamageWindow();
        
        // Update patrol behavior
        PatrolUpdate();
        
        base.Update(gameTime);
    }
}
