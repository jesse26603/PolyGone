using System.Collections.Generic;

namespace PolyGone;

public class SceneManager
{
	private readonly Stack<IScene> sceneStack;

	public SceneManager()
	{
		sceneStack = new();
	}

	public void AddScene(IScene scene)
	{
		scene.Load();
		sceneStack.Push(scene);
	}

	public void RemoveScene(IScene scene)
	{
        scene.Unload();

	public IScene GetCurrentScene()
	{
		return sceneStack.Peek();
	}
}