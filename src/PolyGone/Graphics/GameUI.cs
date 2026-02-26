using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PolyGone.Entities;
using PolyGone.Items;
using System.Collections.Generic;

namespace PolyGone.Graphics;

public class GameUI
{
    private readonly Player player;
    private readonly Texture2D itemIndicatorTexture;
    private readonly Rectangle srcRect;
    private readonly SpriteFont font;

    public GameUI(Player player, Texture2D itemIndicatorTexture, Rectangle srcRect, SpriteFont font)
    {
        this.player = player;
        this.itemIndicatorTexture = itemIndicatorTexture;
        this.srcRect = srcRect;
        this.font = font;
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        // Draw health bar
        DrawHealthBar(spriteBatch);

        // Draw cooldown indicator
        DrawCooldownIndicator(spriteBatch);

        // Draw active item indicators
        DrawActiveItems(spriteBatch);
    }

    private void DrawHealthBar(SpriteBatch spriteBatch)
    {
        int barWidth = 200;
        int barHeight = 20;
        int x = 20;
        int y = 20;

        // Border
        spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x - 2, y - 2, barWidth + 4, barHeight + 4), srcRect, Color.Black);

        // Background
        spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x, y, barWidth, barHeight), srcRect, Color.DarkRed);
        
        // Foreground (health)
        int healthWidth = (int)((player.health / (float)player.maxHealth) * barWidth);
        spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x, y, healthWidth, barHeight), srcRect, Color.Red);
        
    }

    private void DrawCooldownIndicator(SpriteBatch spriteBatch)
    {
        int size = 40;
        int x = 20;
        int y = 50;

        // Background
        spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x, y, size, size), srcRect, Color.DarkGray);
        
        // Border
        spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x - 2, y - 2, size + 4, size + 4), srcRect, Color.Black);
        
        // Icon
        if (player.GetBlaster() != null)
        {
            spriteBatch.Draw(player.GetBlaster().texture, new Rectangle(x + 4, y + 4, size - 8, size - 8), player.GetBlaster().srcRect, Color.White);
        }
        
        // Cooldown fill overlay (fits icon dimensions)
        if (player.GetBlaster() != null)
        {
            float maxCooldown = player.GetBlaster().MaxCooldown;
            int iconSize = size - 8;
            int cooldownHeight = (int)((player.Cooldown / maxCooldown) * iconSize);
            spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x + 4, y + 4 + (iconSize - cooldownHeight), iconSize, cooldownHeight), srcRect, Color.Blue * 0.6f);
        }
    }

    private void DrawActiveItems(SpriteBatch spriteBatch)
    {
        int startX = 20;
        int startY = 100;
        int itemSize = 40;
        int itemSpacing = 50;

        // Get all items from the player (all are active by default now)
        var items = player.GetAllItems();
        
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            int x = startX;
            int y = startY + (i * itemSpacing);

            // Background - always green since items are always active
            Color bgColor = Color.Green * 0.7f;
            spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x, y, itemSize, itemSize), srcRect, bgColor);

            // Item icon
            if (item.texture != null)
            {
                spriteBatch.Draw(item.texture, new Rectangle(x + 4, y + 4, itemSize - 8, itemSize - 8), item.srcRect, item.color);
            }

            // Border - always light green
            Color borderColor = Color.LightGreen;
            spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x - 2, y - 2, 2, itemSize + 4), srcRect, borderColor);
            spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x - 2, y - 2, itemSize + 4, 2), srcRect, borderColor);
            spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x + itemSize, y - 2, 2, itemSize + 4), srcRect, borderColor);
            spriteBatch.Draw(itemIndicatorTexture, new Rectangle(x - 2, y + itemSize, itemSize + 4, 2), srcRect, borderColor);

            // Item name with border
            if (font != null)
            {
                string itemName = item.Name;
                Vector2 namePosition = new Vector2(x + itemSize + 5, y + (itemSize / 2) - (font.LineSpacing / 2));
                
                // Draw black border (8 directions)
                spriteBatch.DrawString(font, itemName, namePosition + new Vector2(-1, -1), Color.Black);
                spriteBatch.DrawString(font, itemName, namePosition + new Vector2(0, -1), Color.Black);
                spriteBatch.DrawString(font, itemName, namePosition + new Vector2(1, -1), Color.Black);
                spriteBatch.DrawString(font, itemName, namePosition + new Vector2(-1, 0), Color.Black);
                spriteBatch.DrawString(font, itemName, namePosition + new Vector2(1, 0), Color.Black);
                spriteBatch.DrawString(font, itemName, namePosition + new Vector2(-1, 1), Color.Black);
                spriteBatch.DrawString(font, itemName, namePosition + new Vector2(0, 1), Color.Black);
                spriteBatch.DrawString(font, itemName, namePosition + new Vector2(1, 1), Color.Black);
                
                // Draw white text on top
                spriteBatch.DrawString(font, itemName, namePosition, Color.White);
            }
        }
    }
}
