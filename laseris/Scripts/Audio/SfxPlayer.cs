using Godot;
using System;

public partial class SfxPlayer : Node
{
	[ExportCategory("Refs")]
	[Export] public NodePath OneShotPath = "OneShot";

	private AudioStreamPlayer _oneShot;

	public override void _Ready()
	{
		_oneShot = GetNodeOrNull<AudioStreamPlayer>(OneShotPath);
		if (_oneShot == null)
			GD.PushWarning("SfxPlayer: OneShotPath não setado/encontrado.");
	}

	// Mais tarde vamos ligar isso ao SpellSfxBank (id -> som).
	// Por enquanto: método genérico.
	public void PlayStream(AudioStream stream, float volumeDb = 0f, float pitchScale = 1f)
	{
		if (_oneShot == null || stream == null) return;

		_oneShot.Stop();
		_oneShot.Stream = stream;
		_oneShot.VolumeDb = volumeDb;
		_oneShot.PitchScale = pitchScale;
		_oneShot.Play();
	}

	// Placeholder pro teu pipeline atual
	public void PlaySpell(SpellDefinition spell)
	{
		// Aqui você vai buscar no bank por spell.Id (depois)
		GD.Print($"[SFX] PlaySpell id={spell?.Id}");
	}
}
