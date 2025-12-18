using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.IO;

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

    public void Load()
    {
        texture = contentManager.Load<Texture2D>("PolyGoneTileMap");
        camera = new(new Vector2(0, 0));
        sprites = new();
        for (int i = 0; i < 100; i++)
        {
            sprites.Add(new Sprite(
                texture: contentManager.Load<Texture2D>("ground"),
                position: new Vector2(i * 64, 300),
                size: new int[2] { 64, 64 },
                color: Color.White
            ));
        }

        for (int i = 0; i < 3; i++)
        {
            sprites.Add(new Sprite(
                texture: contentManager.Load<Texture2D>("ground"),
                position: new Vector2(400, 300 - (i + 1) * 64),
                size: new int[2] { 64, 64 },
                color: Color.White
            ));
        }

        sprites.Add(new Sprite(
            texture: contentManager.Load<Texture2D>("ground"),
            position: new Vector2(200, 200),
            size: new int[2] { 64, 64 },
            color: Color.White
        ));

        sprites.Add(new Sprite(
            texture: contentManager.Load<Texture2D>("ground"),
            position: new Vector2(264, 200),
            size: new int[2] { 64, 64 },
            color: Color.White
        ));

        player = new Player(
            texture: contentManager.Load<Texture2D>("player"),
            position: new Vector2(100, 100),
            size: new int[2] { 64, 64 },
            color: Color.White,
            sprites: sprites
        );
        sprites.Add(player);
    }
    public void Update(GameTime gameTime)
    {

        foreach (Sprite sprite in sprites)
        {
            sprite.Update(gameTime);
        }

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
    }
}