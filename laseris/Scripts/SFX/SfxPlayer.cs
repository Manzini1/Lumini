using Godot;
using System;
using System.Collections.Generic;

public partial class SfxPlayer : Node
{
	[ExportCategory("Bank")]
	[Export] public SpellSfxBank Bank;

	[ExportCategory("Fallback (se não achar entry)")]
	[Export] public AudioStream DefaultRelease;
	[Export] public AudioStream DefaultImpact;

	[ExportCategory("Output")]
	[Export] public string BusName = "Master";

	[ExportCategory("Pool")]
	[Export] public int MaxPlayers = 8; // quantos sons podem tocar ao mesmo tempo

	private readonly List<AudioStreamPlayer> _players = new();
	private readonly Dictionary<string, double> _lastPlay = new(); // cooldown por chave
	private readonly Dictionary<string, int> _lastIndex = new();   // evitar repetir

	private RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();
		
		// cria pool
		for (int i = 0; i < Math.Max(1, MaxPlayers); i++)
		{
			var p = new AudioStreamPlayer();
			p.Bus = BusName;
			AddChild(p);
			_players.Add(p);
		}
	}

	public void PlaySpell(SpellDefinition spell, SpellSfxCue cue)
	{
		if (spell == null) return;
		PlayId(spell.Id, cue);
	}

	public void PlayId(string spellId, SpellSfxCue cue)
	{
		if (string.IsNullOrWhiteSpace(spellId))
			return;
		
		SpellSfxEntry entry = Bank != null ? Bank.Get(spellId) : null;

		AudioStream stream = PickStream(entry, cue, spellId);
		if (stream == null) return;

		// cooldown
		double now = Time.GetTicksMsec() / 1000.0;
		string cdKey = $"{spellId}:{cue}";
		double cd = entry?.CooldownSeconds ?? 0.0f;

		if (cd > 0.0001f && _lastPlay.TryGetValue(cdKey, out var last) && (now - last) < cd)
			return;

		_lastPlay[cdKey] = now;

		// pega player livre
		var p = GetFreePlayer();
		if (p == null) return;

		p.Stream = stream;

		// aplica mix
		p.VolumeDb = entry?.VolumeDb ?? 0f;

		float pitch = 1.0f;
		if (entry != null)
		{
			float min = Mathf.Min(entry.PitchMin, entry.PitchMax);
			float max = Mathf.Max(entry.PitchMin, entry.PitchMax);
			pitch = Mathf.IsEqualApprox(min, max) ? min : _rng.RandfRange(min, max);
		}
		p.PitchScale = pitch;

		p.Play();
		 GD.Print($"[SFX] {spellId} cue={cue} pitch={pitch:0.00}");
	}

	private AudioStream PickStream(SpellSfxEntry entry, SpellSfxCue cue, string spellId)
	{
		Godot.Collections.Array<AudioStream> arr = null;
	
		if (entry != null)
		{
			arr = cue switch
			{
				SpellSfxCue.Release => entry.ReleaseClips,
				SpellSfxCue.Impact => entry.ImpactClips,
				_ => null
			};

			if (arr != null && arr.Count > 0)
			{
				int idx = PickIndex($"{spellId}:{cue}", arr.Count, entry.AvoidRepeatLast);
				return arr[idx];
			}
		}

		// fallback
		return cue switch
		{
			SpellSfxCue.Release => DefaultRelease,
			SpellSfxCue.Impact => DefaultImpact,
			_ => null
		};
	}

	private int PickIndex(string key, int count, bool avoidRepeat)
	{
		if (count <= 1) return 0;

		int idx = (int)_rng.RandiRange(0, count - 1);

		if (!avoidRepeat) return idx;

		if (_lastIndex.TryGetValue(key, out var last))
		{
			// tenta 3 vezes evitar repetir
			for (int i = 0; i < 3 && idx == last; i++)
				idx = (int)_rng.RandiRange(0, count - 1);
		}

		_lastIndex[key] = idx;
		return idx;
	}

	private AudioStreamPlayer GetFreePlayer()
	{
		foreach (var p in _players)
		{
			if (!p.Playing)
				return p;
		}

		// se todos estão tocando, “rouba” o primeiro
		if (_players.Count > 0)
		{
			_players[0].Stop();
			return _players[0];
		}

		return null;
	}
}
