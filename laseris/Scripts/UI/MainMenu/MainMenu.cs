using Godot;

public partial class MainMenu : Control
{
	[ExportCategory("Buttons")]
	[Export] public NodePath BtnStartPath;
	[Export] public NodePath BtnTrainingPath;
	[Export] public NodePath BtnOptionsPath;
	[Export] public NodePath BtnQuitPath;

	private Button _btnStart;
	private Button _btnTraining;
	private Button _btnOptions;
	private Button _btnQuit;

	public override void _Ready()
	{
		_btnStart    = GetNodeOrNull<Button>(BtnStartPath);
		_btnTraining = GetNodeOrNull<Button>(BtnTrainingPath);
		_btnOptions  = GetNodeOrNull<Button>(BtnOptionsPath);
		_btnQuit     = GetNodeOrNull<Button>(BtnQuitPath);

		if (_btnStart == null)    GD.PushError("MainMenu: BtnStartPath não setado ou botão não encontrado.");
		if (_btnTraining == null) GD.PushError("MainMenu: BtnTrainingPath não setado ou botão não encontrado.");
		if (_btnOptions == null)  GD.PushError("MainMenu: BtnOptionsPath não setado ou botão não encontrado.");
		if (_btnQuit == null)     GD.PushError("MainMenu: BtnQuitPath não setado ou botão não encontrado.");

		if (_btnStart != null)    _btnStart.Pressed += OnStartPressed;
		if (_btnTraining != null) _btnTraining.Pressed += OnTrainingPressed;
		if (_btnOptions != null)  _btnOptions.Pressed += OnOptionsPressed;
		if (_btnQuit != null)     _btnQuit.Pressed += OnQuitPressed;
		GetNode<AudioManager>("/root/AudioManager").PlayMenuMusic();
	}

	private void OnStartPressed()
	{
		SceneRouter.I?.GoToRun();
	}

	private void OnTrainingPressed()
	{
		SceneRouter.I?.GoToTraining();
	}

	private void OnOptionsPressed()
	{
		SceneRouter.I?.GoToOptions();
	}

	private void OnQuitPressed()
	{
		GetTree().Quit();
	}
}
