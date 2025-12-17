using Microsoft.Xna.Framework;

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
        position = new Vector2(
            target.X + target.Width / 2 - screenSize.X / 2,
            target.Y + target.Height / 2 - screenSize.Y / 2
        );
    }
};