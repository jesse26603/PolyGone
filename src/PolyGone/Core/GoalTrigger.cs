using Microsoft.Xna.Framework;

namespace PolyGone;

public class GoalTrigger : Trigger
{
    public bool IsTriggered { get; private set; }
    
    public GoalTrigger(Vector2 position, int width, int height) 
        : base(position, width, height)
    {
        IsTriggered = false;
    }
    
    public void CheckTrigger(Rectangle playerBounds)
    {
        if (!IsTriggered && IsTriggeredBy(playerBounds))
        {
            IsTriggered = true;
        }
    }
    
    public void Reset()
    {
        IsTriggered = false;
    }
}