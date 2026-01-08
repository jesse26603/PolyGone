using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System.Runtime.CompilerServices;

namespace PolyGone;

public class GameScene : IScene
{
    private ContentManager contentManager;
    private Texture2D texture;
    private SceneManager sceneManager;
    private Player player;
    private FollowCamera camera;
    private GraphicsDeviceManager graphics;
    private Dictionary<Vector2, int> tileMap;
    private Dictionary<Vector2, int> collisionMap;
    private List<Rectangle> textureStore;

    public GameScene(ContentManager contentManager, SceneManager sceneManager, GraphicsDeviceManager graphics)
    {
        this.contentManager = contentManager;
        this.sceneManager = sceneManager;
        this.graphics = graphics;
        this.tileMap = LoadMap("../../../Content/Maps/TestLevel._Tiles.csv");
        this.collisionMap = LoadMap("../../../Content/Maps/TestLevel._Collisions.csv");
        this.textureStore = GetTextureStore(32, new int[2] { 4, 4 });
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

    // Loads a tile map from a CSV file and returns a dictionary mapping tile positions to tile IDs
    public Dictionary<Vector2, int> LoadMap(string filepath)
        {
            Dictionary<Vector2, int> result = new();
            StreamReader reader = new(filepath);

            int y = 0;
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] items = line.Split(',');
                for (int x = 0; x < items.Length; x++)
                {
                    if (int.TryParse(items[x], out int value))
                    {
                        if (value > -1)
                        {
                            result[new Vector2(x, y)] = value;
                        }
                    }
                }

                y++;
            }

            return result;
        }

    public void Load()
    {
        // Load texture atlas and initialize camera
        texture = contentManager.Load<Texture2D>("PolyGoneTileMap");
        camera = new(new Vector2(0, 0));
        // Initialize player
        player = new Player(
            texture: texture,
            position: new Vector2(100, -100),
            size: new int[2] { 60, 60 },
            color: Color.White,
            srcRect: textureStore[1],
            collisionMap: collisionMap
        );
    }
    public void Update(GameTime gameTime)
    {
        // Update player and camera
        player.Update(gameTime);
        camera.Follow(player.Rectangle, new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));

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