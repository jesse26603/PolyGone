using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

public class GameScene : IScene
{
    private ContentManager contentManager;
    private Texture2D texture;
    private SceneManager sceneManager;
    private Sprite player;
    private List<Sprite> sprites;
    public GameScene(ContentManager contentManager, SceneManager sceneManager)
    {
        this.contentManager = contentManager;
        this.sceneManager = sceneManager;
    }

    public void Load()
    {
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
        // if (Keyboard.GetState().IsKeyDown(Keys.Space))
        // {
        //     sceneManager.AddScene(new ExitScene(contentManager));
        // }

        foreach (Sprite sprite in sprites)
        {
            sprite.Update(gameTime);
        }
    }
    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (Sprite sprite in sprites)
        {
            sprite.Draw(spriteBatch);
        }
    }
}