using System;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PolyGone
{

    class Bullet : Sprite
    {
        public float xSpeed;
        public readonly float ySpeed;
        public float Lifetime;
        public Bullet(Texture2D texture, Vector2 position, int[] size, float lifetime, Color color, float xSpeed, float ySpeed, Rectangle? srcRect = null)
            : base(texture, position, size, color, srcRect)
        {
            this.xSpeed = xSpeed;
            this.ySpeed = ySpeed;
            this.Lifetime = lifetime;
        }
        //Fire projectiles from blaster to global mouse position
        public override void Update(GameTime gameTime)
        {
            position.X += xSpeed;
            position.Y += ySpeed;
            base.Update(gameTime);
        }
    }
}
