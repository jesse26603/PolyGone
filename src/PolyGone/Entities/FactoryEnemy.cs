using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PolyGone.Core;
using PolyGone.Entities;
using System.Collections.Generic;

namespace PolyGone;

/// <summary>
/// Stationary enemy that spawns low health enemies periodically 
/// </summary>
class FactoryEnemy : Enemy
{
    private readonly Player player;
    private AudioManager audioManager;
    private float spawnCooldown = 0f;
    private const float SPAWN_COOLDOWN = 180f; // 3 seconds at 60 fps
    private const int MAX_SPAWNS = 5; // Max number of alive enemies this factory can have spawned at once
    private const float VIEW_RANGE = 1200f;    // Max viewing range in pixels

    public readonly List<Enemy> SpawnedEnemies = new();
    private readonly List<Enemy> activeSpawnedEnemies = new();


    public FactoryEnemy(
        Texture2D texture,
        Vector2 position,
        AudioManager audioManager,
        int[] size,
        Player player,
        int health = 200,
        Color color = default,
        Rectangle? srcRect = null,
        Dictionary<Vector2, int>? collisionMap = null,
        int[]? visualSize = null)
        : base(texture, position, audioManager, size, health, color, srcRect, collisionMap, patrolSpeed: 0f, visualSize: visualSize)
    {
        this.player = player;
        this.audioManager = audioManager;
    }

    private void SpawnEnemy()
    {
        CleanupActiveSpawnedEnemies();

        if (activeSpawnedEnemies.Count >= MAX_SPAWNS)
        {
            return;
        }

        Vector2 myCenter = new Vector2(position.X + size[0] / 2f, position.Y + size[1] / 2f);
        Vector2 playerCenter = new Vector2(player.position.X + player.size[0] / 2f, player.position.Y + player.size[1] / 2f);
        Vector2 direction = playerCenter - myCenter;
        float distance = direction.Length();

        // Only spawn enemy if the player is within range
        if (distance > VIEW_RANGE || distance == 0f)
        {
            return;
        }

        // Spawn a new low health enemy
        audioManager.PlayAudio("shootSfx", true, "null", false); //Play shoot sound effect (placeholder)
        Enemy spawnedEnemy = new Enemy(
            texture: texture,
            position: new Vector2(position.X + size[0] / 2f - 15, position.Y + size[1] / 2f - 15),
            audioManager: audioManager,
            size: new int[] { 30, 30 },
            health: 25,
            color: Color.White,
            srcRect: srcRect,
            collisionMap: CollisionMap,
            patrolSpeed: 2f,
            visualSize: new int[] { 32, 32 },
            player: player
        );

        SpawnedEnemies.Add(spawnedEnemy);
        activeSpawnedEnemies.Add(spawnedEnemy);
 
    }

    private void CleanupActiveSpawnedEnemies()
    {
        activeSpawnedEnemies.RemoveAll(enemy => !enemy.IsAlive);
    }

    private bool IsPlayerInViewRange()
    {
        Vector2 myCenter = new Vector2(position.X + size[0] / 2f, position.Y + size[1] / 2f);
        Vector2 playerCenter = new Vector2(player.position.X + player.size[0] / 2f, player.position.Y + player.size[1] / 2f);
        return Vector2.Distance(myCenter, playerCenter) <= VIEW_RANGE;
    }



    public override void Update(GameTime gameTime)
    {
        CleanupActiveSpawnedEnemies();

        // Count down and spawn while player is nearby
        if (spawnCooldown > 0f)
        {
            spawnCooldown -= 1f;
        }
        else if (IsPlayerInViewRange())
        {
            SpawnEnemy();
            spawnCooldown = SPAWN_COOLDOWN;
        }

        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
    {
        base.Draw(spriteBatch, offset);
    }
}