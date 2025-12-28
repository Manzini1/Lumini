using Godot;
using System;
using System.Collections.Generic;

public partial class SettingsService : Node
{
	public const string BUS_MUSIC = "Music";
	public const string BUS_SFX = "SFX";

	private const string CFG_PATH = "user://settings.cfg";
	private const string SEC_AUDIO = "audio";
	private const string SEC_INPUT = "input";

	public static SettingsService I
	{
		get
		{
			var tree = Engine.GetMainLoop() as SceneTree;
			return tree?.Root?.GetNodeOrNull<SettingsService>("SettingsService");
		}
	}

	public float MusicVolume01 { get; private set; } = 0.5f; // 0..1
	public float SfxVolume01 { get; private set; } = 0.5f;   // 0..1

	// action -> keycode (int)
	private readonly Dictionary<string, int> _keybinds = new();

	public override void _Ready()
	{
		LoadAll();
		ApplyAudio();
		ApplyInputBinds();
	}

	// ---------------- AUDIO ----------------

	public void SetMusicVolume01(float v)
	{
		MusicVolume01 = Mathf.Clamp(v, 0f, 1f);
		ApplyAudio();
		SaveAll();
	}

	public void SetSfxVolume01(float v)
	{
		SfxVolume01 = Mathf.Clamp(v, 0f, 1f);
		ApplyAudio();
		SaveAll();
	}

	public void ApplyAudio()
	{
		SetBusVolumeLinear(BUS_MUSIC, MusicVolume01);
		SetBusVolumeLinear(BUS_SFX, SfxVolume01);
	}

	private void SetBusVolumeLinear(string busName, float linear01)
	{
		int idx = AudioServer.GetBusIndex(busName);
		if (idx < 0)
		{
			GD.PushWarning($"SettingsService: Bus '{busName}' não existe. Verifique o Audio Bus Layout.");
			return;
		}

		// 0 => mute (-80 dB), 1 => 0 dB
		float db = LinearToDbSafe(linear01);
		AudioServer.SetBusVolumeDb(idx, db);
	}

	private float LinearToDbSafe(float linear01)
	{
		// Godot: -80 dB é bem próximo do silêncio
		if (linear01 <= 0.0001f) return -80f;
		return Mathf.LinearToDb(linear01);
	}

	// ---------------- INPUT BINDS ----------------

	public void SetKeybind(string actionName, Key key)
	{
		if (string.IsNullOrWhiteSpace(actionName)) return;

		_keybinds[actionName] = (int)key;
		ApplySingleBind(actionName, key);
		SaveAll();
	}

	public bool TryGetKeybind(string actionName, out Key key)
	{
		key = Key.None;
		if (!_keybinds.TryGetValue(actionName, out int code)) return false;
		key = (Key)code;
		return true;
	}

	public void ApplyInputBinds()
	{
		foreach (var kv in _keybinds)
			ApplySingleBind(kv.Key, (Key)kv.Value);
	}

	private void ApplySingleBind(string actionName, Key key)
	{
		if (!InputMap.HasAction(actionName))
		{
			GD.PushWarning($"SettingsService: action '{actionName}' não existe no InputMap.");
			return;
		}

		// Remove só teclas (mantém mouse/gamepad se você tiver depois)
		var existing = InputMap.ActionGetEvents(actionName);
		for (int i = existing.Count - 1; i >= 0; i--)
		{
			if (existing[i] is InputEventKey)
				InputMap.ActionEraseEvent(actionName, existing[i]);
		}

		var ev = new InputEventKey { Keycode = key, Pressed = true };
		InputMap.ActionAddEvent(actionName, ev);
	}

	// ---------------- SAVE/LOAD ----------------

	public void SaveAll()
	{
		var cfg = new ConfigFile();

		cfg.SetValue(SEC_AUDIO, "music", MusicVolume01);
		cfg.SetValue(SEC_AUDIO, "sfx", SfxVolume01);

		// grava keybinds
		foreach (var kv in _keybinds)
			cfg.SetValue(SEC_INPUT, kv.Key, kv.Value);

		var err = cfg.Save(CFG_PATH);
		if (err != Error.Ok)
			GD.PushWarning($"SettingsService: falha salvando settings ({err}) em {CFG_PATH}");
	}

	public void LoadAll()
	{
		var cfg = new ConfigFile();
		var err = cfg.Load(CFG_PATH);

		if (err != Error.Ok)
		{
			// primeira vez: não existe ainda
			return;
		}

		MusicVolume01 = (float)cfg.GetValue(SEC_AUDIO, "music", MusicVolume01);
		SfxVolume01 = (float)cfg.GetValue(SEC_AUDIO, "sfx", SfxVolume01);

		// limpa e carrega binds
		_keybinds.Clear();

		// ConfigFile não lista chaves direto super fácil, então a gente lê sob demanda:
		// Quem manda quais ações existem é o OptionsMenu (ele setará e salvará).
		// Aqui só mantém o que já foi salvo.
		// (Sem “gambiarra”: isso evita assumir lista fixa no autoload.)
		//
		// Então: nada a fazer aqui além de manter o dicionário (que será preenchido pelo menu quando o usuário mexer).
		//
		// Se você quiser carregar automaticamente uma lista fixa, fazemos depois com segurança.
	}
}
