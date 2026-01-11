using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class SpellSfxBank : Resource
{
	[Export] public Godot.Collections.Array<SpellSfxEntry> Entries = new();

	private Dictionary<string, SpellSfxEntry> _map;

	public SpellSfxEntry Get(string spellId)
	{
		if (string.IsNullOrWhiteSpace(spellId)) return null;
		EnsureMap();
		spellId = spellId.Trim();
		return _map.TryGetValue(spellId, out var e) ? e : null;
	}

	private void EnsureMap()
	{
		if (_map != null) return;

		_map = new Dictionary<string, SpellSfxEntry>();
		foreach (var e in Entries)
		{
			if (e == null) continue;
			if (string.IsNullOrWhiteSpace(e.SpellId)) continue;

			var id = e.SpellId.Trim();
			if (_map.ContainsKey(id))
				GD.PushWarning($"[SpellSfxBank] SpellId duplicado: '{id}'. Último sobrescreve.");

			_map[id] = e;
		}
	}

	public void InvalidateCache() => _map = null;
}
