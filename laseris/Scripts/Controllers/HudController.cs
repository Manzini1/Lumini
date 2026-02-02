using Godot;

namespace Game.UI;

public partial class HUDController : CanvasLayer
{
	private Label _phaseLabel;
	private ProgressBar _turnBar;
	private ProgressBar _flowBar;
private CanvasItem _enemyIntent;
	public HealthBarController MageHP { get; private set; }
	public HealthBarController EnemyHP { get; private set; }

public FlippingStoneController FlippingStone { get; private set; }
	private Control _ringsLayer;
	private RandomNumberGenerator _rng;

	public JudgementCornerController Judgement { get; private set; }
	public ElementBarController ElementBar { get; private set; }
	public FlowVialCircleController FlowVial { get; private set; }

	// =========================
	// Enemy Intent (Opcional)
	// =========================
	[ExportGroup("Enemy Intent (optional)")]
	[Export] public NodePath EnemyIntentPath = "Root/EnemyIntent"; // você pode setar manualmente no inspector
	[Export] public NodePath EnemyIntentModeIconPath = "ModeIcon";
	[Export] public NodePath EnemyIntentElementIconPath = "ElementIcon";

//	private Control _enemyIntent;
	private TextureRect _enemyIntentModeIcon;
	private TextureRect _enemyIntentElementIcon;

	[ExportGroup("Enemy Intent Textures (optional)")]
	[Export] public Texture2D EnemyIntentAttackTexture;  // sprite de "attack"
	[Export] public Texture2D EnemyIntentDefendTexture;  // sprite de "defend"
	[Export] public Texture2D[] ElementIconsById = new Texture2D[7]; // index 1..6 (deixa o 0 vazio)

	// =========================
	// Rings
	// =========================
	[ExportGroup("Rings")]
	[Export] public PackedScene AttackRingScene;
	[Export] public Vector2 RingSize = new Vector2(220, 220);
	[Export] public Vector2 Padding = new Vector2(20, 140);

	public override void _Ready()
	{
		Judgement = GetNodeOrNull<JudgementCornerController>("Root/JudgementCorner");
		_phaseLabel = GetNodeOrNull<Label>("Root/PhaseLabel");
		_turnBar = GetNodeOrNull<ProgressBar>("Root/TurnBar");
		_flowBar = GetNodeOrNull<ProgressBar>("Root/FlowBar");
		 FlowVial = GetNodeOrNull<FlowVialCircleController>("Root/FlowVial");
		FlippingStone = GetNodeOrNull<FlippingStoneController>("Root/FlippingStone");
		_ringsLayer = GetNodeOrNull<Control>("Root/RingsLayer");
		ElementBar = GetNodeOrNull<ElementBarController>("Root/ElementBar");

		_rng = new RandomNumberGenerator();
		_rng.Randomize();

		MageHP = GetNodeOrNull<HealthBarController>("Root/HPBars/MageHP");
		EnemyHP = GetNodeOrNull<HealthBarController>("Root/HPBars/EnemyHP");

		MageHP?.SetName("Mago");
		EnemyHP?.SetName("Inimigo");

		// =========================
		// Resolve EnemyIntent (robusto)
		// =========================
		ResolveEnemyIntent();

		// Se quiser, já começa escondido:
		SetEnemyIntentVisible(false);
	}

	private void ResolveEnemyIntent()
	{
		// 1) tenta pelo NodePath exportado
		_enemyIntent = GetNodeOrNull<CanvasItem>(EnemyIntentPath);

		// 2) fallback: caminhos comuns
		if (_enemyIntent == null)
			_enemyIntent = GetNodeOrNull<CanvasItem>("Root/EnemyIntent");

		if (_enemyIntent == null)
			_enemyIntent = GetNodeOrNull<Control>("EnemyIntent");

		// 3) fallback: procura por nome na árvore inteira
		if (_enemyIntent == null)
		{
		var found = FindChild("EnemyIntent", recursive: true, owned: false);
		_enemyIntent = found as CanvasItem;
		}

		if (_enemyIntent == null)
		{
			GD.PushWarning("HUDController: EnemyIntent NÃO encontrado em runtime. Vou imprimir os filhos do Root para diagnosticar.");

			var root = GetNodeOrNull<Node>("Root");
			if (root == null)
			{
				GD.PushWarning("HUDController: Node 'Root' NÃO existe em runtime. Isso indica que você está rodando outro HUD.tscn ou o Root tem outro nome.");
				PrintChildren(this, "HUD");
			}
			else
			{
				GD.Print($"[HUDController] Root path real: {root.GetPath()}");
				PrintChildren(root, "Root");
			}
			return;
		}

		// pega ModeIcon e ElementIcon dentro do EnemyIntent
		_enemyIntentModeIcon = _enemyIntent.GetNodeOrNull<TextureRect>(EnemyIntentModeIconPath);
		_enemyIntentElementIcon = _enemyIntent.GetNodeOrNull<TextureRect>(EnemyIntentElementIconPath);

		GD.Print($"[HUDController] EnemyIntent OK: {_enemyIntent.GetPath()}");
		GD.Print($"[HUDController] ModeIcon OK? {_enemyIntentModeIcon != null} | ElementIcon OK? {_enemyIntentElementIcon != null}");
	}

	private void PrintChildren(Node node, string label)
	{
		if (node == null) return;

		GD.Print($"--- Children of {label} ({node.GetPath()}) ---");
		foreach (var c in node.GetChildren())
		{
			if (c is Node cn)
				GD.Print($"  - {cn.Name}  type={cn.GetType().Name}  path={cn.GetPath()}");
		}
	}

	// =========================
	// Public API
	// =========================

	public void ShowJudgement(JudgementGrade grade)
	{
		Judgement?.Show(grade);
	}

	public void SetPhaseName(string name)
	{
		if (_phaseLabel != null) _phaseLabel.Text = name ?? "";
	}

	public void SetTurnProgress(double now, double start, double end)
	{
		if (_turnBar == null) return;

		double denom = System.Math.Max(0.0001, end - start);
		double t = (now - start) / denom;
		t = System.Math.Clamp(t, 0.0, 1.0);
		_turnBar.Value = t * 100.0;
	}
	public void OnJudgement(JudgementGrade grade)
{
	if (FlippingStone == null) return;

	if (grade == JudgementGrade.Perfect) FlippingStone.OnPerfect();
	else if (grade == JudgementGrade.Good) FlippingStone.OnGood();
	else FlippingStone.OnMiss();
}
	public void SetFlow(int stacks, int maxStacks)
	{
		if (_flowBar != null)
		{
			_flowBar.Value = (maxStacks <= 0) ? 0 : (double)stacks / maxStacks * 100.0;
		}

		float fill01 = (maxStacks <= 0) ? 0f : (float)stacks / maxStacks;
				GD.Print($"[HUD] Flow stacks={stacks} max={maxStacks} fill01={fill01:0.00}");
		FlowVial?.SetFill01(fill01);
	}

	public void SpawnRing(double startSec, double beatSec, double hitWindowSec)
	{
		if (AttackRingScene == null)
		{
			GD.PushError("HUDController: AttackRingScene não foi setado no Inspector.");
			return;
		}
		if (_ringsLayer == null)
		{
			GD.PushError("HUDController: Root/RingsLayer não encontrado.");
			return;
		}

		var inst = AttackRingScene.Instantiate();
		if (inst is not AttackRingController ring)
		{
			GD.PushError("HUDController: AttackRingScene não instancia AttackRingController.");
			inst.QueueFree();
			return;
		}

		_ringsLayer.AddChild(ring);

		ring.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		ring.CustomMinimumSize = RingSize;

		Vector2 vp = GetViewport().GetVisibleRect().Size;

		float minX = Padding.X;
		float minY = Padding.Y;
		float maxX = Mathf.Max(minX, vp.X - RingSize.X - Padding.X);
		float maxY = Mathf.Max(minY, vp.Y - RingSize.Y - Padding.X);

		float x = _rng.RandfRange(minX, maxX);
		float y = _rng.RandfRange(minY, maxY);

		ring.Position = new Vector2(x, y);
		ring.Arm(startSec, beatSec, hitWindowSec);
	}

	// =========================
	// Enemy Intent API
	// =========================

	public void SetEnemyIntentVisible(bool visible)
	{
		if (_enemyIntent == null) return;
		_enemyIntent.Visible = visible;
	}

	// true = ataque, false = defesa (se você quiser usar depois)
	public void SetEnemyIntentModeAttack(bool isAttack)
	{
		if (_enemyIntentModeIcon == null) return;

		if (isAttack && EnemyIntentAttackTexture != null)
			_enemyIntentModeIcon.Texture = EnemyIntentAttackTexture;
		else if (!isAttack && EnemyIntentDefendTexture != null)
			_enemyIntentModeIcon.Texture = EnemyIntentDefendTexture;
	}

	// elementId: 1..6
	public void SetEnemyIntentElement(int elementId)
	{
		if (_enemyIntentElementIcon == null) return;
		if (ElementIconsById == null || ElementIconsById.Length <= elementId) return;
		var tex = ElementIconsById[elementId];
		if (tex != null) _enemyIntentElementIcon.Texture = tex;
	}
}
