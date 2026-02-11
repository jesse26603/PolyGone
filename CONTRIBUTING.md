# Contributing to PolyGone

Thank you for considering contributing to PolyGone! This document provides guidelines for contributing to the project.

## Code of Conduct

- Be respectful and constructive in all interactions
- Focus on what is best for the project and community
- Show empathy towards other community members

## How to Contribute

### Reporting Bugs

If you find a bug, please create an issue with:
- A clear, descriptive title
- Steps to reproduce the issue
- Expected behavior vs. actual behavior
- Screenshots or error messages (if applicable)
- Your environment (OS, .NET version, MonoGame version)

### Suggesting Enhancements

Enhancement suggestions are welcome! Please create an issue with:
- A clear description of the enhancement
- Why this enhancement would be useful
- Any implementation ideas you might have

### Pull Requests

1. **Fork** the repository
2. **Create a branch** from `main` for your feature or bugfix
   ```bash
   git checkout -b feature/your-feature-name
   ```
3. **Make your changes** following the code style guidelines below
4. **Test your changes** thoroughly
5. **Commit your changes** with clear, descriptive commit messages
   ```bash
   git commit -m "Add feature: description of what you added"
   ```
6. **Push** to your fork
   ```bash
   git push origin feature/your-feature-name
   ```
7. **Open a Pull Request** with:
   - A clear title and description
   - Reference to any related issues
   - Screenshots of any visual changes

## Development Setup

1. Install [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Clone the repository
3. Build the project: `dotnet build PolyGone.sln`
4. Run the game: `dotnet run --project src/PolyGone/PolyGone.csproj`

## Code Style Guidelines

### C# Coding Conventions

- Follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use **PascalCase** for class names, method names, and public properties
- Use **camelCase** for local variables and private fields
- Use **meaningful names** that describe the purpose
- Add **XML documentation comments** for public APIs

### File Organization

- Place files in the appropriate directory:
  - **Core/**: Core game systems and management
  - **Entities/**: Game objects and entities
  - **Graphics/**: Visual components
  - **Scenes/**: Game scenes and states
  - **Weapons/**: Weapon systems
- Keep files focused on a single responsibility
- Namespace should be `PolyGone` for all game code

### Formatting

- Use **4 spaces** for indentation (no tabs)
- Place opening braces on a new line (Allman style)
- Use a blank line between methods
- Limit line length to 120 characters where reasonable

### Comments

- Write clear, concise comments for complex logic
- Keep comments up-to-date with code changes
- Use XML documentation for public APIs
- Avoid obvious comments that just repeat the code

## Testing

- Test your changes thoroughly before submitting
- Ensure the game builds without errors
- Test gameplay functionality affected by your changes
- Check for any performance regressions

## Questions?

If you have questions about contributing, feel free to:
- Open an issue with the "question" label
- Reach out to the maintainers

Thank you for contributing to PolyGone! 🎮
