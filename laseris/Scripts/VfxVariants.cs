using Godot;

namespace Game.Vfx;

[GlobalClass]
public partial class VfxVariants : Resource
{
	[Export] public PackedScene[] Variants = System.Array.Empty<PackedScene>();
}
