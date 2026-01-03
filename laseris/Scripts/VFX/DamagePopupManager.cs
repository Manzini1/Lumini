using Godot;

public partial class DamagePopupManager : Node
{
	[Export] public PackedScene PopupScene; // DamagePopup.tscn
	[Export] public NodePath RootPath;      // aponta pro World/VfxRoot (ou outro root)
	[Export] public Vector2 Offset = new Vector2(0, -40);
	[Export] public int ZIndex = 200;

	private Node2D _root;

	public override void _Ready()
	{
		_root = GetNodeOrNull<Node2D>(RootPath);
		if (_root == null)
			GD.PushWarning("[DamagePopupManager] RootPath inválido. Vou usar CurrentScene.");
	}

	public void ShowFromOutcome(Enemy target, SpellDefinition spell, CastOutcome outcome)
	{
		if (PopupScene == null || target == null || !GodotObject.IsInstanceValid(target))
			return;

		// Decide texto/valor
		string text;
		Color color;
		float scale = 1.0f;

		switch (outcome)
		{
			case CastOutcome.Hit:
				text = $"-{spell.Damage}";
				color = new Color(1.0f, 0.35f, 0.2f); // laranja/vermelho
				break;

			case CastOutcome.Miss:
				text = "MISS";
				color = new Color(0.8f, 0.8f, 0.8f);
				break;

			case CastOutcome.Blocked:
				text = "BLOCK";
				color = new Color(0.6f, 0.75f, 1.0f);
				break;

			case CastOutcome.Absorbed50:
				text = $"+{spell.Damage / 2}";
				color = new Color(0.35f, 1.0f, 0.45f); // verde
				scale = 1.05f;
				break;

			case CastOutcome.Absorbed100:
				text = $"+{spell.Damage}";
				color = new Color(0.35f, 1.0f, 0.45f);
				scale = 1.10f;
				break;

			default:
				return;
		}

		var popup = PopupScene.Instantiate<DamagePopup>();

		Node parent = _root ?? (Node)GetTree().CurrentScene;
		parent.AddChild(popup);

		// spawn no marker do alvo (se existir)
		var marker = target.GetNodeOrNull<Marker2D>("VfxHead") ?? target.GetNodeOrNull<Marker2D>("VfxCenter");
		Vector2 pos = (marker != null ? marker.GlobalPosition : target.GlobalPosition) + Offset;

		popup.GlobalPosition = pos;
		popup.ZIndex = ZIndex;

		popup.Play(text, color, scale);
	}
}
