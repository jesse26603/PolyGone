using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System.Text.Json;
using System.Linq;
using System;
using PolyGone.Entities;
using PolyGone.Graphics;
using PolyGone.Core;
using System.Diagnostics;

namespace PolyGone;

public class GameScene : IScene
{
    private ContentManager contentManager;
    private Texture2D playerSheet = null!;
    private Texture2D enemySheet = null!;
    private Texture2D miscSheet = null!;
    private Texture2D textureSheet = null!;
    private Texture2D foregroundSheet = null!;
    private Texture2D backgroundSheet = null!;
    private Texture2D collisionSheet = null!;
    private AudioManager audioManager;
    private SpriteFont hudFont = null!;
    private SceneManager sceneManager;
    private Player player = null!;
    private FollowCamera camera = null!;
    private GameUI gameUI = null!;
    private readonly GraphicsDeviceManager graphics;
    private Dictionary<Vector2, int> tileMap = null!;
    private Dictionary<Vector2, int> collisionMap = null!;
    private List<Rectangle> textureStore;
    private Vector2 playerPos;
    private bool playerSpawnFound = false;
    private readonly List<Vector2> enemySpawns = new(); // Store enemy spawn positions
    private readonly List<Vector2> turretEnemySpawns = new(); // Store turret spawn positions
    private readonly List<Vector2> berserkEnemySpawns = new(); // Store berserk spawn positions
    private readonly List<Vector2> factoryEnemySpawns = new(); // Store factory spawn positions
    private readonly List<Vector2> frogSpawns = new(); // Store frog spawn positions
    private readonly List<Entity> enemies = new(); // Placeholder for enemy list
    private readonly List<TurretEnemy> turretEnemies = new(); // Stationary blaster enemies
    private readonly List<BerserkEnemy> berserkEnemies = new(); // Chasing enemies that shoot when low health
    private readonly List<FactoryEnemy> factoryEnemies = new(); // Stationary enemies that spawn patrol enemies
    private readonly List<Frog> frogs = new(); // Jumping enemies
    private readonly List<Projectile> orphanedTurretBullets = new(); // Bullets that outlive their turret
    private GoalTrigger goalTrigger = null!; // Win condition trigger
    private SwitchTrigger inventoryAccess = null!; // Inventory access trigger
    private List<LevelDoor> levelDoors = new(); // Doors connecting levels to hub
    private bool levelComplete = false;
    private bool gameOver = false;
    private readonly List<ItemType> selectedItems;
    private readonly List<BlasterAttachmentType> selectedAttachments;
    private readonly string levelName;
    private int? loadX;
    private int? loadY;

    public GameScene(ContentManager contentManager, SceneManager sceneManager, AudioManager audioManager, GraphicsDeviceManager graphics, string levelName = "TestLevel", List<ItemType>? selectedItems = null, List<BlasterAttachmentType>? selectedAttachments = null, int? loadX = null, int? loadY = null)
    {
        this.contentManager = contentManager;
        this.sceneManager = sceneManager;
        this.audioManager = audioManager;
        this.graphics = graphics;
        this.selectedItems = selectedItems ?? new List<ItemType>();
        this.selectedAttachments = selectedAttachments ?? new List<BlasterAttachmentType>();
        this.levelName = levelName;
        this.loadX = loadX;
        this.loadY = loadY;

        LoadMapFromJson("Maps/" + levelName + ".json");
        textureStore = GetTextureStore(32, new int[2] { 2, 2 });
    }

    // Public method to get the level name for restart functionality
    public string GetLevelName() => levelName;

    // Public methods to get the current loadout for restart functionality
    public List<ItemType> GetSelectedItems() => new List<ItemType>(selectedItems);
    public List<BlasterAttachmentType> GetSelectedAttachments() => new List<BlasterAttachmentType>(selectedAttachments);

    // Generates a list of rectangles representing individual textures in a texture atlas
    public List<Rectangle> GetTextureStore(int textureSize, int[] gridSize)
    {
        List<Rectangle> textureStore = new();
        for (int y = 0; y < gridSize[1]; y++)
        {
            for (int x = 0; x < gridSize[0]; x++)
            {
                textureStore.Add(new Rectangle(x * textureSize, y * textureSize, textureSize, textureSize));
            }
        }
        return textureStore;
    }

    // Adjusts coordinates from Tiled's coordinate system to the game's coordinate system
    public Vector2 AdjustCoordinates(float x, float y)
    {
        // Scale from 32px Tiled tiles to 64px game tiles (x2), then shift y up 64px because
        // Tiled stores object y at the bottom of the object while the game uses a top-left origin.
        return new Vector2(x * 2, y * 2 - 64);
    }

    // Loads tile and collision maps from a JSON file exported from Tiled
    public void LoadMapFromJson(string filepath)
    {
        // Read and parse JSON file
        string jsonContent = File.ReadAllText(filepath);
        using JsonDocument doc = JsonDocument.Parse(jsonContent);

        // Get root and layers
        JsonElement root = doc.RootElement;
        JsonElement layers = root.GetProperty("layers");

        int width = root.GetProperty("width").GetInt32();

        tileMap = new Dictionary<Vector2, int>();
        collisionMap = new Dictionary<Vector2, int>();

        foreach (JsonElement layer in layers.EnumerateArray())
        {
            string? layerName = layer.GetProperty("name").GetString();
            // Process tile and collision layers
            if (layerName != "Objects")
            {
                JsonElement dataArray = layer.GetProperty("data");

                int index = 0;
                foreach (JsonElement tile in dataArray.EnumerateArray())
                {
                    int tileValue = tile.GetInt32();
                    int x = index % width;
                    int y = index / width;

                    if (tileValue > 0)
                    {
                        // Tiled's firstgid is 1, so we subtract 1 to convert to 0-based index.
                        // Wrap tileValue to fit within our texture store (assuming 16 tiles per layer in Tiled)
                        if (layerName == "Tiles")
                        {
                            tileMap[new Vector2(x, y)] = tileValue % 16 - 1;
                        }
                        else if (layerName == "Collisions")
                        {
                            collisionMap[new Vector2(x, y)] = tileValue % 16 - 1;
                        }
                    }

                    index++;
                }
            }
            // Process object layer for entity spawns and other objects
            else
            {
                List<JsonElement> objects = layer.GetProperty("objects").EnumerateArray().ToList();
                foreach (JsonElement obj in objects)
                {
                    string? objType = obj.GetProperty("type").GetString();
                    switch (objType)
                    {
                        case "Player":
                        case "PlayerSpawn":
                            int playerX;
                            int playerY;
                            if (loadX.HasValue && loadY.HasValue)
                            {
                                playerX = loadX.Value;
                                playerY = loadY.Value;
                            }
                            else
                            {
                                playerX = (int)obj.GetProperty("x").GetSingle();
                                playerY = (int)obj.GetProperty("y").GetSingle();
                            }
                            playerPos = AdjustCoordinates(
                                playerX,
                                playerY
                            );
                            playerSpawnFound = true;
                            break;
                        case "Enemy":
                            Vector2 enemyPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            enemySpawns.Add(enemyPos);
                            break;
                        case "TurretEnemy":
                            Vector2 turretPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            turretEnemySpawns.Add(turretPos);
                            break;
                        case "BerserkEnemy":
                            Vector2 berserkPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            berserkEnemySpawns.Add(berserkPos);
                            break;
                        case "FactoryEnemy":
                            Vector2 factoryPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            factoryEnemySpawns.Add(factoryPos);
                            break;
                        case "Frog":
                            Vector2 frogPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            frogSpawns.Add(frogPos);
                            break;
                        case "Goal":
                            Vector2 goalPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            int goalWidth = (int)(obj.GetProperty("width").GetSingle() * 2);
                            int goalHeight = (int)(obj.GetProperty("height").GetSingle() * 2);
                            goalTrigger = new GoalTrigger(goalPos, goalWidth, goalHeight);
                            break;
                        case "Door":
                            Vector2 doorPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            int doorWidth = (int)(obj.GetProperty("width").GetSingle() * 2);
                            int doorHeight = (int)(obj.GetProperty("height").GetSingle() * 2);
                            string connectedLevel = "Hub";
                            int playerLoadX = 0;
                            int playerLoadY = 0;
                            List<JsonElement> properties = obj.GetProperty("properties").EnumerateArray().ToList();
                            foreach (JsonElement prop in properties)
                            {
                                string? propName = prop.GetProperty("name").GetString();
                                switch (propName)
                                {
                                    case "connectedLevel":
                                        connectedLevel = (string)(prop.GetProperty("value").GetString() ?? "Hub");
                                        break;
                                    case "loadX":
                                        playerLoadX = (int)prop.GetProperty("value").GetSingle();
                                        break;
                                    case "loadY":
                                        playerLoadY = (int)prop.GetProperty("value").GetSingle();
                                        break;
                                    default:
                                        break;
                                }
                            }
                            levelDoors.Add(new LevelDoor(doorPos, doorWidth, doorHeight, audioManager, connectedLevel, playerLoadX, playerLoadY));
                            break;
                        case "Inventory":
                            Vector2 inventoryPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            int inventoryWidth = (int)(obj.GetProperty("width").GetSingle() * 2);
                            int inventoryHeight = (int)(obj.GetProperty("height").GetSingle() * 2);
                            inventoryAccess = new SwitchTrigger(inventoryPos, inventoryWidth, inventoryHeight, audioManager);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        // If no player marker exists in the map, allow door-provided coordinates.
        if (!playerSpawnFound && loadX.HasValue && loadY.HasValue)
        {
            playerPos = AdjustCoordinates(loadX.Value, loadY.Value);
            playerSpawnFound = true;
        }

        // Validate that a player spawn was found
        if (!playerSpawnFound)
        {
            throw new InvalidOperationException(
                $"Map file '{filepath}' is missing a required player spawn in the Objects layer. " +
                "Please ensure the map contains exactly one object with type='Player' or type='PlayerSpawn', " +
                "or provide door load coordinates when transitioning into this level."
            );
        }
    }

    public void Load()
    {
        // Reset input state to prevent carried over clicks from triggering actions
        InputManager.ResetClickCooldown();

        //Play level music
        if (levelName != null)
        {
            switch (levelName)
            {
                case "TestLevel":
                    audioManager.PlayAudio("null", false, "level1Song", true);
                    break;
                case "TestLevel2":
                    audioManager.PlayAudio("null", false, "level2Song", true);
                    break;
                case "TestLevel3":
                    audioManager.PlayAudio("null", false, "level3Song", true);
                    break;
                default:
                    break;
            }
        }

        // Load texture atlas and initialize camera
        playerSheet = contentManager.Load<Texture2D>("Textures/Sprites/PolyGonePlayerSheet");
        enemySheet = contentManager.Load<Texture2D>("Textures/Sprites/PolyGoneEnemySheet");
        miscSheet = contentManager.Load<Texture2D>("Textures/Sprites/PolyGoneMiscSpriteSheet");
        textureSheet = contentManager.Load<Texture2D>("Textures/Tiles/PolyGoneMgSheet");
        foregroundSheet = contentManager.Load<Texture2D>("Textures/Tiles/PolyGoneFgSheet");
        backgroundSheet = contentManager.Load<Texture2D>("Textures/Tiles/PolyGoneBgSheet");
        collisionSheet = contentManager.Load<Texture2D>("Textures/Tiles/PolyGoneCollisionSheet");
        try
        {
            hudFont = contentManager.Load<SpriteFont>("Fonts/PauseMenu");
        }
        catch (ContentLoadException)
        {
            Console.WriteLine("Warning: Could not load SpriteFont 'Fonts/PauseMenu'. HUD text will not be rendered with this font.");
        }
        camera = new(new Vector2(0, 0));
        // Initialize player with selected items and weapon
        player = new Player(
            texture: playerSheet,
            position: playerPos,
            size: new int[2] { 40, 60 },
            health: 100,
            color: Color.White,
            srcRect: textureStore[0],
            collisionMap: collisionMap,
            blasterTexture: playerSheet,
            selectedItems: selectedItems,
            selectedAttachments: selectedAttachments,
            audioManager: audioManager,
            visualSize: new int[2] { 64, 64 }
        );

        // Initialize GameUI
        gameUI = new GameUI(player, textureSheet, textureStore[2], hudFont);
        // Initialize turret enemies
        turretEnemies.AddRange(turretEnemySpawns.Select(spawnPos => new TurretEnemy(
            texture: enemySheet,
            position: spawnPos,
            audioManager: audioManager,
            size: new int[2] { 60, 60 },
            player: player,
            health: 80,
            color: Color.White,
            srcRect: textureStore[1],
            collisionMap: collisionMap,
            visualSize: new int[2] { 64, 64 }
        )));
        // Initialize berserk enemies
        berserkEnemies.AddRange(berserkEnemySpawns.Select(spawnPos => new BerserkEnemy(
            texture: enemySheet,
            position: spawnPos,
            audioManager: audioManager,
            size: new int[2] { 60, 60 },
            player: player,
            health: 400,
            color: Color.Red,
            srcRect: textureStore[0],
            collisionMap: collisionMap,
            visualSize: new int[2] { 64, 64 }
        )));
        // Initialize factory enemies
        factoryEnemies.AddRange(factoryEnemySpawns.Select(spawnPos => new FactoryEnemy(
            texture: enemySheet,
            position: spawnPos,
            audioManager: audioManager,
            size: new int[2] { 60, 60 },
            player: player,
            health: 200,
            color: Color.White,
            srcRect: textureStore[0],
            collisionMap: collisionMap,
            visualSize: new int[2] { 64, 64 }
        )));
        // Initialize frogs
        frogs.AddRange(frogSpawns.Select(spawnPos => new Frog(
            texture: miscSheet,
            position: spawnPos,
            audioManager: audioManager,
            size: new int[2] { 60, 60 },
            player: player,
            health: 100,
            color: Color.White,
            srcRect: textureStore[0],
            collisionMap: collisionMap,
            visualSize: new int[2] { 64, 64 }
        )));
        // Initialize patrol enemies from spawn positions
        enemies.AddRange(enemySpawns.Select(spawnPos => new Enemy(
            texture: enemySheet,
            position: spawnPos,
            audioManager: audioManager,
            size: new int[2] { 60, 60 },
            health: 100,
            color: Color.White,
            srcRect: textureStore[0],
            collisionMap: collisionMap,
            patrolSpeed: 1f,
            visualSize: new int[2] { 64, 64 },
            player: player
        )));
    }

    private void Reset()
    {
        // Reset player
        player.position = playerPos;
        player.Health = 100;
        player.Bullets.Clear();

        // Reset turret enemies
        orphanedTurretBullets.Clear();
        turretEnemies.Clear();
        turretEnemies.AddRange(turretEnemySpawns.Select(spawnPos => new TurretEnemy(
            texture: enemySheet,
            position: spawnPos,
            audioManager: audioManager,
            size: new int[2] { 60, 60 },
            player: player,
            health: 80,
            color: Color.White,
            srcRect: textureStore[1],
            collisionMap: collisionMap,
            visualSize: new int[2] { 64, 64 }
        )));
        // Reset berserk enemies
        berserkEnemies.Clear();
        berserkEnemies.AddRange(berserkEnemySpawns.Select(spawnPos => new BerserkEnemy(
            texture: enemySheet,
            position: spawnPos,
            audioManager: audioManager,
            size: new int[2] { 60, 60 },
            player: player,
            health: 400,
            color: Color.White,
            srcRect: textureStore[2],
            collisionMap: collisionMap,
            visualSize: new int[2] { 64, 64 }
        )));
        // Reset factory enemies
        factoryEnemies.Clear();
        factoryEnemies.AddRange(factoryEnemySpawns.Select(spawnPos => new FactoryEnemy(
            texture: enemySheet,
            position: spawnPos,
            audioManager: audioManager,
            size: new int[2] { 60, 60 },
            player: player,
            health: 200,
            color: Color.White,
            srcRect: textureStore[0],
            collisionMap: collisionMap,
            visualSize: new int[2] { 64, 64 }
        )));
        // Reset frogs
        frogs.Clear();
        frogs.AddRange(frogSpawns.Select(spawnPos => new Frog(
            texture: miscSheet,
            position: spawnPos,
            audioManager: audioManager,
            size: new int[2] { 60, 60 },
            player: player,
            health: 100,
            color: Color.White,
            srcRect: textureStore[0],
            collisionMap: collisionMap,
            visualSize: new int[2] { 64, 64 }
        )));
        // Reset patrol enemies
        enemies.Clear();
        enemies.AddRange(enemySpawns.Select(spawnPos => new Enemy(
            texture: enemySheet,
            position: spawnPos,
            audioManager: audioManager,
            size: new int[2] { 60, 60 },
            health: 100,
            color: Color.White,
            srcRect: textureStore[0],
            collisionMap: collisionMap,
            patrolSpeed: 1f,
            visualSize: new int[2] { 64, 64 },
            player: player
        )));

        // Reset goal trigger and level completion
        if (goalTrigger != null)
        {
            goalTrigger.Reset();
        }
        levelComplete = false;
        gameOver = false;

        // Reset inventory access trigger
        if (inventoryAccess != null)
        {
            inventoryAccess.Reset();
        }

        // Reset Doors
        foreach (var door in levelDoors)
        {
            door.Reset();
        }
    }

    public void Update(GameTime gameTime)
    {
        // Check if level is complete or game over
        if (levelComplete || gameOver)
        {
            return;
        }

        // Update player and camera
        player.Update(gameTime, camera.position);
        camera.Follow(player.Rectangle, new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight), new Vector2(tileMap.Keys.Max(k => k.X + 1) * 64, tileMap.Keys.Max(k => k.Y + 1) * 64));

        // Check all entities for out-of-bounds
        float worldMaxY = tileMap.Keys.Max(k => k.Y + 1) * 64;
        float worldMaxX = tileMap.Keys.Max(k => k.X + 1) * 64;

        // Check player bounds
        if (player.position.Y > worldMaxY)
        {
            player.HandleDeath();
        }
        else if (player.position.X < 0)
        {
            player.position.X = 0;
        }
        else if (player.position.X + player.size[0] > worldMaxX)
        {
            player.position.X = worldMaxX - player.size[0];
        }

        // Trigger game over if player died
        if (!player.IsAlive && !gameOver)
        {
            gameOver = true;
            sceneManager.AddScene(new GameOverScene(contentManager, sceneManager, audioManager, graphics, this));
            return;
        }

        // Check enemies for falling out of bounds
        foreach (var enemy in enemies)
        {
            if (enemy.position.Y > worldMaxY)
            {
                enemy.HandleDeath();
            }
            else if (enemy.position.X < 0)
            {
                enemy.position.X = 0;
            }
            else if (enemy.position.X + enemy.size[0] > worldMaxX)
            {
                enemy.position.X = worldMaxX - enemy.size[0];
            }
        }

        // Update alive patrol enemies
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive)
            {
                enemy.Update(gameTime);
            }
        }

        // Remove patrol enemies that died this frame
        enemies.RemoveAll(e => !e.IsAlive);

        // Check turret enemies for falling out of bounds
        foreach (var turret in turretEnemies)
        {
            if (turret.position.Y > worldMaxY)
            {
                turret.HandleDeath();
            }
        }
        // Check berserk enemies for falling out of bounds
        foreach (var berserk in berserkEnemies)
        {
            if (berserk.position.Y > worldMaxY)
            {
                berserk.HandleDeath();
            }
            else if (berserk.position.X < 0)
            {
                berserk.position.X = 0;
            }
            else if (berserk.position.X + berserk.size[0] > worldMaxX)
            {
                berserk.position.X = worldMaxX - berserk.size[0];
            }
        }

        // Check factory enemies for falling out of bounds
        foreach (var factory in factoryEnemies)
        {
            if (factory.position.Y > worldMaxY)
            {
                factory.HandleDeath();
            }
            else if (factory.position.X < 0)
            {
                factory.position.X = 0;
            }
            else if (factory.position.X + factory.size[0] > worldMaxX)
            {
                factory.position.X = worldMaxX - factory.size[0];
            }
        }

        // Check frogs for falling out of bounds
        foreach (var frog in frogs)
        {
            if (frog.position.Y > worldMaxY)
            {
                frog.HandleDeath();
            }
            else if (frog.position.X < 0)
            {
                frog.position.X = 0;
            }
            else if (frog.position.X + frog.size[0] > worldMaxX)
            {
                frog.position.X = worldMaxX - frog.size[0];
            }
        }

        // Update alive turret enemies
        foreach (var turret in turretEnemies)
        {
            if (turret.IsAlive)
            {
                turret.Update(gameTime);
            }
        }
        // Update alive berserk enemies
        foreach (var berserk in berserkEnemies)
        {
            if (berserk.IsAlive)
            {
                berserk.Update(gameTime);
            }
        }
        // Update alive factory enemies
        foreach (var factory in factoryEnemies)
        {
            if (factory.IsAlive)
            {
                factory.Update(gameTime);
            }
        }
        // Update alive frogs
        foreach (var frog in frogs)
        {
            if (frog.IsAlive)
            {
                frog.Update(gameTime);
            }
        }

        // Collect newly spawned enemies from factories
        foreach (var factory in factoryEnemies)
        {
            if (factory.SpawnedEnemies.Count > 0)
            {
                enemies.AddRange(factory.SpawnedEnemies);
                factory.SpawnedEnemies.Clear();
            }
        }

        // Update Doors Input
        foreach (var door in levelDoors)
        {
            door.Update();
        }

        //Update Inventory Access Input
        if (inventoryAccess != null)
        {
            inventoryAccess.Update();
            inventoryAccess.CheckTrigger(player.Rectangle);
            if (inventoryAccess.IsTriggered && inventoryAccess.IsActivated)
            {
                sceneManager.AddScene(new InventoryManagement(contentManager, sceneManager, audioManager, graphics, levelName));
            }
        }

        // Before removing dead turrets, rescue any live bullets they still own
        foreach (var turret in turretEnemies)
        {
            if (!turret.IsAlive)
            {
                orphanedTurretBullets.AddRange(turret.Bullets);
            }
        }

        // Before removing dead berserk enemies, rescue any live bullets they still own
        foreach (var berserk in berserkEnemies)
        {
            if (!berserk.IsAlive)
            {
                orphanedTurretBullets.AddRange(berserk.Bullets);
            }
        }

        // Remove turret enemies that died this frame
        turretEnemies.RemoveAll(t => !t.IsAlive);

        // Remove berserk enemies that died this frame
        berserkEnemies.RemoveAll(b => !b.IsAlive);

        // Remove factory enemies that died this frame
        factoryEnemies.RemoveAll(f => !f.IsAlive);

        // Remove frogs that died this frame
        frogs.RemoveAll(j => !j.IsAlive);

        // Advance and prune orphaned bullets
        for (int i = orphanedTurretBullets.Count - 1; i >= 0; i--)
        {
            orphanedTurretBullets[i].Update(gameTime);
            if (orphanedTurretBullets[i].Lifetime <= 0)
            {
                orphanedTurretBullets.RemoveAt(i);
            }
        }

        // Gather all entities for collision detection after all updates
        List<Entity> allEntities = [player, .. enemies, .. turretEnemies, .. berserkEnemies, .. factoryEnemies, .. frogs, .. player.Bullets, .. turretEnemies.SelectMany(t => t.Bullets), .. berserkEnemies.SelectMany(b => b.Bullets), .. orphanedTurretBullets];

        // Handle entity-to-entity collisions
        player.EntityCollisionUpdate(allEntities);
        foreach (var enemy in enemies)
        {
            enemy.EntityCollisionUpdate(allEntities);
        }
        foreach (var turret in turretEnemies)
        {
            turret.EntityCollisionUpdate(allEntities);
        }
        foreach (var berserk in berserkEnemies)
        {
            berserk.EntityCollisionUpdate(allEntities);
        }
        foreach (var factory in factoryEnemies)
        {
            factory.EntityCollisionUpdate(allEntities);
        }
        foreach (var frog in frogs)
        {
            frog.EntityCollisionUpdate(allEntities);
        }


        // Check for goal trigger
        if (goalTrigger != null && !levelComplete)
        {
            goalTrigger.CheckTrigger(player.Rectangle);
            if (goalTrigger.IsTriggered)
            {
                levelComplete = true;
                // Transition to win scene with current loadout
                sceneManager.AddScene(new WinScene(contentManager, sceneManager, audioManager, graphics, levelName, selectedItems, selectedAttachments));
            }
        }

        //Check if player enters door
        foreach (var door in levelDoors)
        {
            door.CheckTrigger(player.Rectangle);
            if (door.IsTriggered && door.IsActivated)
            {
                sceneManager.PopScene(this);
                sceneManager.AddScene(new GameScene(contentManager, sceneManager, audioManager, graphics, door.ConnectedLevel, selectedItems, selectedAttachments, door.LoadX, door.LoadY));
            }
        }
    }
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var tile in tileMap)
        {
            Rectangle dest = new Rectangle(
                (int)(tile.Key.X * 64 - camera.position.X),
                (int)(tile.Key.Y * 64 - camera.position.Y),
                64,
                64
            );
            Rectangle src = textureStore[tile.Value % textureStore.Count]; // Ensure we don't go out of bounds
            spriteBatch.Draw(textureSheet, dest, src, Color.White);
        }
        foreach (var enemy in enemies)
        {
            enemy.Draw(spriteBatch, camera.position);
        }
        foreach (var turret in turretEnemies)
        {
            turret.Draw(spriteBatch, camera.position);
        }
        foreach (var berserk in berserkEnemies)
        {
            berserk.Draw(spriteBatch, camera.position);
        }
        foreach (var factory in factoryEnemies)
        {
            factory.Draw(spriteBatch, camera.position);
        }
        foreach (var frog in frogs)
        {
            frog.Draw(spriteBatch, camera.position);
        }
        foreach (var bullet in orphanedTurretBullets)
        {
            bullet.Draw(spriteBatch, camera.position);
        }
        foreach (var door in levelDoors)
        {
            Rectangle doorRect = door.GetBounds();
            Rectangle doorDest = new Rectangle(
                (int)(doorRect.X - camera.position.X),
                (int)(doorRect.Y - camera.position.Y),
                doorRect.Width,
                doorRect.Height
            );
            Color doorColor = door.IsTriggered ? Color.Gold : Color.SaddleBrown;
            spriteBatch.Draw(textureSheet, doorDest, textureStore[0], doorColor * 0.5f);
        }

        //Draw inventory access trigger (if it exists)
        if (inventoryAccess != null)
        {
            Rectangle inventoryRect = inventoryAccess.GetBounds();
            Rectangle inventoryDest = new Rectangle(
                (int)(inventoryRect.X - camera.position.X),
                (int)(inventoryRect.Y - camera.position.Y),
                inventoryRect.Width,
                inventoryRect.Height
            );
            Color inventoryColor = inventoryAccess.IsTriggered ? Color.Gold : Color.SaddleBrown;
            spriteBatch.Draw(textureSheet, inventoryDest, textureStore[0], inventoryColor * 0.5f);
        }

        player.Draw(spriteBatch, camera.position);

        // Draw goal trigger (if it exists)
        if (goalTrigger != null)
        {
            Rectangle goalRect = goalTrigger.GetBounds();
            Rectangle goalDest = new Rectangle(
                (int)(goalRect.X - camera.position.X),
                (int)(goalRect.Y - camera.position.Y),
                goalRect.Width,
                goalRect.Height
            );
            // Draw goal with a green tint (using tile 0 or any appropriate texture)
            Color goalColor = goalTrigger.IsTriggered ? Color.Gold : Color.LimeGreen;
            spriteBatch.Draw(textureSheet, goalDest, textureStore[0], goalColor * 0.5f);
        }

        // Draw new GameUI (health, cooldown, and active items)
        gameUI.Draw(spriteBatch);
    }
}
