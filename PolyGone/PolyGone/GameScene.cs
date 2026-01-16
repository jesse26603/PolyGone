using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;

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
            position: new Vector2(100, -100),
            size: new int[2] { 60, 60 },
            color: Color.White,
            srcRect: textureStore[1],
            collisionMap: collisionMap,
            blaster: blaster
        );
    }
    public void Update(GameTime gameTime)
    {
        // Update player and camera
        player.Update(gameTime);
        camera.Follow(player.Rectangle, new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
        player.blaster.Follow(player.Rectangle, camera.position); // Temporary Fix
        // Handle scene switching
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
        player.Draw(spriteBatch, camera.position);
    }
}
