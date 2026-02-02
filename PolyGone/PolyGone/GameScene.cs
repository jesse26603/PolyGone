using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Text.Json;
using System.Linq;
using System;

namespace PolyGone;

public class GameScene : IScene
{
    private ContentManager contentManager;
    private Texture2D texture;
    private SceneManager sceneManager;
    private Player player;
    private FollowCamera camera;
    private Blaster blaster;
    private readonly GraphicsDeviceManager graphics;
    private Dictionary<Vector2, int> tileMap;
    private Dictionary<Vector2, int> collisionMap;
    private List<Rectangle> textureStore;
    private Vector2 playerPos;
    private bool playerSpawnFound = false;
    private readonly List<Vector2> enemySpawns = new(); // Store enemy spawn positions
    private readonly List<Entity> enemies = new(); // Placeholder for enemy list

    public GameScene(ContentManager contentManager, SceneManager sceneManager, GraphicsDeviceManager graphics)
    {
        this.contentManager = contentManager;
        this.sceneManager = sceneManager;
        this.graphics = graphics;
        LoadMapFromJson("../../../Content/Maps/TestLevel.json");
        textureStore = GetTextureStore(32, new int[2] { 4, 4 });
    }

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
            string layerName = layer.GetProperty("name").GetString();
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
                        // Tiled uses 1-based indexing, convert to 0-based
                        if (layerName == "Tiles")
                        {
                            tileMap[new Vector2(x, y)] = tileValue - 1;
                        }
                        else if (layerName == "Collisions")
                        {
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
                    string objType = obj.GetProperty("type").GetString();
                    switch (objType)
                    {
                        case "PlayerSpawn":
                            playerPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            playerSpawnFound = true;
                            break;
                        case "EnemySpawn":
                            Vector2 enemyPos = AdjustCoordinates(
                                obj.GetProperty("x").GetSingle(),
                                obj.GetProperty("y").GetSingle()
                            );
                            enemySpawns.Add(enemyPos);
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
        // Load texture atlas and initialize camera
        texture = contentManager.Load<Texture2D>("PolyGoneTileMap");
        camera = new(new Vector2(0, 0));
        // Initialize blaster
        blaster = new Blaster(
            texture: texture,
            position: new Vector2(0, 0),
            size: new int[2] { 32, 32 },
            color: Color.White,
            srcRect: textureStore[1]
        );
        // Initialize player
        player = new Player(
            texture: texture,
            position: playerPos,
            size: new int[2] { 60, 60 },
            health: 100,
            color: Color.White,
            srcRect: textureStore[1],
            collisionMap: collisionMap,
            blaster: blaster
        );
        // Initialize enemies from spawn positions
        enemies.AddRange(enemySpawns.Select(spawnPos => new Entity(
            texture: texture,
            position: spawnPos,
            size: new int[2] { 60, 60 },
            health: 50,
            color: Color.White,
            srcRect: textureStore[2],
            collisionMap: collisionMap
        )));
    }
    public void Update(GameTime gameTime)
    {
        // Update player and camera
        player.Update(gameTime);
        camera.Follow(player.Rectangle, new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight), new Vector2( tileMap.Keys.Max(k => k.X + 1) * 64, tileMap.Keys.Max(k => k.Y + 1) * 64));
        
        // If player is outside world bounds, reset position
        if (player.position.Y > tileMap.Keys.Max(k => k.Y + 1) * 64)
        {
            player.position = new Vector2(100, -100);
        } else if (player.position.X < 0)
        {
            player.position.X = 0;
        } else if (player.position.X + player.size[0] > tileMap.Keys.Max(k => k.X + 1) * 64)
        {
            player.position.X = tileMap.Keys.Max(k => k.X + 1) * 64 - player.size[0];
        }
        player.blaster.Follow(player.Rectangle, camera.position); // Temporary Fix
        // Update enemies
        foreach (var enemy in enemies)
        {
            enemy.Update(gameTime);
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
            Rectangle src = textureStore[tile.Value];
            spriteBatch.Draw(texture, dest, src, Color.White);
        }
        foreach (var enemy in enemies)
        {
            enemy.Draw(spriteBatch, camera.position);
        }
        player.Draw(spriteBatch, camera.position);
    }
}
