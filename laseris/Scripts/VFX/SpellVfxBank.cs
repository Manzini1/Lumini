using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class SpellVfxBank : Resource
{
	[Export] public Godot.Collections.Array<SpellVfxEntry> Entries = new();

	private Dictionary<string, SpellVfxEntry> _map;

	public SpellVfxEntry Get(string spellId)
	{
		if (string.IsNullOrWhiteSpace(spellId)) return null;
		EnsureMap();
		spellId = spellId.Trim();
		return _map.TryGetValue(spellId, out var e) ? e : null;
	}

	private void EnsureMap()
	{
		if (_map != null) return;

		_map = new Dictionary<string, SpellVfxEntry>();
		foreach (var e in Entries)
		{
			if (e == null) continue;
			if (string.IsNullOrWhiteSpace(e.SpellId)) continue;

			var id = e.SpellId.Trim();
			if (_map.ContainsKey(id))
				GD.PushWarning($"[SpellVfxBank] SpellId duplicado: '{id}' (último sobrescreve).");

			_map[id] = e;
		}
	}

	public void InvalidateCache() => _map = null;
}
