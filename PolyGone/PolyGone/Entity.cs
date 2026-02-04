using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;

namespace PolyGone;

class Entity : Sprite
{

    protected readonly Dictionary<Vector2, int>? collisionMap;
    protected float changeX;
    protected float changeY;
    protected bool isOnGround;
    protected int health;
    protected float invincibilityFrames;
    protected float friction; // Horizontal friction multiplier in range [0, 1]; 1 keeps full velocity (no friction), 0 stops movement immediately (maximum friction)


    public Entity(Texture2D texture, Vector2 position, int[] size, int health = 100, Color color = default, Rectangle? srcRect = null, Dictionary<Vector2, int>? collisionMap = null)
        : base(texture, position, size, color, srcRect)
    {
        this.collisionMap = collisionMap;
        this.changeX = 0f;
        this.changeY = 0f;
        this.isOnGround = false;
        this.health = health;
        this.invincibilityFrames = 0f;
        this.friction = 0.9f; // Default friction
    }

    protected virtual List<(Rectangle, CollisionType)> GetIntersectingTiles(Rectangle target)
    {
        var intersectingTiles = new List<(Rectangle, CollisionType)>();
        if (collisionMap == null) return intersectingTiles;
        foreach (var tile in collisionMap)
        {
            if (tile.Value == -1) continue;
            Rectangle tileRect = new Rectangle((int)tile.Key.X * 64, (int)tile.Key.Y * 64, 64, 64);
            CollisionType colType = CollisionTypeMapper.GetCollisionType(tile.Value);
            if (target.Intersects(tileRect))
            {
                intersectingTiles.Add((tileRect, colType));
            }
        }
        return intersectingTiles;
    }

    protected virtual void HandleVerticalCollision(ref bool onGround, ref float deltaY, List<(Rectangle, CollisionType)> collisions)
    {
        var (tileRect, colType) = collisions[0];
        switch (colType)
        {
            default:
            case CollisionType.Solid:
            case CollisionType.Rough:
            case CollisionType.Slippery:
                position.Y = deltaY > 0 ? tileRect.Top - size[1] : tileRect.Bottom;
                onGround = deltaY > 0;
                deltaY = 0;
                break;
            case CollisionType.SemiSolid:
                // By default, entities do not drop through platforms
                if (deltaY > 0 && (position.Y + size[1]) <= tileRect.Top + 10)
                {
                    position.Y = tileRect.Top - size[1];
                    deltaY = 0;
                    onGround = true;
                }
                else
                {
                    position.Y += deltaY;
                }
                break;
        }
    }

    protected virtual void HandleHorizontalCollision(ref float deltaX, List<(Rectangle, CollisionType)> collisions)
    {
        var (tileRect, colType) = collisions[0];
        switch (colType)
        {
            default:
            case CollisionType.Solid:
            case CollisionType.Rough:
            case CollisionType.Slippery:
                position.X = deltaX > 0 ? tileRect.Left - size[0] : tileRect.Right;
                deltaX = 0;
                break;
            case CollisionType.SemiSolid:
                position.X += deltaX;
                break;
        }
    }

    protected virtual List<Entity> GetIntersectingEntities(Rectangle target, List<Entity> others)
    {
        var intersectingEntities = new List<Entity>();
        foreach (var other in others)
        {
            if (other == this) continue;
            Rectangle otherRect = other.Rectangle;
            if (target.Intersects(otherRect))
            {
                intersectingEntities.Add(other);
            }
        }
        return intersectingEntities;
    }

    protected virtual void OnEntityCollision(Entity other)
    {
        // Default implementation does nothing
        // Override in derived classes to handle specific collision behavior
    }


    public virtual void HandleDeath()
    {
        // Default implementation resets position and health
        position = new Vector2(100, -100);
        health = 100;
    }

    // Physics and collision update for non-player entities (no input)
    protected virtual void PhysicsUpdate(float deltaTime)
    {
        // Apply gravity
        changeY += 0.7f;
        changeY = Math.Min(changeY, 14f);

        // Vertical collision and movement
        isOnGround = false;
        float nextY = position.Y + (changeY * deltaTime);
        Rectangle nextRectY = new Rectangle((int)position.X, (int)nextY, size[0], size[1]);
        var verticalCollisions = GetIntersectingTiles(nextRectY);
        if (verticalCollisions.Count > 0)
        {
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
        var horizontalCollisions = GetIntersectingTiles(nextRectX);
        if (horizontalCollisions.Count > 0)
        {
            horizontalCollisions = horizontalCollisions
                .OrderBy(c => Math.Abs(changeX > 0 ? c.Item1.Left - (position.X + size[0]) : c.Item1.Right - position.X))
                .ToList();
            HandleHorizontalCollision(ref changeX, horizontalCollisions.Take(1).ToList());
        }
        else
        {
            position.X = nextX;
        }

        // Apply friction to horizontal movement
        if (Math.Abs(changeX) > 0.5f)
            changeX *= friction;
        else
            changeX = 0f;

        // Round positions to prevent sub-pixel jittering
        position.X = (float)Math.Round(position.X);
        position.Y = (float)Math.Round(position.Y);
    }

    public void EntityCollisionUpdate(List<Entity> others)
    {
        Rectangle currentRect = new Rectangle((int)position.X, (int)position.Y, size[0], size[1]);
        var intersectingEntities = GetIntersectingEntities(currentRect, others);
        foreach (var other in intersectingEntities)
        {
            OnEntityCollision(other);
        }
    }

    // Check if there's ground ahead in the movement direction
    protected virtual bool IsGroundAhead(float direction)
    {
        if (collisionMap == null) return true;
        
        // Check from the bottom corner in the direction of movement
        float checkX = direction > 0
            ? position.X + size[0] + 1f   // Moving right: check from bottom-right corner, slightly ahead
            : position.X - 1f;            // Moving left: check from bottom-left corner, slightly ahead (to the left)
        
        float checkY = position.Y + size[1] + 1f; // Just below feet
        
        // Check if there's a tile at that position
        Rectangle checkRect = new Rectangle((int)checkX, (int)checkY, 1, 1);
        var tiles = GetIntersectingTiles(checkRect);
        
        return tiles.Count > 0;
    }

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 60f, 3);
        PhysicsUpdate(deltaTime);
        invincibilityFrames = Math.Max(0f, invincibilityFrames - 1f);
        if (health <= 0)
        {
            HandleDeath();
        }
        base.Update(gameTime);
    }

    
}