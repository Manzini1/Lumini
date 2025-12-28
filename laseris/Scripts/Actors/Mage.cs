using Godot;

public partial class Mage : Node2D
{
	[ExportCategory("Refs")]
	// Arraste aqui o node visual que você quer “dar scale” (normalmente o Sprite2D)
	[Export] public NodePath VisualPath;

	private Node2D _visual;      // <- TEM Scale
	private Tween _castTween;

	public override void _Ready()
	{
		// Se você setar VisualPath, ele usa. Se não, tenta achar um Sprite2D filho.
		_visual = GetNodeOrNull<Node2D>(VisualPath);

		if (_visual == null)
		{
			// fallback: procurar um Sprite2D em algum filho
			_visual = FindFirstChildOfType<Sprite2D>(this);
		}

		// Se ainda assim não achou, usa o próprio Mage (porque Mage é Node2D)
		if (_visual == null)
			_visual = this;
	}

	/// <summary>
	/// Vira a mage para a direção do alvo (esquerda/direita).
	/// </summary>
	public void FaceWorldPosition(Vector2 targetGlobalPos)
	{
		// Se o alvo está à esquerda, flip (escala X negativa).
		// Mantém o tamanho do boneco consistente.
		float dir = targetGlobalPos.X >= GlobalPosition.X ? 1f : -1f;

		var s = _visual.Scale;
		s.X = Mathf.Abs(s.X) * dir;
		_visual.Scale = s;
	}

	/// <summary>
	/// Feedback simples de cast: “incha e volta”.
	/// </summary>
	public void PlayCastFeedback()
	{
		if (_visual == null) return;

		_castTween?.Kill();
		_castTween = CreateTween();

		Vector2 baseScale = _visual.Scale;
		Vector2 upScale = baseScale * 1.10f;

		// sobe rápido, volta rápido
		_castTween.TweenProperty(_visual, "scale", upScale, 0.06f);
		_castTween.TweenProperty(_visual, "scale", baseScale, 0.08f);
	}

	// ---------- helper ----------
	private static T FindFirstChildOfType<T>(Node root) where T : Node
	{
		foreach (var childObj in root.GetChildren())
		{
			if (childObj is Node child)
			{
				if (child is T typed) return typed;

				var deeper = FindFirstChildOfType<T>(child);
				if (deeper != null) return deeper;
			}
		}
		return null;
	}
}
