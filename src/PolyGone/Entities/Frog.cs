using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PolyGone.Core;
using PolyGone.Entities;
using System;
using System.Collections.Generic;

namespace PolyGone;

/// <summary>
/// Default enemy type but can jump
/// </summary>
class Frog : Enemy
{
    private readonly Player player;
    private const float VIEW_RANGE = 500f;    // Max viewing range in pixels
    private float jumpCooldown = 0f;
    private const float JUMP_COOLDOWN_FRAMES = 30f;


    public Frog
(
        Texture2D texture,
        Vector2 position,
        AudioManager audioManager,
        int[] size,
        Player player,
        int health = 100,
        Color color = default,
        Rectangle? srcRect = null,
        Dictionary<Vector2, int>? collisionMap = null,
        int[]? visualSize = null)
        : base(texture, position, audioManager, size, health, color, srcRect, collisionMap, patrolSpeed: 0.8f, visualSize: visualSize, player: player)
    {
        this.player = player;
        this.Friction = 0.93f;
        this.GravityScale = 1.1f;
    }

    private void Jump()
    {
        Vector2 myCenter = new Vector2(position.X + size[0] / 2f, position.Y + size[1] / 2f);
        Vector2 playerCenter = new Vector2(player.position.X + player.size[0] / 2f, player.position.Y + player.size[1] / 2f);
        Vector2 direction = playerCenter - myCenter;
        float distance = direction.Length();

        // Only jump if player is in range and not below this enemy.
        // In screen coordinates, larger Y means lower on screen.
        if (distance > VIEW_RANGE || distance == 0f || playerCenter.Y > myCenter.Y)
        {
            return;
        }

        direction.Normalize();

        ChangeY = -17f; // Jump strength
        ChangeX = direction.X * 20f; // Horizontal leap velocity

    }

    public override void Update(GameTime gameTime)
    {
        if (jumpCooldown > 0f)
        {
            jumpCooldown -= 1f;
        }

        base.Update(gameTime);
    }

    protected override void ChasePlayerUpdate()
    {
        float myCenterX = position.X + size[0] / 2f;
        float playerCenterX = player.position.X + player.size[0] / 2f;
        float deltaX = playerCenterX - myCenterX;
        float chaseSpeed = PatrolSpeed * GetPatrolSpeedMultiplier();

        // Use regular chase movement while grounded.
        // In air, keep leap momentum instead of snapping to patrol speed.
        if (IsOnGround)
        {
            ChangeX = Math.Abs(deltaX) > 2f ? Math.Sign(deltaX) * chaseSpeed : 0f;
        }

        if (IsOnGround && jumpCooldown <= 0f)
        {
            Jump();
            jumpCooldown = JUMP_COOLDOWN_FRAMES;
        }
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
    {
        base.Draw(spriteBatch, offset);
    }
}
