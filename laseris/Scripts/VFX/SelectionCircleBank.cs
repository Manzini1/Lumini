using Godot;

[GlobalClass]
public partial class SelectionCircleEntry : Resource
{
	[Export] public ElementType Element;
	[Export] public SpriteFrames Frames;
	[Export] public string AnimationName = "loop";
	[Export(PropertyHint.Range, "0.1,4.0,0.05")]
	public float SpeedScale = 1.0f;
}

[GlobalClass]
public partial class SelectionCircleBank : Resource
{
	[Export] public Godot.Collections.Array<SelectionCircleEntry> Entries = new();

	public SelectionCircleEntry Get(ElementType e)
	{
		for (int i = 0; i < Entries.Count; i++)
		{
			var it = Entries[i];
			if (it != null && it.Element == e) return it;
		}
		return null;
	}
}
