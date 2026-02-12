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

    public void Follow(Rectangle target, Vector2 screenSize, Vector2 worldSize)
    {
        position = new Vector2(
            target.X + target.Width / 2 - screenSize.X / 2,
            target.Y + target.Height / 2 - screenSize.Y / 2
        );
        position.X = MathHelper.Clamp(position.X, 0, worldSize.X - screenSize.X);
        position.Y = MathHelper.Clamp(position.Y, 0, worldSize.Y - screenSize.Y);
    }
};