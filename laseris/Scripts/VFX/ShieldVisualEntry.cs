using Godot;

[GlobalClass]
public partial class ShieldVisualEntry : Resource
{
	// Ex: "fire", "ice", "fire_lightning"
	[Export] public string Key = "";

	// SpriteFrames com animação (normalmente "default")
	[Export] public SpriteFrames Frames;

	// Nome da animação dentro do Frames (por padrão "default")
	[Export] public string AnimationName = "default";
}
