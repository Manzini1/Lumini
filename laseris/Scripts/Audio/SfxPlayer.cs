using Godot;

public partial class SfxPlayer : Node
{
	[Export] public SpellAudioBank Bank;
	[Export] public AudioStreamPlayer Player;

	public override void _Ready()
	{
		if (Player == null)
			Player = GetNodeOrNull<AudioStreamPlayer>("AudioStreamPlayer");
	}

	public void PlaySpell(SpellDefinition spell)
	{
		if (spell == null || Bank == null || Player == null)
			return;

		// agora usa Id
		var stream = Bank.GetRandom(spell.Id);
		if (stream == null)
			return;

		Player.Stream = stream;
		Player.Play();
	}
}
