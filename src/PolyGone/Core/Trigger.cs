using Microsoft.Xna.Framework;

namespace PolyGone;

public class Trigger
{
    public Vector2 Position { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    
    public Trigger(Vector2 position, int width, int height)
    {
        Position = position;
        Width = width;
        Height = height;
    }
    
    public Rectangle GetBounds()
    {
        return new Rectangle((int)Position.X, (int)Position.Y, Width, Height);
    }
    
    public bool IsTriggeredBy(Rectangle entityBounds)
    {
        return GetBounds().Intersects(entityBounds);
    }
}