using Godot;
using System;

public partial class OptionsMenu : Control
{
	[ExportCategory("Refs")]
	[Export] public NodePath MusicSliderPath;
	[Export] public NodePath SfxSliderPath;
	[Export] public NodePath MusicValueLabelPath;
	[Export] public NodePath SfxValueLabelPath;

	[Export] public NodePath BtnRebindCastPath;
	[Export] public NodePath BtnRebindNextTargetPath;
	[Export] public NodePath BtnRebindUltimatePath;

	[Export] public NodePath BackButtonPath;

	[ExportCategory("Navigation")]
	[Export] public string BackScenePath = "res://Scenes/UI/MainMenu.tscn"; // ajuste pelo Inspector

	// Actions do InputMap
	private const string ACTION_CAST = "cast";
	private const string ACTION_NEXT_TARGET = "next_target";
	private const string ACTION_ULTIMATE = "ultimate";

	private HSlider _musicSlider;
	private HSlider _sfxSlider;
	private Label _musicValue;
	private Label _sfxValue;

	private Button _btnCast;
	private Button _btnNextTarget;
	private Button _btnUltimate;
	private Button _backBtn;

	private bool _waitingForKey = false;
	private string _waitingAction = null;
	private Button _waitingButton = null;

	public override void _Ready()
	{
		GD.Print("[OptionsMenu] _Ready");

		_musicSlider = GetNodeOrNull<HSlider>(MusicSliderPath);
		_sfxSlider = GetNodeOrNull<HSlider>(SfxSliderPath);
		_musicValue = GetNodeOrNull<Label>(MusicValueLabelPath);
		_sfxValue = GetNodeOrNull<Label>(SfxValueLabelPath);

		_btnCast = GetNodeOrNull<Button>(BtnRebindCastPath);
		_btnNextTarget = GetNodeOrNull<Button>(BtnRebindNextTargetPath);
		_btnUltimate = GetNodeOrNull<Button>(BtnRebindUltimatePath);
		_backBtn = GetNodeOrNull<Button>(BackButtonPath);

		// ---- valida refs (isso te diz o que está faltando) ----
		LogMissing(_musicSlider, nameof(MusicSliderPath));
		LogMissing(_sfxSlider, nameof(SfxSliderPath));
		LogMissing(_backBtn, nameof(BackButtonPath));

		// ---- init sliders ----
		if (_musicSlider != null)
		{
			// garante range 0..1
			_musicSlider.MinValue = 0;
			_musicSlider.MaxValue = 1;
			_musicSlider.Step = 0.01;

			_musicSlider.Value = SettingsService.I.MusicVolume01;
			_musicSlider.ValueChanged += OnMusicSliderChanged;

			UpdatePercentLabel(_musicValue, SettingsService.I.MusicVolume01);
		}

		if (_sfxSlider != null)
		{
			_sfxSlider.MinValue = 0;
			_sfxSlider.MaxValue = 1;
			_sfxSlider.Step = 0.01;

			_sfxSlider.Value = SettingsService.I.SfxVolume01;
			_sfxSlider.ValueChanged += OnSfxSliderChanged;

			UpdatePercentLabel(_sfxValue, SettingsService.I.SfxVolume01);
		}

		// ---- keybind buttons ----
		if (_btnCast != null) _btnCast.Pressed += () => BeginRebind(ACTION_CAST, _btnCast);
		if (_btnNextTarget != null) _btnNextTarget.Pressed += () => BeginRebind(ACTION_NEXT_TARGET, _btnNextTarget);
		if (_btnUltimate != null) _btnUltimate.Pressed += () => BeginRebind(ACTION_ULTIMATE, _btnUltimate);

		RefreshBindButtonText(ACTION_CAST, _btnCast);
		RefreshBindButtonText(ACTION_NEXT_TARGET, _btnNextTarget);
		RefreshBindButtonText(ACTION_ULTIMATE, _btnUltimate);

		// ---- back ----
		if (_backBtn != null)
			_backBtn.Pressed += OnBackPressed;
	}

	private void LogMissing(Node n, string fieldName)
	{
		if (n == null)
			GD.PushWarning($"[OptionsMenu] NodePath não setado ou node não encontrado: {fieldName}");
	}

	// ---------------- AUDIO ----------------

	private void OnMusicSliderChanged(double value)
	{
		float v = (float)value;
		GD.Print($"[OptionsMenu] Music slider -> {v:0.00}");
		SettingsService.I.SetMusicVolume01(v);
		UpdatePercentLabel(_musicValue, v);
	}

	private void OnSfxSliderChanged(double value)
	{
		float v = (float)value;
		GD.Print($"[OptionsMenu] SFX slider -> {v:0.00}");
		SettingsService.I.SetSfxVolume01(v);
		UpdatePercentLabel(_sfxValue, v);
	}

	private void UpdatePercentLabel(Label label, float v01)
	{
		if (label == null) return;
		int pct = Mathf.RoundToInt(v01 * 100f);
		label.Text = $"{pct}%";
	}

	// ---------------- KEYBINDS ----------------

	private void BeginRebind(string actionName, Button btn)
	{
		if (_waitingForKey) return;
		if (btn == null) return;

		_waitingForKey = true;
		_waitingAction = actionName;
		_waitingButton = btn;

		btn.Text = "Press a key...";
		btn.Disabled = true;
		SetBindButtonsEnabled(false, except: btn);
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (!_waitingForKey) return;

		if (@event is InputEventKey keyEv && keyEv.Pressed && !keyEv.Echo)
		{
			if (keyEv.Keycode == Key.Escape)
			{
				CancelRebind();
				return;
			}

			SettingsService.I.SetKeybind(_waitingAction, keyEv.Keycode);
			EndRebind();
		}
	}

	private void EndRebind()
	{
		_waitingForKey = false;

		RefreshBindButtonText(_waitingAction, _waitingButton);

		if (_waitingButton != null)
			_waitingButton.Disabled = false;

		SetBindButtonsEnabled(true);

		_waitingAction = null;
		_waitingButton = null;
	}

	private void CancelRebind()
	{
		_waitingForKey = false;

		RefreshBindButtonText(_waitingAction, _waitingButton);

		if (_waitingButton != null)
			_waitingButton.Disabled = false;

		SetBindButtonsEnabled(true);

		_waitingAction = null;
		_waitingButton = null;
	}

	private void SetBindButtonsEnabled(bool enabled, Button except = null)
	{
		void set(Button b)
		{
			if (b == null) return;
			if (b == except) return;
			b.Disabled = !enabled;
		}

		set(_btnCast);
		set(_btnNextTarget);
		set(_btnUltimate);
	}

	private void RefreshBindButtonText(string actionName, Button btn)
	{
		if (btn == null) return;

		if (SettingsService.I.TryGetKeybind(actionName, out var key))
		{
			btn.Text = key.ToString();
			return;
		}

		var events = InputMap.ActionGetEvents(actionName);
		foreach (var ev in events)
		{
			if (ev is InputEventKey k)
			{
				btn.Text = k.Keycode.ToString();
				return;
			}
		}

		btn.Text = "(none)";
	}

	// ---------------- NAV ----------------

	private void OnBackPressed()
	{
		GD.Print($"[OptionsMenu] Back -> {BackScenePath}");

		if (string.IsNullOrWhiteSpace(BackScenePath))
		{
			GD.PushWarning("[OptionsMenu] BackScenePath vazio.");
			return;
		}

		var err = GetTree().ChangeSceneToFile(BackScenePath);
		if (err != Error.Ok)
			GD.PushWarning($"[OptionsMenu] Falha ao trocar cena ({err}) para {BackScenePath}");
	}
}
