using Godot;

public partial class ElementOrbVfx : Node2D
{
	[ExportCategory("Refs")]
	[Export] public NodePath AnimPath = "Anim";

	[ExportCategory("Orbit (Fake 3D)")]
	[Export] public float RadiusX = 34f;     // largura da órbita
	[Export] public float RadiusY = 12f;     // altura da órbita (menor = mais “horizontal”)
	[Export] public float ScaleBack = 0.75f; // tamanho quando está “atrás”
	[Export] public float ScaleFront = 1.10f;// tamanho quando está “na frente”
	[Export] public float AlphaBack = 0.35f; // transparência quando está “atrás”
	[Export] public float AlphaFront = 1.0f; // opacidade quando está “na frente”
	[Export] public int ZRange = 30;         // quanto sobe/desce no ZIndex em relação ao inimigo

	public float Angle { get; set; } = 0f;

	private AnimatedSprite2D _anim;

	public override void _Ready()
	{
		_anim = GetNodeOrNull<AnimatedSprite2D>(AnimPath)
				?? GetNodeOrNull<AnimatedSprite2D>("Anim")
				?? GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		if (_anim == null)
		{
			GD.PushError($"{Name}: ElementOrbVfx não encontrou AnimatedSprite2D. Ajuste AnimPath.");
			QueueFree();
			return;
		}

		Visible = true;

		// importante: o ZIndex do orb é no Node2D root (este aqui)
		ZAsRelative = true;
	}

	public void Configure(ElementType element, SpriteFrames frames, string animName, float speedScale)
	{
		if (_anim == null) return;

		if (frames != null)
		{
			_anim.SpriteFrames = frames;
			_anim.SpeedScale = speedScale;

			if (!_anim.SpriteFrames.HasAnimation(animName))
				animName = _anim.SpriteFrames.HasAnimation("default")
					? "default"
					: _anim.SpriteFrames.GetAnimationNames()[0];

			_anim.Play(animName);
		}
		else
		{
			GD.PushWarning($"[Orb] frames null p/ {element}. (Bank/Entry não setado?)");
		}
	}

	/// centerLocal normalmente é Vector2.Zero se você instanciar o orb como filho do Anchor.
	/// baseEnemyZ é o ZIndex do sprite do inimigo (no seu caso 0).
	public void UpdateOrbitFake3D(Vector2 centerLocal, int baseEnemyZ)
	{
		float cos = Mathf.Cos(Angle);
		float sin = Mathf.Sin(Angle);

		// órbita “achatada” (horizontal)
		Position = centerLocal + new Vector2(cos * RadiusX, sin * RadiusY);

		// depth: 0 (atrás) -> 1 (frente)
		float depth01 = (sin + 1f) * 0.5f;

		// escala + alpha
		float s = Mathf.Lerp(ScaleBack, ScaleFront, depth01);
		Scale = new Vector2(s, s);

		if (_anim != null)
		{
			float a = Mathf.Lerp(AlphaBack, AlphaFront, depth01);
			var m = _anim.Modulate;
			m.A = a;
			_anim.Modulate = m;
		}

		// ZIndex: atrás negativo, frente positivo
		int dz = Mathf.RoundToInt(Mathf.Lerp(-ZRange, +ZRange, depth01));
		ZIndex = baseEnemyZ + dz;
	}
}
