using Godot;
using System.Threading.Tasks;

public partial class Luci : Node2D
{
	[Export] public NodePath AnimatedSpritePath { get; set; } = "AnimatedSprite2D";
	[Export] public string FirstAnimation { get; set; } = "Anim1";
	[Export] public string SecondAnimation { get; set; } = "Anim2";

	private AnimatedSprite2D _sprite;

	public override async void _Ready()
	{
		_sprite = GetNode<AnimatedSprite2D>(AnimatedSpritePath);

		// Garantir que a segunda vai ficar em loop (depende do setting do SpriteFrames)
		// O loop mesmo é configurado por animação no SpriteFrames, mas isso aqui "segura" a intenção.

		// 1) Toca a primeira e espera terminar
		_sprite.Play(FirstAnimation);
		await ToSignal(_sprite, AnimatedSprite2D.SignalName.AnimationFinished);

		// 2) Toca a segunda e deixa rodando
		_sprite.Play(SecondAnimation);
	}
}
