using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Math = System.Math;
using System.Collections.Generic;
using PolyGone.Entities;

namespace PolyGone.Weapons
{
    /// <summary>
    /// Fully automatic weapon - fires continuously while the mouse button is held.
    /// Very fast fire rate, but each burst deals low damage.
    /// </summary>
    class Automatic : Blaster
    {
        public override float MaxCooldown => 4f; // Very fast fire rate

        public Automatic(Texture2D texture, Vector2 position, int[] size, Color color, Dictionary<Vector2, int> collisionMap, List<Projectile> sharedBullets, Rectangle? srcRect = null)
            : base(texture, position, size, color, collisionMap, sharedBullets, srcRect)
        {
            Name = "Automatic";
            Description = "Hold to spray bullets. Fast rate, low damage per shot.";
        }

        public override void Use()
        {
            // Fires while mouse button is held - no ConsumeClick needed (hold-fire weapon)
            if (InputManager.IsLeftMouseButtonHeld() && cooldown <= 0f)
            {
                bullets.Add(new Projectile(
                    texture: texture,
                    position: new Vector2(position.X + size[0] / 2f - 5f, position.Y + size[1] / 2f - 5f),
                    size: new int[2] { 7, 7 },
                    lifetime: 160f,
                    health: 1,
                    damage: 15,
                    color: Color.Cyan,
                    xSpeed: (float)(Math.Cos(rotation) * 900f),
                    ySpeed: (float)(Math.Sin(rotation) * 900f),
                    owner: Owner.Player,
                    srcRect: srcRect,
                    collisionMap: collisionMap
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
                            position: new Vector2(position.X + size[0] / 2f - 5f, position.Y + size[1] / 2f - 5f),
                            size: new int[2] { 7, 7 },
                            lifetime: 160f,
                            health: 1,
                            damage: 15,
                            color: Color.Cyan,
                            xSpeed: (float)(Math.Cos(angle) * 900f),
                            ySpeed: (float)(Math.Sin(angle) * 900f),
                            owner: Owner.Player,
                            srcRect: srcRect,
                            collisionMap: collisionMap
                        ));
                    }
                }

                cooldown = MaxCooldown * CooldownMultiplier;
            }
        }
    }
}
