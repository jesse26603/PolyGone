using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Math = System.Math;
using System.Collections.Generic;
using System.Linq;
using PolyGone.Items;
using PolyGone.Entities;

namespace PolyGone.Weapons
{
    public class Blaster : Item
    {
        public float rotation = 0f;
        protected readonly List<Projectile> bullets; // Reference to shared bullets list
        protected float cooldown;
        public float Cooldown => cooldown;
        public virtual float MaxCooldown => 12f; // Default blaster cooldown
        /// <summary>Extra bullets fired per shot (in addition to the base bullet). Set by MultiShotItem.</summary>
        public int ExtraBulletsPerShot { get; set; } = 0;
        /// <summary>Multiplier applied to the reset cooldown after firing. Set by RapidFireItem.</summary>
        public float CooldownMultiplier { get; set; } = 1f;
        protected readonly Dictionary<Vector2, int> collisionMap;
        public Blaster(Texture2D texture, Vector2 position, int[] size, Color color, Dictionary<Vector2, int> collisionMap, List<Projectile> sharedBullets, Rectangle? srcRect = null)
            : base(texture, position, size, color, "Blaster", "Basic energy weapon", srcRect)
        {
            this.bullets = sharedBullets; // Use shared bullets list
            this.cooldown = 0f;
            this.collisionMap = collisionMap;
        }

        public void Follow(Rectangle target, Vector2 cameraOffset)
        {
            // Get mouse position from InputManager
            Vector2 mousePosition = InputManager.GetMousePosition().ToVector2();

            // Convert mouse position from screen space to world space
            Vector2 worldMousePosition = mousePosition + cameraOffset;

            // Calculate angle to mouse from player center
            Vector2 targetCenter = new Vector2(target.Center.X, target.Center.Y);
            float angle = (float)Math.Atan2(worldMousePosition.Y - targetCenter.Y, worldMousePosition.X - targetCenter.X);

            // Set rotation to point at mouse
            rotation = angle;

            // Position blaster in circle around target center
            float radius = 50f; // Adjust this value to change orbit distance
            position = targetCenter + new Vector2(
                (float)Math.Cos(angle) * radius,
                (float)Math.Sin(angle) * radius
            );

            // Offset to center the blaster sprite on its position
            position -= new Vector2(size[0] / 2f, size[1] / 2f);
        }

        public override void Use()
        {
            // Handle shooting with InputManager to prevent click carryover
            if (InputManager.IsLeftMouseButtonClicked() && cooldown <= 0f)
            {
                // Central / base bullet
                bullets.Add(new Projectile(
                    texture: texture,
                    position: new Vector2(position.X + size[0] / 2f - 5f, position.Y + size[1] / 2f - 5f),
                    size: new int[2] { 10, 10 },
                    lifetime: 200f,
                    health: 1,
                    damage: 40,
                    color: Color.White,
                    xSpeed: (float)(Math.Cos(rotation) * 750f),
                    ySpeed: (float)(Math.Sin(rotation) * 750f),
                    owner: Owner.Player,
                    srcRect: srcRect,
                    collisionMap: collisionMap
                ));

                // Extra spread bullets added by MultiShotItem
                if (ExtraBulletsPerShot > 0)
                {
                    const float SpreadStep = 0.18f; // radians between each extra bullet
                    int half = ExtraBulletsPerShot / 2;
                    for (int i = 0; i < ExtraBulletsPerShot; i++)
                    {
                        float spreadOffset = (i - half + (ExtraBulletsPerShot % 2 == 0 ? 0.5f : 0f)) * SpreadStep;
                        float angle = rotation + spreadOffset;
                        bullets.Add(new Projectile(
                            texture: texture,
                            position: new Vector2(position.X + size[0] / 2f - 5f, position.Y + size[1] / 2f - 5f),
                            size: new int[2] { 10, 10 },
                            lifetime: 200f,
                            health: 1,
                            color: Color.White,
                            xSpeed: (float)(Math.Cos(angle) * 750f),
                            ySpeed: (float)(Math.Sin(angle) * 750f),
                            owner: Owner.Player,
                            srcRect: srcRect,
                            collisionMap: collisionMap
                        ));
                    }
                }

                cooldown = MaxCooldown * CooldownMultiplier;
                InputManager.ConsumeClick(); // Prevent multiple shots from same click
            }
        }

        public override void Update(GameTime gameTime)
        {
            // Update cooldown only - bullets are managed by Player
            cooldown = Math.Max(0f, cooldown - 1f);

            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, Vector2 offset)
        {
            // Draw the blaster with rotation around its center
            Vector2 origin = new Vector2(size[0] / 2f, size[1] / 2f);
            Vector2 drawPosition = position - offset + origin;
            spriteBatch.Draw(texture, drawPosition, srcRect, color, rotation, origin, 1f, SpriteEffects.None, 0f);
            
            // Bullets are drawn by Player
        }
    }
}