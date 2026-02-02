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

    public Entity(Texture2D texture, Vector2 position, int[] size, int health = 100, Color color = default, Rectangle? srcRect = null, Dictionary<Vector2, int>? collisionMap = null)
        : base(texture, position, size, color, srcRect)
    {
        this.collisionMap = collisionMap;
        this.changeX = 0f;
        this.changeY = 0f;
        this.isOnGround = false;
        this.health = health;
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
                if (deltaY > 0)
                {
                    position.Y = tileRect.Top - size[1];
                    onGround = true;
                }
                else if (deltaY < 0)
                {
                    position.Y = tileRect.Bottom;
                }
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
                if (deltaX > 0)
                {
                    position.X = tileRect.Left - size[0];
                }
                else if (deltaX < 0)
                {
                    position.X = tileRect.Right;
                }
                deltaX = 0;
                break;
            case CollisionType.SemiSolid:
                position.X += deltaX;
                break;
        }
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

        // Round positions to prevent sub-pixel jittering
        position.X = (float)Math.Round(position.X);
        position.Y = (float)Math.Round(position.Y);
    }

    public override void Update(GameTime gameTime)
    {
        float deltaTime = (float)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 60f, 3);
        PhysicsUpdate(deltaTime);
        base.Update(gameTime);
    }

    
}