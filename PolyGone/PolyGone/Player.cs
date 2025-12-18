using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace PolyGone
{
    internal class Player : Sprite
    {
        private List<Sprite> sprites;
        private KeyboardState keyboardState;
        private float changeX;
        private float changeY;
        public Player(Texture2D texture, Vector2 position, int[] size, Color color, List<Sprite> sprites, Rectangle? srcRect = null) : base(texture, position, size, color, srcRect)
        {
            this.sprites = sprites;
        }

        // Handle player movement and collisions
        private void Movement()
        {
            keyboardState = Keyboard.GetState();

            // Handle horizontal input
            if (keyboardState.IsKeyDown(Keys.A))
            {
                changeX -= 1f;
                if (changeX < -5f) changeX = -5f;
            }
            else if (keyboardState.IsKeyDown(Keys.D))
            {
                changeX += 1f;
                if (changeX > 5f) changeX = 5f;
            }
            else
            {
                // Apply friction only when no input
                if (Math.Abs(changeX) > 0.5f) 
                    changeX *= 0.8f;
                else 
                    changeX = 0f;
            }

            // Apply gravity
            changeY += 0.5f;
            if (changeY > 10f) changeY = 10f;

            // Horizontal collision and movement
            bool horizontalCollision = false;
            float nextX = position.X + changeX;
            
            foreach (Sprite sprite in sprites)
            {
                if (this != sprite)
                {
                    Rectangle nextRect = new Rectangle((int)nextX, (int)position.Y, size[0], size[1]);
                    if (sprite.Rectangle.Intersects(nextRect))
                    {
                        horizontalCollision = true;
                        changeX = 0f;
                        break;
                    }
                }
            }
            
            if (!horizontalCollision)
            {
                position.X = nextX;
            }

            // Vertical collision and movement
            bool verticalCollision = false;
            bool isOnGround = false;
            float nextY = position.Y + changeY;
            
            foreach (Sprite sprite in sprites)
            {
                if (this != sprite)
                {
                    Rectangle nextRect = new Rectangle((int)position.X, (int)nextY, size[0], size[1]);
                    if (sprite.Rectangle.Intersects(nextRect))
                    {
                        verticalCollision = true;
                        
                        if (changeY > 0 && (int)position.Y + size[1] <= (int)sprite.position.Y + 5)
                        {
                            // Landing on top
                            isOnGround = true;
                            position.Y = sprite.position.Y - size[1];
                        }
                        else if (changeY < 0)
                        {
                            // Hit ceiling
                            position.Y = sprite.position.Y + sprite.size[1];
                        }
                        
                        changeY = 0f;
                        break;
                    }
                }
            }
            
            if (!verticalCollision)
            {
                position.Y = nextY;
            }

            // Round positions to prevent sub-pixel jittering
            position.X = (float)Math.Round(position.X);
            position.Y = (float)Math.Round(position.Y);

            // Jumping
            if (isOnGround && keyboardState.IsKeyDown(Keys.Space))
            {
                changeY = -12f;
            }
        }

        public override void Update(GameTime gameTime)
        {
            Movement();
            
            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
        {
            // Player-specific draw logic can go here

            base.Draw(spriteBatch, offset);
        }
    }
}
