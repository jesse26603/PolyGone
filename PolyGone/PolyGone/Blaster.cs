using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone
{
    class Blaster : Sprite
    {
        public float rotation = 0f;
        public Blaster(Texture2D texture, Vector2 position, int[] size, Color color, Rectangle? srcRect = null)
            : base(texture, position, size, color, srcRect)
        {
        }

        public void Follow(Rectangle target, Vector2 cameraOffset)
        {
            // Get mouse position
            MouseState mouseState = Mouse.GetState();
            Vector2 mousePosition = mouseState.Position.ToVector2();
            
            // Convert mouse position from screen space to world space
            Vector2 worldMousePosition = mousePosition + cameraOffset;

            // Calculate angle to mouse from player center
            Vector2 targetCenter = new Vector2(target.Center.X, target.Center.Y);
            float angle = (float)System.Math.Atan2(worldMousePosition.Y - targetCenter.Y, worldMousePosition.X - targetCenter.X);

            // Set rotation to point at mouse
            rotation = angle;

            // Position blaster in circle around target center
            float radius = 50f; // Adjust this value to change orbit distance
            position = targetCenter + new Vector2(
                (float)System.Math.Cos(angle) * radius,
                (float)System.Math.Sin(angle) * radius
            );
            
            // Offset to center the blaster sprite on its position
            position -= new Vector2(size[0] / 2f, size[1] / 2f);
        }

        public override void Update(GameTime gameTime)
        {
            // Blaster-specific update logic can go here

            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
        {
            // Draw the blaster with rotation around its center
            Vector2 origin = new Vector2(size[0] / 2f, size[1] / 2f);
            Vector2 drawPosition = position + offset + origin;
            spriteBatch.Draw(texture, drawPosition, srcRect, color, rotation, origin, 1f, SpriteEffects.None, 0f);
        }
    }
}