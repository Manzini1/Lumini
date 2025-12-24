using Godot;

[GlobalClass]
public partial class SpellSoundEntry : Resource
{
	[Export] public string SpellId = ""; // ex: "Fire", "Lightning"
	[Export] public Godot.Collections.Array<AudioStream> Sounds = new();
}
