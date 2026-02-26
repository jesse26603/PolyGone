using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Math = System.Math;
using System.Collections.Generic;
using PolyGone.Entities;

namespace PolyGone.Weapons
{
    class Rifle : Blaster
    {
        public override float MaxCooldown => 60f; // Rifle has longer cooldown

        public Rifle(Texture2D texture, Vector2 position, int[] size, Color color, Dictionary<Vector2, int> collisionMap, List<Projectile> sharedBullets, Rectangle? srcRect = null)
            : base(texture, position, size, color, collisionMap, sharedBullets, srcRect)
        {
            Name = "Rifle";
            Description = "Long range weapon with high damage but slow fire rate.";
        }

        public override void Use()
        {
            // Handle shooting with spread using InputManager
            if (InputManager.IsLeftMouseButtonClicked() && cooldown <= 0f)
            {

                    bullets.Add(new Projectile(
                        texture: texture,
                        position: new Vector2(position.X + size[0] / 2f - 5f, position.Y + size[1] / 2f - 5f),
                        size: new int[2] { 11, 11 }, 
                        lifetime: 250, // Longer range than blaster
                        health: 3,
                        damage: 120, // Stronger than blaster
                        color: Color.Black, // Different color to distinguish
                        xSpeed: (float)(Math.Cos(rotation) * 3000), // Fast
                        ySpeed: (float)(Math.Sin(rotation) * 3000),
                        owner: Owner.Player,
                        srcRect: srcRect,
                        collisionMap: collisionMap
                    ));

                // Extra bullets from MultiShotItem
                if (ExtraBulletsPerShot > 0)
                {
                    const float SpreadStep = 0.15f;
                    int half = ExtraBulletsPerShot / 2;
                    for (int i = 0; i < ExtraBulletsPerShot; i++)
                    {
                        float spreadOffset = (i - half + (ExtraBulletsPerShot % 2 == 0 ? 0.5f : 0f)) * SpreadStep;
                        float angle = rotation + spreadOffset;
                        bullets.Add(new Projectile(
                            texture: texture,
                            position: new Vector2(position.X + size[0] / 2f - 5f, position.Y + size[1] / 2f - 5f),
                            size: new int[2] { 11, 11 },
                            lifetime: 250,
                            health: 3,
                            damage: 120,
                            color: Color.Black,
                            xSpeed: (float)(Math.Cos(angle) * 3000),
                            ySpeed: (float)(Math.Sin(angle) * 3000),
                            owner: Owner.Player,
                            srcRect: srcRect,
                            collisionMap: collisionMap
                        ));
                    }
                }

                cooldown = 60f; // Slower fire rate (1 seconds at 60fps)
                InputManager.ConsumeClick(); // Prevent multiple shots from same click
            }
        }
    }
}
