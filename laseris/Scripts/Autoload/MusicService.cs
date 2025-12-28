using Godot;
using System;
using System.Collections.Generic;

public partial class MusicService : Node
{
	[ExportCategory("Audio")]
	[Export] public string MusicBusName = SettingsService.BUS_MUSIC;

	private AudioStreamPlayer _player;

	private readonly Dictionary<MusicDomain, List<AudioStream>> _tracks = new();
	private readonly RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();

		_player = new AudioStreamPlayer();
		AddChild(_player);

		int busIdx = AudioServer.GetBusIndex(MusicBusName);
		if (busIdx >= 0) _player.Bus = MusicBusName;
		else GD.PushWarning($"MusicService: bus '{MusicBusName}' não existe (crie no Audio Bus Layout).");

		// ✅ Seus caminhos
		LoadDomain(MusicDomain.Menu, "res://Audio/Menu");
		LoadDomain(MusicDomain.Battle, "res://Audio/Battle");
		LoadDomain(MusicDomain.Merchant, "res://Audio/Merchant");

		// Boss (se não existir ainda, cai no warning e fica vazio)
		LoadDomain(MusicDomain.Boss, "res://Audio/Boss");
	}

	public void PlayDomainRandom(MusicDomain domain)
	{
		if (!_tracks.TryGetValue(domain, out var list) || list == null || list.Count == 0)
		{
			GD.PushWarning($"MusicService: sem músicas para domínio {domain}.");
			return;
		}

		int idx = _rng.RandiRange(0, list.Count - 1);
		_player.Stream = list[idx];
		_player.Play();
	}

	public void Stop() => _player?.Stop();

	private void LoadDomain(MusicDomain domain, string folder)
	{
		var list = new List<AudioStream>();

		var dir = DirAccess.Open(folder);
		if (dir == null)
		{
			GD.PushWarning($"MusicService: pasta não encontrada: {folder}");
			_tracks[domain] = list;
			return;
		}

		dir.ListDirBegin();
		while (true)
		{
			string file = dir.GetNext();
			if (string.IsNullOrEmpty(file)) break;
			if (dir.CurrentIsDir()) continue;

			string lower = file.ToLowerInvariant();
			if (!lower.EndsWith(".ogg") && !lower.EndsWith(".wav"))
				continue;

			string path = $"{folder}/{file}";
			var stream = GD.Load<AudioStream>(path);
			if (stream != null) list.Add(stream);
			else GD.PushWarning($"MusicService: falha ao carregar {path}");
		}
		dir.ListDirEnd();

		_tracks[domain] = list;
		GD.Print($"MusicService: {domain} -> {list.Count} tracks carregadas ({folder}).");
	}
}
