using Godot;

[GlobalClass]
public partial class ShieldVfxEntry : Resource
{
	[ExportCategory("Identity")]
	[Export] public ElementType Element;

	[ExportCategory("Aura")]
	[Export] public SpriteFrames Frames;
	[Export] public string AnimationName = "loop";
	[Export(PropertyHint.Range, "0.1,4.0,0.05")]
	public float SpeedScale = 1.0f;

	[ExportCategory("Render")]
	[Export] public int AuraZIndex = 20;
}
