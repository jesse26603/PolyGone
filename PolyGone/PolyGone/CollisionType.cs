namespace PolyGone;

public enum CollisionType
{
    None = 0,
    Solid = 1,      // Normal solid collision
    SemiSolid = 2,  // Drop-through platforms
    Slippery = 3,   // Slippery surface (e.g., ice)
    Rough = 4,     // Rough surface (e.g., mud)
    OneWay = 5,     // One-way platforms
}

public static class CollisionTypeMapper
{
    // Map Tiled tile IDs to collision types
    // Adjust these based on your Tiled collision tileset
    public static CollisionType GetCollisionType(int tileId)
    {
        return tileId switch
        {
            16 => CollisionType.Solid,
            17 => CollisionType.SemiSolid,
            18 => CollisionType.Slippery,
            19 => CollisionType.Rough,
            20 => CollisionType.OneWay,
            _ => CollisionType.None,
        };
    }
}
 