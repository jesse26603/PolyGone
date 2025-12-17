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
        public Player(Texture2D texture, Vector2 position, int[] size, Color color, List<Sprite> sprites) : base(texture, position, size, color)
        {
            this.sprites = sprites;
        }

        // Handle player movement and collisions
        private void Movement()
        {
            keyboardState = Keyboard.GetState();
            // Reset horizontal movement
            if (changeX > 0.5) changeX -= .2f;
            else if (changeX < -0.5) changeX += .2f;
            else changeX = 0f;

            // Handle horizontal input
            if (keyboardState.IsKeyDown(Keys.A))
            {
                changeX -= 1f;
                if (changeX < -5f) changeX = -5f;

            }
            if (keyboardState.IsKeyDown(Keys.D))
            {
                changeX += 1f;
                if (changeX > 5f) changeX = 5f;
            }

            // Apply gravity
            changeY += 0.5f; // Gravity effect
            if (changeY > 10f) changeY = 10f; // Terminal velocity

            // Check for collisions before moving
            bool isOnGround = false;
            bool canMoveHorizontally = true;

            // Try horizontal movement first
            if (changeX != 0)
            {
                Rectangle newHorizontalRect = new Rectangle((int)(position.X + changeX), (int)position.Y, size[0], size[1]);
                foreach (Sprite sprite in sprites)
                {
                    if (this != sprite && sprite.Rectangle.Intersects(newHorizontalRect))
                    {
                        canMoveHorizontally = false;
                        break;
                    }
                }
            }

            // Try vertical movement
            Rectangle newVerticalRect = new Rectangle((int)position.X, (int)(position.Y + changeY), size[0], size[1]);
            foreach (Sprite sprite in sprites)
            {
                if (this != sprite && sprite.Rectangle.Intersects(newVerticalRect))
                {
                    // Check if player is landing on top of the sprite
                    if (changeY > 0 && position.Y + size[1] <= sprite.position.Y + 5)
                    {
                        isOnGround = true;
                        position.Y = sprite.position.Y - size[1];
                        changeY = 0f;
                        break;
                    }
                    else
                    {
                        // Hit ceiling or side
                        changeY = 0f;
                        break;
                    }
                }
            }

            // Apply movements
            if (canMoveHorizontally)
            {
                position.X += changeX;
            }

            if (!isOnGround)
            {
                position.Y += changeY;
            }

            // Allow jumping when on ground
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
