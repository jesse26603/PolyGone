using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PolyGone.Core;
using PolyGone.Entities;
using System;
using System.Collections.Generic;

namespace PolyGone;

/// <summary>
/// A high-health enemy that chases and attacks the player, speeding up and shooting at low health.
/// </summary>
class BerserkEnemy : Enemy
{
    private const int BERSERK_HEALTH_THRESHOLD = 150;
    private const float BERSERK_PATROL_MULTIPLIER = 2.5f;
    private readonly Player player;
    private AudioManager audioManager;
    private float shootCooldown = 0f;
    private const float SHOOT_COOLDOWN = 15f; // .25 seconds at 60 fps
    private const float VIEW_RANGE = 500f;    // Max viewing range in pixels
    private const float BULLET_SPEED = 50f;
    private const int BULLET_DAMAGE = 34;

    /// <summary>Projectiles fired by this enemy (flagged as Owner.Enemy).</summary>
    public readonly List<Projectile> Bullets = new();

    public BerserkEnemy(
        Texture2D texture,
        Vector2 position,
        AudioManager audioManager,
        int[] size,
        Player player,
        int health = 400,
        Color color = default,
        Rectangle? srcRect = null,
        Dictionary<Vector2, int>? collisionMap = null,
        int[]? visualSize = null)
        : base(texture, position, audioManager, size, health, color, srcRect, collisionMap, patrolSpeed: 1f, visualSize: visualSize)
    {
        this.player = player;
        this.audioManager = audioManager;
    }

    private void ShootAtPlayer()
    {
        Vector2 myCenter = new Vector2(position.X + size[0] / 2f, position.Y + size[1] / 2f);
        Vector2 playerCenter = new Vector2(player.position.X + player.size[0] / 2f, player.position.Y + player.size[1] / 2f);
        Vector2 direction = playerCenter - myCenter;
        float distance = direction.Length();

        // Only shoot if the player is within range
        if (distance > VIEW_RANGE || distance == 0f)
        {
            return;
        }

        direction.Normalize();

        Bullets.Add(new Projectile(
            texture: texture,
            position: new Vector2(myCenter.X - 5f, myCenter.Y - 5f),
            audioManager: audioManager,
            size: new int[2] { 10, 10 },
            lifetime: 800f,
            health: 1,
            damage: BULLET_DAMAGE,
            color: Color.Black,
            xSpeed: direction.X * BULLET_SPEED,
            ySpeed: direction.Y * BULLET_SPEED,
            owner: Owner.Enemy,
            srcRect: srcRect,
            collisionMap: CollisionMap
        ));

        audioManager.PlayAudio("shootSfx", true, "null", false); //Play shoot sound effect

    }

    private bool IsPlayerInViewRange()
    {
        Vector2 myCenter = new Vector2(position.X + size[0] / 2f, position.Y + size[1] / 2f);
        Vector2 playerCenter = new Vector2(player.position.X + player.size[0] / 2f, player.position.Y + player.size[1] / 2f);
        return Vector2.Distance(myCenter, playerCenter) <= VIEW_RANGE;
    }

    protected override bool ShouldPatrol()
    {
        return !IsPlayerInViewRange();
    }

    protected override float GetPatrolSpeedMultiplier()
    {
        return Health <= BERSERK_HEALTH_THRESHOLD ? BERSERK_PATROL_MULTIPLIER : 1f;
    }

    public override void Update(GameTime gameTime)
    {
        bool isBerserk = Health <= BERSERK_HEALTH_THRESHOLD;
        bool playerInViewRange = IsPlayerInViewRange();

        // Chase player when in view range (instead of patrolling)
        if (playerInViewRange)
        {
            float myCenterX = position.X + size[0] / 2f;
            float playerCenterX = player.position.X + player.size[0] / 2f;
            float deltaX = playerCenterX - myCenterX;
            float chaseSpeed = PatrolSpeed * GetPatrolSpeedMultiplier();

            ChangeX = Math.Abs(deltaX) > 2f ? Math.Sign(deltaX) * chaseSpeed : 0f;
        }

        // Count down and fire while berserk
        if (isBerserk && shootCooldown > 0f)
        {
            shootCooldown -= 1f;
        }
        else if (isBerserk)
        {
            ShootAtPlayer();
            shootCooldown = SHOOT_COOLDOWN;
        }

        // Advance own bullets and prune expired ones
        for (int i = Bullets.Count - 1; i >= 0; i--)
        {
            Bullets[i].Update(gameTime);
            if (Bullets[i].Lifetime <= 0)
            {
                Bullets.RemoveAt(i);
            }
        }

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
    {
        base.Draw(spriteBatch, offset);

        foreach (var bullet in Bullets)
        {
            bullet.Draw(spriteBatch, offset);
        }
    }
}
