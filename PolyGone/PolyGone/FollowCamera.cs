using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PolyGone;

public class FollowCamera
{
    public Vector2 position;
    
    public FollowCamera(Vector2 position)
    {
        this.position = position;
    }

    public void Follow(Rectangle target, Vector2 screenSize)
    {
        MouseState mouseState = Mouse.GetState();
        Vector2 mousePosition = mouseState.Position.ToVector2();
        Vector2 offset = mousePosition - screenSize / 2;
        float distance = offset.Length();
        if (distance > 120)
        {
            offset = Vector2.Normalize(offset) * 120;
        }
        mousePosition = screenSize / 2 + offset;
        mousePosition.Y = MathHelper.Clamp(mousePosition.Y, screenSize.Y / 2 - 120, screenSize.Y / 2 + 120);
        position = new Vector2(
            target.X + target.Width / 2 - screenSize.X / 2 + (mousePosition.X - screenSize.X / 2) / 2,
            target.Y + target.Height / 2 - screenSize.Y / 2 + (mousePosition.Y - screenSize.Y / 2) / 2
        );
    }
};