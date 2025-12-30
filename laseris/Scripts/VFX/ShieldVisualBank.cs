using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class ShieldVisualBank : Resource
{
	[Export] public Godot.Collections.Array<ShieldVisualEntry> Entries = new();

	private Dictionary<string, ShieldVisualEntry> _map;

	public ShieldVisualEntry Get(string key)
	{
		if (string.IsNullOrWhiteSpace(key)) return null;

		if (_map == null)
			_map = BuildMap();

		_map.TryGetValue(key.Trim(), out var entry);
		return entry;
	}

	private Dictionary<string, ShieldVisualEntry> BuildMap()
	{
		var dict = new Dictionary<string, ShieldVisualEntry>();

		foreach (var e in Entries)
		{
			if (e == null) continue;
			if (string.IsNullOrWhiteSpace(e.Key)) continue;

			dict[e.Key.Trim()] = e;
		}

		return dict;
	}
}
