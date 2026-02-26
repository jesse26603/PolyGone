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
    // Map tileset tile IDs to collision types (after subtracting firstgid)
    // CollisionTiles.tsx defines: tile 0=Solid, tile 1=SemiSolid
    public static CollisionType GetCollisionType(int tileId)
    {
        return tileId switch
        {
            0 => CollisionType.Solid,
            1 => CollisionType.SemiSolid,
            2 => CollisionType.Slippery,
            3 => CollisionType.Rough,
            4 => CollisionType.OneWay,
            _ => CollisionType.None,
        };
    }
}
 