using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class SpellVfxBank : Resource
{
	// Godot.Collections.Array aparece editável no Inspector
	[Export] public Godot.Collections.Array<SpellVfxEntry> Entries = new();

	private Dictionary<string, SpellVfxEntry> _map;

	public SpellVfxEntry Get(string spellId)
	{
		if (string.IsNullOrEmpty(spellId)) return null;

		EnsureMap();

		return _map.TryGetValue(spellId, out var entry) ? entry : null;
	}

	private void EnsureMap()
	{
		if (_map != null) return;

		_map = new Dictionary<string, SpellVfxEntry>();

		foreach (var e in Entries)
		{
			if (e == null) continue;
			if (string.IsNullOrWhiteSpace(e.SpellId)) continue;

			// Último ganha (se duplicar sem querer, pelo menos compila)
			_map[e.SpellId] = e;
		}
	}
}
