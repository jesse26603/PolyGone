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

namespace PolyGone;

public class GameScene : IScene
{
    private ContentManager contentManager;
    private Texture2D texture;
    private SpriteFont hudFont;
    private SceneManager sceneManager;
    private Player player;
    private FollowCamera camera;
    private GameUI gameUI;
    private readonly GraphicsDeviceManager graphics;
    private Dictionary<Vector2, int> tileMap = null!;
    private Dictionary<Vector2, int> collisionMap = null!;
    private List<Rectangle> textureStore;
    private Vector2 playerPos;
    private bool playerSpawnFound = false;
    private readonly List<Vector2> enemySpawns = new(); // Store enemy spawn positions
    private readonly List<Entity> enemies = new(); // Placeholder for enemy list
    private GoalTrigger goalTrigger; // Win condition trigger
    private bool levelComplete = false;
    private readonly List<ItemType> selectedItems;
    private readonly WeaponType selectedWeapon;
    private readonly string levelName;

    public GameScene(ContentManager contentManager, SceneManager sceneManager, GraphicsDeviceManager graphics, string levelName = "TestLevel", List<ItemType>? selectedItems = null, WeaponType selectedWeapon = WeaponType.Blaster)
    {       
        this.contentManager = contentManager;
        this.sceneManager = sceneManager;
        this.graphics = graphics;
        this.selectedItems = selectedItems ?? new List<ItemType>(); // Default to empty list
        this.selectedWeapon = selectedWeapon;
        this.levelName = levelName;
        LoadMapFromJson("../../../Content/Maps/" + levelName + ".json");
        textureStore = GetTextureStore(32, new int[2] { 4, 4 });
    }

    // Public method to get the level name for restart functionality
    public string GetLevelName() => levelName;
    
    // Public methods to get the current loadout for restart functionality
    public List<ItemType> GetSelectedItems() => new List<ItemType>(selectedItems);
    public WeaponType GetSelectedWeapon() => selectedWeapon;

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
                        // Subtract the firstgid to get texture atlas index
                        // BasicTiles tileset has firstgid=17, so we subtract 17
                        if (layerName == "Tiles")
                        {
                            tileMap[new Vector2(x, y)] = tileValue - 17;
                        }
                        else if (layerName == "Collisions")
                        {
                            // CollisionTiles tileset has firstgid=1, so we subtract 1
                            collisionMap[new Vector2(x, y)] = tileValue - 1;
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
                            playerPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
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
                        case "Goal":
                            Vector2 goalPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            int goalWidth = (int)(obj.GetProperty("width").GetSingle() * 2);
                            int goalHeight = (int)(obj.GetProperty("height").GetSingle() * 2);
                            goalTrigger = new GoalTrigger(goalPos, goalWidth, goalHeight);
                            break;
                        default:
                            break;
                    }
                }
            } 
        }
        
        // Validate that a player spawn was found
        if (!playerSpawnFound)
        {
            throw new InvalidOperationException(
                $"Map file '{filepath}' is missing a required PlayerSpawn object in the Objects layer. " +
                "Please ensure the map contains exactly one object with type='PlayerSpawn'."
            );
        }
    }

    public void Load()
    {
        // Reset input state to prevent carried over clicks from triggering actions
        InputManager.ResetClickCooldown();
        
        // Load texture atlas and initialize camera
        texture = contentManager.Load<Texture2D>("PolyGoneTileMap");
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
            texture: texture,
            position: playerPos,
            size: new int[2] { 60, 60 },
            health: 100,
            color: Color.White,
            srcRect: textureStore[1],
            collisionMap: collisionMap,
            blasterTexture: texture,
            selectedItems: selectedItems,
            selectedWeapon: selectedWeapon,
            visualSize: new int[2] { 64, 64 }
        );
        
        // Initialize GameUI
        gameUI = new GameUI(player, texture, textureStore[1], hudFont);
        // Initialize enemies from spawn positions
        enemies.AddRange(enemySpawns.Select(spawnPos => new Enemy(
            texture: texture,
            position: spawnPos,
            size: new int[2] { 60, 60 },
            health: 50,
            color: Color.White,
            srcRect: textureStore[2],
            collisionMap: collisionMap,
            patrolSpeed: 1f,
            visualSize: new int[2] { 64, 64 }
        )));
    }
    
    private void Reset()
    {
        // Reset player
        player.position = playerPos;
        player.health = 100;
        player.bullets.Clear();
        
        // Reset enemies
        enemies.Clear();
        enemies.AddRange(enemySpawns.Select(spawnPos => new Enemy(
            texture: texture,
            position: spawnPos,
            size: new int[2] { 60, 60 },
            health: 50,
            color: Color.White,
            srcRect: textureStore[2],
            collisionMap: collisionMap,
            patrolSpeed: 1f,
            visualSize: new int[2] { 64, 64 }
        )));
        
        // Reset goal trigger and level completion
        if (goalTrigger != null)
        {
            goalTrigger.Reset();
        }
        levelComplete = false;
    }
    
    public void Update(GameTime gameTime)
    {
        // Check if level is complete
        if (levelComplete)
        {
            // Could transition to next level or exit scene here
            // For now, just prevent further updates
            return;
        }
        
        // Update player and camera
        player.Update(gameTime, camera.position);
        camera.Follow(player.Rectangle, new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight), new Vector2( tileMap.Keys.Max(k => k.X + 1) * 64, tileMap.Keys.Max(k => k.Y + 1) * 64));
        
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
        
        // Check enemies for falling out of bounds
        foreach (var enemy in enemies.ToList())
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
        
        // Update enemies
        foreach (var enemy in enemies)
        {
            enemy.Update(gameTime);
        }
        
        // Gather all entities for collision detection after all updates
        List<Entity> allEntities = [player, .. enemies, .. player.bullets];
        
        // Handle entity-to-entity collisions
        player.EntityCollisionUpdate(allEntities);
        foreach (var enemy in enemies)
        {
            // Only process collisions for enemies that are still alive
            if (enemy.health > 0)
            {
                enemy.EntityCollisionUpdate(allEntities);
            }
        }
        
        // Check for goal trigger
        if (goalTrigger != null && !levelComplete)
        {
            goalTrigger.CheckTrigger(player.Rectangle);
            if (goalTrigger.IsTriggered)
            {
                levelComplete = true;
                // Transition to win scene with current loadout
                sceneManager.AddScene(new WinScene(contentManager, sceneManager, graphics, levelName, selectedItems, selectedWeapon));
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
            spriteBatch.Draw(texture, dest, src, Color.White);
        }
        foreach (var enemy in enemies)
        {
            enemy.Draw(spriteBatch, camera.position);
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
            spriteBatch.Draw(texture, goalDest, textureStore[0], goalColor * 0.5f);
        }
        
        // Draw new GameUI (health, cooldown, and active items)
        gameUI.Draw(spriteBatch);
    }
}
