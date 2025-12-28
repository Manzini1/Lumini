using Godot;

public partial class KeybindRow : HBoxContainer
{
	private Label _label;
	private Button _button;

	private string _action;
	private bool _listening;

	public override void _Ready()
	{
		_label = new Label();
		_label.CustomMinimumSize = new Vector2(220, 0);
		AddChild(_label);

		_button = new Button();
		AddChild(_button);

		_button.Pressed += OnPressed;
		SetProcessUnhandledInput(true);
	}

	public void Setup(string actionName)
	{
		_action = actionName;
		_label.Text = actionName;
		RefreshText();
	}

	private void OnPressed()
	{
		_listening = true;
		_button.Text = "Press a key...";
		_button.Disabled = true; // evita clicar de novo
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_listening) return;

		if (@event is InputEventKey k && k.Pressed && !k.Echo)
		{
			// Esc cancela
			if (k.Keycode == Key.Escape)
			{
				_listening = false;
				_button.Disabled = false;
				RefreshText();
				return;
			}

			SettingsService.I.SetKeybind(_action, k.Keycode);

			_listening = false;
			_button.Disabled = false;
			RefreshText();

			AcceptEvent();
		}
	}

	private void RefreshText()
	{
		_button.Text = GetCurrentKeyText(_action);
	}

	private static string GetCurrentKeyText(string actionName)
	{
		// prioriza o saved
		if (SettingsService.I.TryGetKeybind(actionName, out var saved) && saved != Key.None)
			return saved.ToString();

		// fallback: olha no InputMap
		var events = InputMap.ActionGetEvents(actionName);
		foreach (var e in events)
		{
			if (e is InputEventKey k)
				return k.Keycode.ToString();
		}
		return "Unbound";
	}
}
