using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;
using System.Diagnostics;

namespace PolyGone;

public class GameScene : IScene
{
    private ContentManager contentManager;
    private Texture2D texture;
    private SceneManager sceneManager;
    private Sprite player;
    private FollowCamera camera;
    private List<Sprite> sprites;
    private GraphicsDeviceManager graphics;
    private Dictionary<Vector2, int> tileMap;
    private List<Rectangle> textureStore;
    private List<Rectangle> intersections;

    public GameScene(ContentManager contentManager, SceneManager sceneManager, GraphicsDeviceManager graphics)
    {
        this.contentManager = contentManager;
        this.sceneManager = sceneManager;
        this.graphics = graphics;
        this.tileMap = LoadMap("../../../Content/Maps/test.csv");
        this.textureStore = new()
        {
            new Rectangle(0, 0, 32, 32),   // Tile ID 1
            new Rectangle(32, 0, 32, 32),  // Tile ID 2
            new Rectangle(64, 0, 32, 32),  // Tile ID 3
            new Rectangle(96, 0, 32, 32),  // Tile ID 4
            new Rectangle(0, 32, 32, 32)   // Tile ID 5
        };
    }

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
                    if (value > 0)
                    {
                        result[new Vector2(x, y)] = value;
                    }
                }
            }

            y++;
        }

        return result;
    }

    public List<Rectangle> GetIntersectingTilesHorizontal(Rectangle rect)
    {
        List<Rectangle> intersectiions = new();
        int width = (rect.Width - (rect.Width % 64)) / 64;
        int height = (rect.Height - (rect.Height % 64)) / 64;

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                intersectiions.Add(new Rectangle(
                    (rect.X + x * 64) / 64,
                    (rect.Y + y * (64 - 1)) / 64,
                    64,
                    64
                ));
            }
        }
        return intersectiions;
    }

    public List<Rectangle> GetIntersectingTilesVertical(Rectangle rect)
    {
        List<Rectangle> intersectiions = new();
        int width = (rect.Width - (rect.Width % 64)) / 64;
        int height = (rect.Height - (rect.Height % 64)) / 64;

        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                intersectiions.Add(new Rectangle(
                    (rect.X + x * (64 - 1)) / 64,
                    (rect.Y + y * 64) / 64,
                    64,
                    64
                ));
            }
        }
        return intersectiions;
    }

    public void Load()
    {
        texture = contentManager.Load<Texture2D>("PolyGoneTileMap");
        camera = new(new Vector2(0, 0));
        intersections = new();

        player = new Player(
            texture: texture,
            position: new Vector2(100, 100),
            size: new int[2] { 64, 64 },
            color: Color.White,
            srcRect: textureStore[1],
            sprites: sprites
        );

        intersections = GetIntersectingTilesHorizontal(player.Rectangle);

        foreach (var rect in intersections)
        {
            Debug.WriteLine($"Intersecting Tile at: {rect.X}, {rect.Y}");
        }
        
        intersections = GetIntersectingTilesVertical(player.Rectangle);

        foreach (var rect in intersections)
        {
            Debug.WriteLine($"Intersecting Tile at: {rect.X}, {rect.Y}");
        }
    }
    public void Update(GameTime gameTime)
    {



        camera.Follow(player.Rectangle, new Vector2(graphics.PreferredBackBufferWidth, graphics.PreferredBackBufferHeight));
    }
    public void Draw(SpriteBatch spriteBatch)
    {
        // foreach (Sprite sprite in sprites)
        // {
        //     sprite.Draw(spriteBatch, camera.position);
        // }
        foreach (var tile in tileMap)
        {
            Rectangle dest = new Rectangle(
                (int)(tile.Key.X * 64 - camera.position.X),
                (int)(tile.Key.Y * 64 - camera.position.Y),
                64,
                64
            );
            Rectangle src = textureStore[tile.Value - 1];
            spriteBatch.Draw(texture, dest, src, Color.White);
        }
        player.Draw(spriteBatch, camera.position);
    }
}