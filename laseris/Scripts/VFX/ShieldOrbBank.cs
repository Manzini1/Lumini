using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class ShieldOrbBank : Resource
{
	[Export] public Godot.Collections.Array<ShieldOrbEntry> Entries = new();

	private Dictionary<ElementType, ShieldOrbEntry> _map;

	public ShieldOrbEntry Get(ElementType element)
	{
		EnsureMap();
		return _map.TryGetValue(element, out var e) ? e : null;
	}

	private void EnsureMap()
	{
		if (_map != null) return;

		_map = new Dictionary<ElementType, ShieldOrbEntry>();
		foreach (var e in Entries)
		{
			if (e == null) continue;
			_map[e.Element] = e;
		}
	}
}
