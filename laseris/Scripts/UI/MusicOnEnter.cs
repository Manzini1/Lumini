using Godot;

public partial class MusicOnEnter : Node
{
	[Export] public MusicDomain Domain = MusicDomain.Menu;

	// Troca a música sempre que entra na cena
	[Export] public bool ForceNewRandomOnEnter = true;

	public override void _Ready()
	{
		var music = GetTree().Root.GetNodeOrNull<MusicService>("MusicService");
		if (music == null)
		{
			GD.PushWarning("MusicOnEnter: MusicService não encontrado (Autoload).");
			return;
		}

		if (ForceNewRandomOnEnter)
			music.PlayDomainRandom(Domain);
		else
			music.PlayDomainRandom(Domain); // por enquanto igual; depois dá pra otimizar
	}
}
