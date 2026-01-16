using Godot;
using System.Threading.Tasks;

namespace Game.Characters;

public partial class MageController : Node2D
{
	[Signal] public delegate void HealthChangedEventHandler(int current, int max);
	[Signal] public delegate void DiedEventHandler();

	[ExportGroup("Stats")]
	[Export] public int MaxHp = 100;

	[ExportGroup("Defense")]
	[Export] public float DefenseGraceSeconds = 0.05f; // tolerância visual pequena
	[Export] public bool DebugPrints = true;

	public int Hp { get; private set; }
	public bool IsDead => Hp <= 0;

	public bool IsDefenseWindowActive { get; private set; }
	public bool IsShieldActive { get; private set; } // usado pelo projétil / lógica

	public AnimatedSprite2D Sprite { get; private set; }
	public Marker2D WeaponSocket { get; private set; }
	public Marker2D BlockPoint { get; private set; }

	// ✅ token para cancelar timers antigos de defesa sem race condition
	private int _defenseToken = 0;

	public override void _Ready()
	{
		Sprite = GetNode<AnimatedSprite2D>("Sprite");
		WeaponSocket = GetNode<Marker2D>("WeaponSocket");
		BlockPoint = GetNode<Marker2D>("BlockPoint");

		Hp = MaxHp;
		EmitSignal(SignalName.HealthChanged, Hp, MaxHp);

		PlayIfExists("idle");
	}

	// =========================================================
	// Public API (usada pelo BattleController / Projectiles)
	// =========================================================

	public Vector2 GetWeaponSocketGlobal() => WeaponSocket.GlobalPosition;
	public Vector2 GetBlockPointGlobal() => BlockPoint.GlobalPosition;

	/// <summary>
	/// Abre a janela de defesa (escudo) por durationSeconds (+ grace).
	/// Chame isso no "prepare" do inimigo.
	/// </summary>
	public void ArmDefenseWindow(double durationSeconds)
	{
		_defenseToken++;
		int token = _defenseToken;

		IsDefenseWindowActive = true;
		IsShieldActive = true;

		// (opcional) animação de levantar escudo
		PlayIfExists("block");

		// encerra automaticamente (cancelável via token)
		_ = FinishDefenseAfter(token, (float)durationSeconds + DefenseGraceSeconds);

		if (DebugPrints)
			GD.Print($"[Mage] Defense window armed for {durationSeconds:0.000}s");
	}

	public void CancelDefenseWindow()
	{
		_defenseToken++; // invalida qualquer timer pendente
		IsDefenseWindowActive = false;
		IsShieldActive = false;
	}

	private async Task FinishDefenseAfter(int token, float seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);

		// se cancelou/rearmou, ignora
		if (token != _defenseToken) return;

		IsDefenseWindowActive = false;
		IsShieldActive = false;
		PlayIfExists("idle");
	}

	/// <summary>
	/// Chamado pelo projétil ou por qualquer sistema de dano.
	/// </summary>
	public void ApplyDamage(int amount)
	{
		if (IsDead) return;

		int dmg = Mathf.Max(0, amount);

		// se escudo estiver ativo, você pode decidir absorver aqui no futuro.
		// (por enquanto, se chegou aqui é porque não foi bloqueado)
		Hp = Mathf.Max(0, Hp - dmg);
		EmitSignal(SignalName.HealthChanged, Hp, MaxHp);

		if (DebugPrints)
			GD.Print($"[Mage] Took damage: {dmg}. HP={Hp}/{MaxHp}");

		PlayIfExists("hurt");

		if (Hp <= 0)
		{
			PlayIfExists("dead");
			EmitSignal(SignalName.Died);
		}
	}

	public void Heal(int amount)
	{
		if (IsDead) return;

		int heal = Mathf.Max(0, amount);
		Hp = Mathf.Min(MaxHp, Hp + heal);
		EmitSignal(SignalName.HealthChanged, Hp, MaxHp);

		if (DebugPrints)
			GD.Print($"[Mage] Healed: {heal}. HP={Hp}/{MaxHp}");
	}

	// =========================================================
	// Hooks usados pelo BattleController (feedback do ritmo)
	// =========================================================

	public void OnDefendSuccess()
	{
		if (DebugPrints) GD.Print("[Mage] Defend SUCCESS");
		// mantém escudo levantado, e dá um feedback se existir
		PlayIfExists("block"); // ou "parry" se você tiver
	}

	public void OnDefendFail()
	{
		if (DebugPrints) GD.Print("[Mage] Defend FAIL");
		// o dano normalmente vem do projétil
	}

	public void OnAttackSuccess()
	{
		if (DebugPrints) GD.Print("[Mage] Attack SUCCESS");
		PlayIfExists("attack");
	}

	public void OnAttackFail()
	{
		if (DebugPrints) GD.Print("[Mage] Attack FAIL");
		PlayIfExists("idle");
	}

	// =========================================================
	// Helpers
	// =========================================================

	private void PlayIfExists(string anim)
	{
		if (Sprite?.SpriteFrames == null) return;
		if (!Sprite.SpriteFrames.HasAnimation(anim)) return;

		// Evita restart desnecessário
		if (Sprite.Animation == anim && Sprite.IsPlaying()) return;

		Sprite.Play(anim);
	}
}
