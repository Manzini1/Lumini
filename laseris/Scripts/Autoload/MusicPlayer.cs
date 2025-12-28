using Godot;
using System;
using System.Collections.Generic;

public partial class MusicPlayer : Node
{
	public enum Domain { None, Menu, Battle, Merchant, Boss }

	[ExportCategory("Folders")]
	[Export] public string MenuDir = "res://Audio/Menu";
	[Export] public string BattleDir = "res://Audio/Battle";
	[Export] public string MerchantDir = "res://Audio/Merchant";
	[Export] public string BossDir = "res://Audio/Boss";

	[ExportCategory("Behavior")]
	[Export] public string MusicBusName = SettingsService.BUS_MUSIC;
	[Export] public float FadeSeconds = 0.3f;

	private AudioStreamPlayer _player;
	private readonly RandomNumberGenerator _rng = new();

	private Domain _currentDomain = Domain.None;
	private string _currentTrackPath = "";

	public static MusicPlayer I
	{
		get
		{
			var tree = Engine.GetMainLoop() as SceneTree;
			return tree?.Root?.GetNodeOrNull<MusicPlayer>("MusicPlayer");
		}
	}

	public override void _Ready()
	{
		_rng.Randomize();

		_player = new AudioStreamPlayer();
		_player.Name = "MusicStream";
		AddChild(_player);

		_player.Bus = MusicBusName;
		_player.Autoplay = false;

		// aplica volume do SettingsService (se existir)
		SettingsService.I?.ApplyAudio();
	}

	public void PlayDomain(Domain domain)
	{
		if (domain == Domain.None)
			return;

		if (domain == _currentDomain && _player.Playing)
			return; // já tocando algo desse domínio

		var dir = DomainToDir(domain);
		if (string.IsNullOrWhiteSpace(dir))
			return;

		var tracks = ScanAudioFiles(dir);
		if (tracks.Count == 0)
		{
			GD.PushWarning($"MusicPlayer: Nenhuma música encontrada em {dir}");
			return;
		}

		// evitar repetir a mesma se possível
		string pick = tracks[_rng.RandiRange(0, tracks.Count - 1)];
		if (tracks.Count > 1)
		{
			int guard = 0;
			while (pick == _currentTrackPath && guard < 10)
			{
				pick = tracks[_rng.RandiRange(0, tracks.Count - 1)];
				guard++;
			}
		}

		_currentDomain = domain;
		_currentTrackPath = pick;

		PlayFile(pick);
	}

	private void PlayFile(string path)
	{
		var stream = GD.Load<AudioStream>(path);
		if (stream == null)
		{
			GD.PushWarning($"MusicPlayer: não consegui carregar {path}");
			return;
		}

		_player.Stream = stream;
		_player.Play();
		GD.Print($"[MUSIC] Playing: {path}");
	}

	private string DomainToDir(Domain d)
	{
		return d switch
		{
			Domain.Menu => MenuDir,
			Domain.Battle => BattleDir,
			Domain.Merchant => MerchantDir,
			Domain.Boss => BossDir,
			_ => ""
		};
	}

	private List<string> ScanAudioFiles(string dir)
	{
		var list = new List<string>();
		var da = DirAccess.Open(dir);
		if (da == null)
		{
			GD.PushWarning($"MusicPlayer: pasta não existe: {dir}");
			return list;
		}

		da.ListDirBegin();
		while (true)
		{
			var file = da.GetNext();
			if (file == "") break;
			if (da.CurrentIsDir()) continue;
			if (file.StartsWith(".")) continue;

			// Godot: OGG/WAV
			var lower = file.ToLowerInvariant();
			if (lower.EndsWith(".ogg") || lower.EndsWith(".wav"))
				list.Add($"{dir}/{file}");
		}
		da.ListDirEnd();

		return list;
	}
}
