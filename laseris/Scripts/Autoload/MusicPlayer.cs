using Godot;
using System;
using System.Collections.Generic;

public partial class MusicPlayer : Node
{
	[ExportCategory("Paths")]
	[Export] public string MenuDir = "res://Audio/Menu/";
	[Export] public string BattleDir = "res://Audio/Battle/";
	[Export] public string MerchantDir = "res://Audio/Merchant/";
	[Export] public string BossDir = "res://Audio/Boss/";

	[ExportCategory("Settings")]
	[Export] public string MusicBusName = SettingsService.BUS_MUSIC;

	private AudioStreamPlayer _player;

	private Node _lastScene;
	private MusicDomain _currentDomain = MusicDomain.None;

	// cache: domain -> lista de paths
	private readonly Dictionary<MusicDomain, List<string>> _pathsCache = new();

	// evita repetir imediatamente
	private readonly Dictionary<MusicDomain, int> _lastPickIndex = new();

	public override void _Ready()
	{
		// garante um AudioStreamPlayer
		_player = GetNodeOrNull<AudioStreamPlayer>("AudioStreamPlayer");
		if (_player == null)
		{
			_player = new AudioStreamPlayer();
			_player.Name = "AudioStreamPlayer";
			AddChild(_player);
		}

		_player.Bus = MusicBusName;

		// em C# no Godot 4: use TreeChanged e detecte mudança da CurrentScene
		GetTree().TreeChanged += OnTreeChanged;

		// já tenta tocar para a cena atual (caso já tenha entrado)
		CallDeferred(nameof(RefreshFromCurrentScene));
	}

	public override void _ExitTree()
	{
		if (GetTree() != null)
			GetTree().TreeChanged -= OnTreeChanged;
	}

	private void OnTreeChanged()
	{
		RefreshFromCurrentScene();
	}

	private void RefreshFromCurrentScene()
	{
		var scene = GetTree().CurrentScene;
		if (scene == null) return;

		// evita rodar toda hora se for a mesma cena
		if (scene == _lastScene) return;
		_lastScene = scene;

		// Convenção: a cena tem um child chamado "SceneConfig" com SceneMusicTag
		var tag = scene.GetNodeOrNull<SceneMusicTag>("SceneConfig");
		if (tag == null)
		{
			// sem tag = não mexe na música
			GD.Print($"[Music] Cena '{scene.Name}' sem SceneConfig(SceneMusicTag). Mantendo música atual.");
			return;
		}

		PlayDomain(tag.Domain);
	}

	public void PlayDomain(MusicDomain domain, bool forceRestart = false)
	{
		if (domain == MusicDomain.None) return;

		// se já está tocando esse domínio, não troca
		if (!forceRestart && domain == _currentDomain && _player.Playing)
			return;

		_currentDomain = domain;

		string dir = DomainToDir(domain);
		if (string.IsNullOrEmpty(dir))
		{
			GD.PushWarning($"[Music] Domínio '{domain}' sem diretório configurado.");
			return;
		}

		var paths = GetCachedPaths(domain, dir);
		if (paths.Count == 0)
		{
			GD.PushWarning($"[Music] Nenhuma música encontrada em: {dir}");
			return;
		}

		int pick = PickIndexAvoidRepeat(domain, paths.Count);
		string path = paths[pick];

		var stream = GD.Load<AudioStream>(path);
		if (stream == null)
		{
			GD.PushWarning($"[Music] Falha ao carregar AudioStream: {path}");
			return;
		}

		_player.Stop();
		_player.Stream = stream;
		_player.Play();

		GD.Print($"[Music] Domain={domain} -> {path}");
	}

	private string DomainToDir(MusicDomain domain)
	{
		return domain switch
		{
			MusicDomain.Menu => MenuDir,
			MusicDomain.Battle => BattleDir,
			MusicDomain.Merchant => MerchantDir,
			MusicDomain.Boss => BossDir,
			_ => ""
		};
	}

	private List<string> GetCachedPaths(MusicDomain domain, string dir)
	{
		if (_pathsCache.TryGetValue(domain, out var cached))
			return cached;

		var list = new List<string>();

		// garante barra no final
		if (!dir.EndsWith("/"))
			dir += "/";

		var da = DirAccess.Open(dir);
		if (da == null)
		{
			GD.PushWarning($"[Music] Não consegui abrir diretório: {dir}");
			_pathsCache[domain] = list;
			return list;
		}

		da.ListDirBegin();
		while (true)
		{
			var file = da.GetNext();
			if (string.IsNullOrEmpty(file))
				break;

			if (da.CurrentIsDir())
				continue;

			// aceita ogg/wav (Godot)
			string lower = file.ToLowerInvariant();
			if (lower.EndsWith(".ogg") || lower.EndsWith(".wav"))
				list.Add(dir + file);
		}
		da.ListDirEnd();

		_pathsCache[domain] = list;
		return list;
	}

	private int PickIndexAvoidRepeat(MusicDomain domain, int count)
	{
		if (count <= 1)
			return 0;

		int last = _lastPickIndex.TryGetValue(domain, out int v) ? v : -1;

		int pick = (int)GD.RandRange(0, count - 1);

		// tenta evitar repetição imediata
		if (pick == last)
		{
			pick++;
			if (pick >= count) pick = 0;
		}

		_lastPickIndex[domain] = pick;
		return pick;
	}
}
