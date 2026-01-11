using Godot;

[GlobalClass]
public partial class SpellSfxEntry : Resource
{
	[ExportCategory("Identity")]
	[Export] public string SpellId = "";

	[ExportCategory("Clips")]
	[Export] public Godot.Collections.Array<AudioStream> ReleaseClips = new();
	[Export] public Godot.Collections.Array<AudioStream> ImpactClips = new();

	[ExportCategory("Mix")]
	[Export] public float VolumeDb = 0f;

	// Pitch “sempre igual”: deixa min=max=1
	[Export(PropertyHint.Range, "0.5,2.0,0.01")]
	public float PitchMin = 1.0f;

	[Export(PropertyHint.Range, "0.5,2.0,0.01")]
	public float PitchMax = 1.0f;

	[ExportCategory("Spam Control")]
	[Export(PropertyHint.Range, "0,2.0,0.01")]
	public float CooldownSeconds = 0.0f; // 0 = sem cooldown

	[Export] public bool AvoidRepeatLast = true;
}
