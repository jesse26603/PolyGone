// Ensure the working directory is the folder containing the EXE so that
// relative paths (e.g. Content/, Maps/) are resolved correctly when double-clicking.
System.Environment.CurrentDirectory = System.AppContext.BaseDirectory;

using var game = new PolyGone.Game1();
game.Run();
