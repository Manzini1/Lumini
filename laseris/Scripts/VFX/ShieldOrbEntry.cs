using Godot;
using System;

[GlobalClass]
public partial class ShieldOrbEntry : Resource
{
	[Export] public ElementType Element;
	[Export] public SpriteFrames Frames;
	[Export] public string AnimationName = "play";
	[Export(PropertyHint.Range, "0.1,4.0,0.05")] public float SpeedScale = 1.0f;
}
