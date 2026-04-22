using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PolyGone.Core;
using PolyGone.Entities;
using System;
using System.Collections.Generic;

namespace PolyGone;

class Enemy : Entity
{
    private readonly float patrolSpeed;
    private float patrolDirection = 1f; // 1 for right, -1 for left
    private AudioManager audioManager;
    private readonly Player? player;
    private const float VIEW_RANGE = 400f;
    private float hitFlashFrames = 0f;
    private float jumpDelay = 0f;
    private const float HIT_FLASH_DURATION = 6f; // 0.1s at 60 FPS

    // Multi-hit damage system
    private float damageWindow = 0f; // Frames remaining in damage window
    private int accumulatedDamage = 0; // Damage accumulated during current window
    private readonly List<Projectile> hitProjectiles = new List<Projectile>(); // Track projectiles that hit during window
    private const float DAMAGE_WINDOW_DURATION = 2f; // 2 frames to accumulate damage

    public Enemy(Texture2D texture, Vector2 position, AudioManager audioManager, int[] size, int health = 100, Color color = default, Rectangle? srcRect = null, Dictionary<Vector2, int>? collisionMap = null, float patrolSpeed = 1f, int[]? visualSize = null, Player? player = null)

        : base(texture, position, audioManager, size, health, color, srcRect, collisionMap, visualSize)
    {
        this.Friction = 0.9f; // Enemy has default friction
        this.patrolSpeed = patrolSpeed;
        this.audioManager = audioManager;
        this.player = player;
    }

    protected override void OnEntityCollision(Entity other)
    {
        switch (other)
        {
            case Projectile projectile:
                // Only take damage from player projectiles
                if (projectile.FiredBy == Owner.Player)
                {
                    HandleProjectileHit(projectile);
                }
                break;
        }
    }

    private void HandleProjectileHit(Projectile projectile)
    {
        if (projectile.Lifetime <= 0f)
        {
            return;
        }

        if (projectile.EnemiesHit.Contains(this))
        {
            return; // Already hit by this projectile
        }

        hitFlashFrames = HIT_FLASH_DURATION;

        // If no damage window is active, start a new one
        if (damageWindow <= 0f)
        {
            damageWindow = DAMAGE_WINDOW_DURATION;
            accumulatedDamage = 0;
            hitProjectiles.Clear();
        }

        audioManager.PlayAudio("collisionSfx", true, "null", false); //Play collision sound effect

        // Add this projectile's damage to accumulated damage
        accumulatedDamage += projectile.Damage;
        hitProjectiles.Add(projectile);
        projectile.EnemiesHit.Add(this); // Track that this enemy has been hit by this projectile
        // Expire the projectile unless it's piercing (piercing goes through enemies)
        if (!projectile.IsPiercing)
        {
            projectile.Lifetime = 0f;
        }

        // Apply knockback from the first projectile only (to prevent excessive knockback)
        if (hitProjectiles.Count == 1)
        {
            ApplyKnockback(projectile);
        }
    }

    private void ApplyKnockback(Projectile projectile)
    {
        float knockbackStrength = 8f;
        Vector2 projectileVelocity = new Vector2(projectile.XSpeed, projectile.YSpeed);
        if (projectileVelocity != Vector2.Zero)
        {
            projectileVelocity.Normalize();
            ChangeX += projectileVelocity.X * knockbackStrength;
            ChangeY += projectileVelocity.Y * knockbackStrength;
        }
        else
        {
            // Fallback: if projectile has no velocity, apply a simple upward knockback
            ChangeY -= knockbackStrength;
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
                Health -= accumulatedDamage; // Apply accumulated damage
                accumulatedDamage = 0;
                hitProjectiles.Clear();
            }
        }
    }

    private void PatrolUpdate()
    {
        // Check for walls ahead by looking a bit further ahead
        float checkDistance = 10f;
        float nextX = position.X + (patrolDirection * checkDistance);
        Rectangle nextRect = new Rectangle((int)nextX, (int)position.Y, size[0], size[1]);
        var horizontalCollisions = GetIntersectingTiles(nextRect);

        if (!ShouldPatrol())
        {
            return;
        }

        // Only check ahead if we're on the ground
        if (!IsOnGround)
        {
            return;
        }

        // Check if there's ground ahead
        bool groundAhead = IsGroundAhead(patrolDirection);

        // Reverse direction if hitting a wall or reaching an edge
        if (horizontalCollisions.Count > 0 || !groundAhead)
        {
            patrolDirection *= -1f;
        }

        // Set horizontal velocity (not position directly)
        ChangeX = patrolDirection * patrolSpeed * GetPatrolSpeedMultiplier();
    }

    private bool IsPlayerInViewRange()
    {
        if (player is null)
        {
            return false;
        }

        Vector2 myCenter = new Vector2(position.X + size[0] / 2f, position.Y + size[1] / 2f);
        Vector2 playerCenter = new Vector2(player.position.X + player.size[0] / 2f, player.position.Y + player.size[1] / 2f);
        return Vector2.Distance(myCenter, playerCenter) <= VIEW_RANGE;
    }

    private bool IsStandingOnSemiSolid()
    {
        if (!IsOnGround || CollisionMap is null)
        {
            return false;
        }

        int centerTileX = (int)((position.X + size[0] / 2f) / TILE_SIZE);
        int feetTileY = (int)((position.Y + size[1]) / TILE_SIZE);
        var keyBelow = new Vector2(centerTileX, feetTileY);

        return CollisionMap.TryGetValue(keyBelow, out int tileId)
               && tileId != -1
               && CollisionTypeMapper.GetCollisionType(tileId) == CollisionType.SemiSolid;
    }

    private void DropThroughSemiSolid()
    {
        // Push down far enough to bypass the semi-solid landing tolerance.
        position.Y += 12f;
        IsOnGround = false;
        if (ChangeY < 2f)
        {
            ChangeY = 2f;
        }
    }

    protected virtual void ChasePlayerUpdate()
    {
        if (player is null || !IsPlayerInViewRange())
        {
            return;
        }

        float myCenterX = position.X + size[0] / 2f;
        float playerCenterX = player.position.X + player.size[0] / 2f;
        float deltaX = playerCenterX - myCenterX;

        ChangeX = Math.Abs(deltaX) > 2f ? Math.Sign(deltaX) * patrolSpeed * GetPatrolSpeedMultiplier() : 0f;

        bool playerIsAbove = player.position.Y + player.size[1] < position.Y + 5f;
        bool playerIsNext = player.position.X + player.size[1] < position.X + 10f;
        bool playerIsBelow = player.position.Y > position.Y + size[1] - 5f;

        if (playerIsAbove && IsOnGround)
        {
            float checkDistance = 10f;
            float nextX = position.X + (Math.Sign(deltaX) * checkDistance);
            Rectangle nextRect = new Rectangle((int)nextX, (int)position.Y, size[0], size[1]);
            var horizontalCollisions = GetIntersectingTiles(nextRect);
            bool groundAhead = IsGroundAhead(Math.Sign(deltaX));

            if (horizontalCollisions.Count > 0 || !groundAhead || playerIsNext)
            {
                if (jumpDelay <= 0f)
                {
                    ChangeY = -16.75f; // Jump strength
                    jumpDelay = 30f; // 0.5s delay at 60 FPS
                }
            }
        }
        else if (playerIsBelow && IsStandingOnSemiSolid())
        {
            DropThroughSemiSolid();
        }
    }




    protected virtual float GetPatrolSpeedMultiplier()
    {
        return 1f;
    }

    protected virtual bool ShouldPatrol()
    {
        return true;
    }

    protected float PatrolSpeed => patrolSpeed;


    public override void Update(GameTime gameTime)
    {
        if (hitFlashFrames > 0f)
        {
            hitFlashFrames -= 1f;
        }
        if (jumpDelay > 0f && IsOnGround)
        {
            jumpDelay -= 1f;
        }

        // Update damage window system
        UpdateDamageWindow();

        if (IsPlayerInViewRange())
        {
            ChasePlayerUpdate();
        }
        else
        {
            // Update patrol behavior
            PatrolUpdate();
        }

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
    {
        Color originalColor = color;

        if (hitFlashFrames > 0f)
        {
            // Keep some original brightness while flashing red
            color = Color.Lerp(originalColor, Color.Red, 0.75f);
        }

        base.Draw(spriteBatch, offset);
        color = originalColor;
    }
}
