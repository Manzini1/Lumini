using Godot;

public partial class MusicDomainOnEnter : Node
{
	[Export] public MusicPlayer.Domain Domain = MusicPlayer.Domain.Menu;

	public override void _Ready()
	{
		MusicPlayer.I?.PlayDomain(Domain);
	}
}
