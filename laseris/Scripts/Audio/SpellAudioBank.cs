using Godot;

[GlobalClass]
public partial class SpellAudioBank : Resource
{
	[Export] public Godot.Collections.Array<SpellSoundEntry> Entries = new();

	public AudioStream GetRandom(string spellId)
	{
		if (string.IsNullOrEmpty(spellId) || Entries == null)
			return null;

		foreach (var entry in Entries)
		{
			if (entry == null) continue;

			// ✅ aqui é SpellId (não Id)
			if (entry.SpellId != spellId) continue;

			if (entry.Sounds == null || entry.Sounds.Count == 0)
				return null;

			int idx = GD.RandRange(0, entry.Sounds.Count - 1);
			return entry.Sounds[idx];
		}

		return null;
	}
}
