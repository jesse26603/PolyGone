using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Math = System.Math;
using System.Collections.Generic;
using PolyGone.Entities;

namespace PolyGone.Weapons
{
    /// <summary>
    /// Void Lance — a slow-firing energy weapon that fires piercing bolts.
    /// Each bolt passes through every enemy it touches, dealing base blaster damage.
    /// The tradeoff: only one shot every ~0.8 seconds.
    /// </summary>
    class VoidLance : Blaster
    {
        public override float MaxCooldown => 50f; // Slow fire rate

        public VoidLance(Texture2D texture, Vector2 position, int[] size, Color color, Dictionary<Vector2, int> collisionMap, List<Projectile> sharedBullets, Rectangle? srcRect = null)
            : base(texture, position, size, color, collisionMap, sharedBullets, srcRect)
        {
            Name = "Void Lance";
            Description = "Fires a slow, piercing bolt that passes through enemies.";
        }

        public override void Use()
        {
            if (InputManager.IsLeftMouseButtonClicked() && cooldown <= 0f)
            {
                bullets.Add(new Projectile(
                    texture: texture,
                    position: new Vector2(position.X + size[0] / 2f - 7f, position.Y + size[1] / 2f - 7f),
                    size: new int[2] { 14, 14 }, // Bigger bolt
                    lifetime: 240f,              // Long range
                    health: 99,                  // Won't die from health damage
                    damage: 40,                  // Same as base blaster
                    color: new Color(180, 0, 220), // Deep violet
                    xSpeed: (float)(Math.Cos(rotation) * 700f),
                    ySpeed: (float)(Math.Sin(rotation) * 700f),
                    owner: Owner.Player,
                    srcRect: srcRect,
                    collisionMap: collisionMap,
                    isPiercing: true             // Passes through enemies
                ));

                // Extra bullets from MultiShotItem
                if (ExtraBulletsPerShot > 0)
                {
                    const float SpreadStep = 0.18f;
                    int half = ExtraBulletsPerShot / 2;
                    for (int i = 0; i < ExtraBulletsPerShot; i++)
                    {
                        float spreadOffset = (i - half + (ExtraBulletsPerShot % 2 == 0 ? 0.5f : 0f)) * SpreadStep;
                        float angle = rotation + spreadOffset;
                        bullets.Add(new Projectile(
                            texture: texture,
                            position: new Vector2(position.X + size[0] / 2f - 7f, position.Y + size[1] / 2f - 7f),
                            size: new int[2] { 14, 14 },
                            lifetime: 240f,
                            health: 99,
                            damage: 40,
                            color: new Color(180, 0, 220),
                            xSpeed: (float)(Math.Cos(angle) * 700f),
                            ySpeed: (float)(Math.Sin(angle) * 700f),
                            owner: Owner.Player,
                            srcRect: srcRect,
                            collisionMap: collisionMap,
                            isPiercing: true
                        ));
                    }
                }

                cooldown = MaxCooldown * CooldownMultiplier;
                InputManager.ConsumeClick();
            }
        }
    }
}
