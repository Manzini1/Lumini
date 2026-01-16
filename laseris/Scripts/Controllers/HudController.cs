using Godot;

namespace Game.UI;

public partial class HUDController : CanvasLayer
{
	private Label _phaseLabel;
	private ProgressBar _turnBar;
	private ProgressBar _flowBar;
	public HealthBarController MageHP { get; private set; }
	public HealthBarController EnemyHP { get; private set; }
	private Control _ringsLayer;
	private RandomNumberGenerator _rng;
	public JudgementCornerController Judgement { get; private set; }

	public ElementBarController ElementBar { get; private set; }

	[ExportGroup("Rings")]
	[Export] public PackedScene AttackRingScene;     // arraste res://Scenes/UI/AttackRing.tscn
	[Export] public Vector2 RingSize = new Vector2(220, 220);
	[Export] public Vector2 Padding = new Vector2(20, 140); // evita topo da HUD

	public override void _Ready()
	{
		Judgement = GetNode<JudgementCornerController>("Root/JudgementCorner");
		_phaseLabel = GetNode<Label>("Root/PhaseLabel");
		_turnBar = GetNode<ProgressBar>("Root/TurnBar");
		_flowBar = GetNode<ProgressBar>("Root/FlowBar");

		_ringsLayer = GetNode<Control>("Root/RingsLayer");
		ElementBar = GetNode<ElementBarController>("Root/ElementBar");

		_rng = new RandomNumberGenerator();
		_rng.Randomize();
		MageHP = GetNode<HealthBarController>("Root/HPBars/MageHP");
		EnemyHP = GetNode<HealthBarController>("Root/HPBars/EnemyHP");

		MageHP.SetName("Mago");
		EnemyHP.SetName("Inimigo");
	}
	public void ShowJudgement(JudgementGrade grade)
	{
		Judgement?.Show(grade);
	}
	public void SetPhaseName(string name) => _phaseLabel.Text = name ?? "";

	public void SetTurnProgress(double now, double start, double end)
	{
		double denom = System.Math.Max(0.0001, end - start);
		double t = (now - start) / denom;
		t = System.Math.Clamp(t, 0.0, 1.0);
		_turnBar.Value = t * 100.0;
	}

	public void SetFlow(int stacks, int maxStacks)
	{
		if (maxStacks <= 0) { _flowBar.Value = 0; return; }
		_flowBar.Value = (double)stacks / maxStacks * 100.0;
	}

	public void SpawnRing(double startSec, double beatSec, double hitWindowSec)
	{
		if (AttackRingScene == null)
		{
			GD.PushError("HUDController: AttackRingScene não foi setado no Inspector.");
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

		// define tamanho
		ring.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
		ring.CustomMinimumSize = RingSize;

		// posição aleatória dentro do viewport, com padding
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
}
