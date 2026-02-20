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

	public void PopScene(IScene scene)
	{
		if (sceneStack.Count == 0) { return ; }

		if (ReferenceEquals(sceneStack.Peek(), scene))
		{
			scene.Unload();
			sceneStack.Pop();
		}
	}

	public IScene GetCurrentScene()
	{
		return sceneStack.Peek();
	}
}