# PolyGone

A 2D platformer game built with MonoGame and C#.

## Description

PolyGone is a tile-based 2D platformer featuring:
- **Player character** with physics-based movement and shooting mechanics
- **Enemy AI** with intelligent behavior
- **Tile-based collision detection** using maps created in Tiled Map Editor
- **Scene management system** for game states
- **Camera system** that follows the player
- **Weapon system** with projectile mechanics

## Features

- Smooth platformer physics with coyote time and wall interactions
- Tile-based level design with collision detection
- Enemy entities with AI behavior
- Shooting mechanics with a blaster weapon
- Scene-based architecture for managing game states
- MonoGame content pipeline integration

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [MonoGame 3.8](https://www.monogame.net/) or later
- (Optional) [Tiled Map Editor](https://www.mapeditor.org/) for creating/editing levels

## Building and Running

### Clone the Repository

```bash
git clone https://github.com/jesse26603/PolyGone.git
cd PolyGone
```

### Build the Project

```bash
dotnet build PolyGone.sln
```

### Run the Game

```bash
dotnet run --project src/PolyGone/PolyGone.csproj
```

## Controls

- **Arrow Keys / WASD** - Move the player
- **Space** - Jump
- **Mouse** - Aim
- **Left Mouse Button** - Shoot

## Project Structure

```
PolyGone/
├── src/
│   └── PolyGone/           # Main game project
│       ├── Core/           # Core game systems (Game1, SceneManagement)
│       ├── Entities/       # Game entities (Player, Enemy, Entity base class)
│       ├── Graphics/       # Graphics components (Sprite, Camera)
│       ├── Scenes/         # Scene implementations
│       ├── Weapons/        # Weapon systems (Blaster)
│       └── Content/        # Game assets (textures, maps)
├── TiledAssets/            # Tiled map editor files
│   ├── TiledMaps/          # .tmx map files
│   ├── TiledSets/          # .tsx tileset files
│   └── TileMaps/           # Tilemap images
└── PolyGone.sln            # Visual Studio solution file
```

## Development

### Code Organization

The codebase is organized into logical folders:
- **Core**: Main game loop, program entry point, and scene management
- **Entities**: All game objects that exist in the world (player, enemies, projectiles)
- **Graphics**: Visual components like sprites and camera systems
- **Scenes**: Different game screens/states (game scene, exit scene)
- **Weapons**: Weapon systems and related mechanics

### Level Design

Levels are created using the [Tiled Map Editor](https://www.mapeditor.org/). Map files are stored in `TiledAssets/TiledMaps/` and exported to JSON format for use in the game.

## Technologies Used

- **C# / .NET 8.0** - Primary programming language and runtime
- **MonoGame 3.8** - Cross-platform game framework
- **Tiled Map Editor** - Level design tool

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [MonoGame](https://www.monogame.net/)
- Levels designed with [Tiled Map Editor](https://www.mapeditor.org/)
