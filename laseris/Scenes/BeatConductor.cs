using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class BeatConductor : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath MusicPlayerPath;

	[ExportCategory("Beatmap")]
	[Export(PropertyHint.File, "*.json")]
	public string BeatmapJsonPath = "";

	[ExportCategory("Timing")]
	[Export] public float PreHintSeconds = 0.12f;
	[Export] public float PerfectWindowSeconds = 0.06f;
	[Export] public float GoodWindowSeconds = 0.12f;

	public event Action<int> PreBeat; // index
	public event Action<int> Beat;    // index

	private AudioStreamPlayer _player;
	private List<float> _beats = new();

	private int _nextPre = 0;
	private int _nextBeat = 0;

	public bool IsPlaying => _player != null && _player.Playing;

	public override void _Ready()
	{
		_player = GetNodeOrNull<AudioStreamPlayer>(MusicPlayerPath);
		if (_player == null)
			GD.PushError("[BeatConductor] MusicPlayerPath inválido.");

		LoadBeatmap();
		ResetSchedule();
	}

	public void ResetSchedule()
	{
		_nextPre = 0;
		_nextBeat = 0;
	}

	public void Play()
	{
		if (_player == null) return;
		ResetSchedule();
		_player.Play();
	}

	public void Stop()
	{
		if (_player == null) return;
		_player.Stop();
		ResetSchedule();
	}

	public float SongTime()
	{
		if (_player == null) return 0f;
		// básico (ok pra agora)
		return (float)_player.GetPlaybackPosition();
	}

	public override void _Process(double delta)
	{
		if (_player == null || !_player.Playing) return;
		if (_beats.Count == 0) return;

		float t = SongTime();

		// Pre-beat
		while (_nextPre < _beats.Count && t >= (_beats[_nextPre] - PreHintSeconds))
		{
			PreBeat?.Invoke(_nextPre);
			_nextPre++;
		}

		// Beat
		while (_nextBeat < _beats.Count && t >= _beats[_nextBeat])
		{
			Beat?.Invoke(_nextBeat);
			_nextBeat++;
		}
	}

	public enum TimingGrade { Miss, Good, Perfect }

	/// Retorna (grade, beatIndex, offsetSeconds). offset >0 = você apertou atrasado.
	public (TimingGrade grade, int beatIndex, float offset) JudgeNow()
	{
		return JudgeAtTime(SongTime());
	}

	public (TimingGrade grade, int beatIndex, float offset) JudgeAtTime(float inputTime)
	{
		if (_beats.Count == 0) return (TimingGrade.Miss, -1, 999f);

		// pega o beat mais próximo entre o beat anterior e o próximo
		int iNext = Math.Clamp(_nextBeat, 0, _beats.Count - 1);
		int iPrev = Math.Clamp(iNext - 1, 0, _beats.Count - 1);

		float dPrev = inputTime - _beats[iPrev];
		float dNext = inputTime - _beats[iNext];

		int best = Math.Abs(dPrev) <= Math.Abs(dNext) ? iPrev : iNext;
		float offset = inputTime - _beats[best];
		float abs = Math.Abs(offset);

		if (abs <= PerfectWindowSeconds) return (TimingGrade.Perfect, best, offset);
		if (abs <= GoodWindowSeconds) return (TimingGrade.Good, best, offset);

		return (TimingGrade.Miss, best, offset);
	}

	private void LoadBeatmap()
	{
		_beats.Clear();

		if (string.IsNullOrWhiteSpace(BeatmapJsonPath))
		{
			GD.PushWarning("[BeatConductor] BeatmapJsonPath vazio.");
			return;
		}

		if (!FileAccess.FileExists(BeatmapJsonPath))
		{
			GD.PushError($"[BeatConductor] Beatmap não existe: {BeatmapJsonPath}");
			return;
		}

		string json = FileAccess.GetFileAsString(BeatmapJsonPath);

		try
		{
			var map = JsonSerializer.Deserialize<BeatMap>(json);
			if (map?.markers == null || map.markers.Count == 0)
			{
				GD.PushError("[BeatConductor] JSON sem markers.");
				return;
			}

			foreach (var m in map.markers)
				_beats.Add(m.time);

			_beats.Sort();
			GD.Print($"[BeatConductor] Loaded beats={_beats.Count} from {BeatmapJsonPath}");
		}
		catch (Exception ex)
		{
			GD.PushError("[BeatConductor] Falha parse JSON: " + ex.Message);
		}
	}

	// -------- JSON DTO ----------
	private sealed class BeatMap
	{
		public string songId { get; set; } = "";
		public string source { get; set; } = "";
		public List<BeatMarker> markers { get; set; } = new();
	}

	private sealed class BeatMarker
	{
		public string id { get; set; } = "";
		public string name { get; set; } = "";
		public float time { get; set; }
	}
}
