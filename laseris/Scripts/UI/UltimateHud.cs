using Godot;
using System.Collections.Generic;

public partial class UltimateHud : Control
{
	[ExportCategory("Nodes")]
	[Export] public NodePath RuneRowPath = "RuneRow";
	[Export] public NodePath TimerLabelPath = "TimerLabel";
	[Export] public NodePath CompanionPath = "Companion";

	[ExportCategory("Textures (assign in Inspector)")]
	[Export] public Texture2D FireTex;
	[Export] public Texture2D IceTex;
	[Export] public Texture2D LightningTex;
	[Export] public Texture2D PoisonTex;
	[Export] public Texture2D EarthTex;
	[Export] public Texture2D AirTex;
	[Export] public Texture2D LightTex;
	[Export] public Texture2D ShadowTex;

	private HBoxContainer _row;
	private Label _timerLabel;
	private CanvasItem _companion;

	private readonly List<TextureRect> _slots = new();

	public override void _Ready()
	{
		_row = GetNode<HBoxContainer>(RuneRowPath);
		_timerLabel = GetNode<Label>(TimerLabelPath);
		_companion = GetNodeOrNull<CanvasItem>(CompanionPath);

		_slots.Clear();
		foreach (var child in _row.GetChildren())
		{
			if (child is TextureRect tr)
				_slots.Add(tr);
		}

		HideHud();
	}

	public void ShowHud()
	{
		Visible = true;
		if (_companion != null) _companion.Visible = true;
	}

	public void HideHud()
	{
		Visible = false;
		if (_companion != null) _companion.Visible = false;
	}

	public void SetTimer(float secondsLeft)
	{
		if (_timerLabel == null) return;
		_timerLabel.Text = $"ULT: {secondsLeft:0.0}s";
	}

	public void SetSequence(IReadOnlyList<ElementType> seq)
	{
		// garante que temos slots suficientes
		for (int i = 0; i < _slots.Count; i++)
		{
			if (i < seq.Count)
			{
				_slots[i].Texture = GetTex(seq[i]);
				_slots[i].Modulate = Colors.White;
				_slots[i].Scale = Vector2.One;
				_slots[i].Visible = true;
			}
			else
			{
				_slots[i].Visible = false;
			}
		}
	}

	public void MarkCorrect(int index)
	{
		if (index < 0 || index >= _slots.Count) return;

		var slot = _slots[index];
		slot.Modulate = new Color(1.2f, 1.2f, 1.2f, 1f);

		// popzinho
		var t = CreateTween();
		t.TweenProperty(slot, "scale", new Vector2(1.15f, 1.15f), 0.06);
		t.TweenProperty(slot, "scale", Vector2.One, 0.06);
	}

	public void MarkFail(int index)
	{
		if (index < 0 || index >= _slots.Count) return;

		var slot = _slots[index];

		// 2 flashes vermelhos
		var t = CreateTween();
		t.TweenProperty(slot, "modulate", new Color(1f, 0.2f, 0.2f, 1f), 0.06);
		t.TweenProperty(slot, "modulate", Colors.White, 0.06);
		t.TweenProperty(slot, "modulate", new Color(1f, 0.2f, 0.2f, 1f), 0.06);
		t.TweenProperty(slot, "modulate", Colors.White, 0.06);
	}

	private Texture2D GetTex(ElementType e)
	{
		return e switch
		{
			ElementType.Fire => FireTex,
			ElementType.Ice => IceTex,
			ElementType.Lightning => LightningTex,
			ElementType.Poison => PoisonTex,
			ElementType.Earth => EarthTex,
			ElementType.Air => AirTex,
			ElementType.Light => LightTex,
			ElementType.Shadow => ShadowTex,
			_ => FireTex
		};
	}
}
