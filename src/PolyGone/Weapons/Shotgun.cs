using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Math = System.Math;
using System.Collections.Generic;
using PolyGone.Entities;

namespace PolyGone.Weapons
{
    class Shotgun : Blaster
    {
        public override float MaxCooldown => 30f; // Shotgun has longer cooldown
        
        public Shotgun(Texture2D texture, Vector2 position, int[] size, Color color, Dictionary<Vector2, int> collisionMap, List<Projectile> sharedBullets, Rectangle? srcRect = null)
            : base(texture, position, size, color, collisionMap, sharedBullets, srcRect)
        {
            Name = "Shotgun";
            Description = "Spread-fire weapon with multiple projectiles";
        }

        public override void Use()
        {
            // Handle shooting with spread using InputManager
            if (InputManager.IsLeftMouseButtonClicked() && cooldown <= 0f)
            {
                // Shotgun fires base pellets + any extras from MultiShotItem
                int pelletCount = 5 + ExtraBulletsPerShot;
                float spreadAngle = 0.6f; // Total spread in radians (about 34 degrees)
                float angleStep = spreadAngle / (pelletCount - 1);
                float startAngle = rotation - spreadAngle / 2;

                for (int i = 0; i < pelletCount; i++)
                {
                    float currentAngle = startAngle + (angleStep * i);
                    
                    bullets.Add(new Projectile(
                        texture: texture,
                        position: new Vector2(position.X + size[0] / 2f - 5f, position.Y + size[1] / 2f - 5f),
                        size: new int[2] { 8, 8 }, // Slightly smaller pellets
                        lifetime: 120f, // Shorter range than regular blaster
                        health: 1,
                        damage: 15, // Weaker than blaster but multiple pellets
                        color: Color.Orange, // Different color to distinguish
                        xSpeed: (float)(Math.Cos(currentAngle) * 600f), // Slightly slower
                        ySpeed: (float)(Math.Sin(currentAngle) * 600f),
                        owner: Owner.Player,
                        srcRect: srcRect,
                        collisionMap: collisionMap
                    ));
                }
                
                cooldown = 30f; // Slower fire rate (0.5 seconds at 60fps)
                InputManager.ConsumeClick(); // Prevent multiple shots from same click
            }
        }
    }
}
