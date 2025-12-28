using Godot;

public partial class SceneRouter : Node
{
	// Caminhos oficiais das cenas (mantém estável)
	public const string SCENE_MAIN_MENU = "res://Scenes/UI/MainMenu.tscn";
	public const string SCENE_OPTIONS   = "res://Scenes/UI/OptionsMenu.tscn";
	public const string SCENE_TRAINING  = "res://Scenes/Training.tscn";
	public const string SCENE_RUN_GAME  = "res://Scenes/Game/RunGame.tscn";

	// Acesso fácil ao autoload
	public static SceneRouter I
	{
		get
		{
			var tree = Engine.GetMainLoop() as SceneTree;
			return tree?.Root?.GetNodeOrNull<SceneRouter>("SceneRouter");
		}
	}

	public void GoToMenu()    => ChangeTo(SCENE_MAIN_MENU);
	public void GoToOptions() => ChangeTo(SCENE_OPTIONS);
	public void GoToTraining()=> ChangeTo(SCENE_TRAINING);
	public void GoToRun()     => ChangeTo(SCENE_RUN_GAME);

	private void ChangeTo(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			GD.PushError("SceneRouter: path vazio.");
			return;
		}

		if (!ResourceLoader.Exists(path))
		{
			GD.PushError($"SceneRouter: cena não existe em: {path}");
			return;
		}

		var err = GetTree().ChangeSceneToFile(path);
		if (err != Error.Ok)
			GD.PushError($"SceneRouter: falha ao trocar cena ({err}) para: {path}");
	}
}
