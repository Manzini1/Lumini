using Godot;
using System;
using System.Collections.Generic;

using Game.Data;
using Game.UI;
using Game.Characters;
using Game.Combat;

namespace Game.Battle;

public partial class BattleController : Node2D
{
	[Export] public PhaseDefinition Phase;

	private AudioStreamPlayer _music;
	private BeatScheduler _beatScheduler;
	private TurnManager _turnManager;
	private InputJudge _inputJudge;
	private AttackPattern _pattern;
	private FlowMeter _flow;

	private HUDController _hud;
	private MageController _mage;
	private EnemyController _enemy;
	private Node2D _projectilesParent;

	// ===== Enemy Element System =====
	[ExportGroup("Enemy Element System")]
	[Export] public NodePath EnemyElementCyclePath = "World/Characters/Enemy/ElementCycle";
	private EnemyElementCycleController _enemyCycle;

	// ===== Refs / VFX =====
	private EnemyProtectionController _enemyProtection;
	private Marker2D _enemyGround;
	private Marker2D _enemyHit;
	private Marker2D _mageCast;
	private AnimationPlayer _mageAnim;

	[ExportGroup("Attack/Protection Refs")]
	[Export] public NodePath EnemyProtectionPath = "World/Characters/Enemy/Protection";
	[Export] public NodePath EnemyGroundPointPath = "World/Characters/Enemy/GroundPoint";
	[Export] public NodePath EnemyHitPointPath = "World/Characters/Enemy/HitPoint";
	[Export] public NodePath MageVfxCastPath = "World/Characters/Mage/VfxCast";
	[Export] public NodePath MageAnimPlayerPath = "World/Characters/Mage/AnimationPlayer";

	private float[] _beats;

	[ExportGroup("Judgement Damage")]
	[Export] public float PlayerGoodDamageMultiplier = 0.70f;

	[ExportGroup("Defense Feel (Late block)")]
	[Export] public bool AllowLatePerfectBlock = true;
	[Export] public bool AllowLateGoodBlock = true;
	[Export] public float LateBlockGraceSecondsPerfect = 0.06f;
	[Export] public float LateBlockGraceSecondsGood = 0.03f;

	private readonly Dictionary<int, JudgementGrade> _defenseGrade = new();
	private readonly Dictionary<int, JudgementGrade> _attackGrade = new();
	private readonly Dictionary<int, TurnSide> _beatOwner = new();

	private readonly Dictionary<int, int> _requiredElementByBeat = new();
	private readonly Dictionary<int, int> _enemyElementLockedByBeat = new();

	private readonly Dictionary<int, RhythmProjectile> _enemyProjectiles = new();

	private int _playerRequiredElementNow = -1;

	[ExportGroup("Damage")]
	[Export] public int PlayerBaseDamage = 12;

	[ExportGroup("Projectile Timing")]
	[Export] public float EnemyTravelToHitSeconds = 0.10f;
	[Export] public float EnemyHoldOnBlockSeconds = 0.12f;

	// Flow agora é: Perfect = +2, Good = +1, Miss = reset (ataque e defesa)
	[ExportGroup("Flow Points")]
	[Export] public int PerfectFlowGain = 2;
	[Export] public int GoodFlowGain = 1;

	[ExportGroup("Defense Rewards")]
	[Export] public float PerfectTurnReduceMult = 1.0f;
	[Export] public float GoodTurnReduceMult = 0.6f;

	[ExportGroup("Attack Circle")]
	[Export] public NodePath AttackCirclePath = "World/Vfx/AttackCircle";
	[Export] public NodePath ElementVfxLibraryPath = "Systems/ElementVfxLibrary";

	[ExportGroup("World VFX")]
	[Export] public NodePath WorldVfxParentPath = "World/Vfx";

	private AttackCircleController _attackCircle;
	private ElementVfxLibrary _vfxLib;

	private bool _broken;
	private int _enemyStanceElementNow = -1;

	[ExportGroup("Debug")]
	[Export] public bool DebugLogs = true;

	// =========================
	// TURN VISUALS (Overlay + Banner)
	// =========================
	[ExportGroup("Turn Visuals")]
	[Export] public NodePath BackgroundPath = "Background";     // Sprite2D
	[Export] public NodePath TurnBannerPath = "HUD/TurnBanner";  // TurnBanner (script)

	[Export] public Color NeutralBgModulate = new Color(1, 1, 1, 1);
	[Export] public Color AttackBgModulate = new Color(1.10f, 0.92f, 0.92f, 1);
	[Export] public Color DefendBgModulate = new Color(0.92f, 0.98f, 1.10f, 1);
	[Export] public float TurnOverlayTweenTime = 0.18f;

	private Sprite2D _background;
	private TurnBanner _turnBanner;
	private Tween _bgTween;

	// =========================
	// Turn transition safety
	// =========================
	[ExportGroup("Turn Transition Safety")]
	[Export] public bool DelayNewTurnCuesForFullLead = true;

	public override void _Ready()
	{
		_music = GetNodeOrNull<AudioStreamPlayer>("Systems/MusicPlayer");
		_beatScheduler = GetNodeOrNull<BeatScheduler>("Systems/BeatScheduler");
		_turnManager = GetNodeOrNull<TurnManager>("Systems/TurnManager");
		_inputJudge = GetNodeOrNull<InputJudge>("Systems/InputJudge");
		_pattern = GetNodeOrNull<AttackPattern>("Systems/AttackPattern");
		_flow = GetNodeOrNull<FlowMeter>("Systems/FlowMeter");

		_hud = GetNodeOrNull<HUDController>("HUD");
		_mage = GetNodeOrNull<MageController>("World/Characters/Mage");
		_enemy = GetNodeOrNull<EnemyController>("World/Characters/Enemy");
		_projectilesParent = GetNodeOrNull<Node2D>("World/Projectiles");

		if (Phase == null) { Fail("BattleController: Phase não setada no Inspector."); return; }
		if (_music == null) { Fail("BattleController: não achei Systems/MusicPlayer"); return; }
		if (_beatScheduler == null) { Fail("BattleController: não achei Systems/BeatScheduler"); return; }
		if (_turnManager == null) { Fail("BattleController: não achei Systems/TurnManager"); return; }
		if (_inputJudge == null) { Fail("BattleController: não achei Systems/InputJudge"); return; }
		if (_pattern == null) { Fail("BattleController: não achei Systems/AttackPattern"); return; }
		if (_flow == null) { Fail("BattleController: não achei Systems/FlowMeter"); return; }
		if (_hud == null) { Fail("BattleController: não achei HUD"); return; }
		if (_mage == null) { Fail("BattleController: não achei World/Characters/Mage"); return; }
		if (_enemy == null) { Fail("BattleController: não achei World/Characters/Enemy"); return; }
		if (_projectilesParent == null) { Fail("BattleController: não achei World/Projectiles"); return; }
		if (_hud.ElementBar == null) { Fail("BattleController: HUD.ElementBar está null"); return; }

		_attackCircle = GetNodeOrNull<AttackCircleController>(AttackCirclePath);
		_vfxLib = GetNodeOrNull<ElementVfxLibrary>(ElementVfxLibraryPath);

		if (_attackCircle == null) { Fail($"BattleController: não achei AttackCircle em {AttackCirclePath}"); return; }
		if (_vfxLib == null) { Fail($"BattleController: não achei ElementVfxLibrary em {ElementVfxLibraryPath}"); return; }

		_enemyProtection = GetNodeOrNull<EnemyProtectionController>(EnemyProtectionPath);
		_enemyGround = GetNodeOrNull<Marker2D>(EnemyGroundPointPath);
		_enemyHit = GetNodeOrNull<Marker2D>(EnemyHitPointPath);
		_mageCast = GetNodeOrNull<Marker2D>(MageVfxCastPath);
		_mageAnim = GetNodeOrNull<AnimationPlayer>(MageAnimPlayerPath);

		_enemyCycle = GetNodeOrNull<EnemyElementCycleController>(EnemyElementCyclePath);
		if (_enemyCycle == null) GD.PushWarning($"BattleController: não achei EnemyElementCycle em {EnemyElementCyclePath}");

		_background = GetNodeOrNull<Sprite2D>(BackgroundPath);
		if (_background == null) GD.PushWarning($"BattleController: não achei Background Sprite2D em {BackgroundPath}");

		_turnBanner = GetNodeOrNull<TurnBanner>(TurnBannerPath);
		if (_turnBanner == null) GD.PushWarning($"BattleController: não achei TurnBanner em {TurnBannerPath}");

		if (_enemyProtection != null)
		{
			_enemyProtection.ProtectionChanged += (int elem) =>
			{
				if (_enemyCycle == null)
					_attackCircle.SetElement(elem);
			};
		}

		if (_enemyCycle != null)
		{
			_enemyCycle.ElementChanged += (int elem) =>
			{
				_attackCircle.SetElement(elem);
				if (DebugLogs) GD.Print($"[EnemyCycle] elem -> {elem}");
			};
		}

		_hud.SetPhaseName(Phase.PhaseName);
		_pattern.ElementCount = 7;

		// IMPORTANTE: pra "20 pontos pra encher", Phase.FlowMaxStacks precisa ser 20 no inspector.
		_flow.Configure(Phase.FlowMaxStacks, Phase.FlowDamagePerStack);

		_beats = BeatmapData.LoadBeatsFromJson(Phase.BeatmapJsonPath);
		_beatScheduler.SetBeatmap(_beats);

		_music.Stream = Phase.Music;
		_music.Play();

		_mage.HealthChanged += (cur, max) => _hud.MageHP.SetHp(cur, max);
		_enemy.HealthChanged += (cur, max) => _hud.EnemyHP.SetHp(cur, max);
		CallDeferred(nameof(InitHpBars));

		_turnManager.Configure(Phase.EnemyTurnBaseSeconds, Phase.PlayerTurnBaseSeconds);
		_inputJudge.Configure(Phase.HitWindowSeconds, _hud.ElementBar);

		_turnManager.TurnStarted += OnTurnStarted;
		_beatScheduler.BeatPrepare += OnBeatPrepare;
		_beatScheduler.BeatFire += OnBeatFire;

		_inputJudge.DefenseJudged += OnDefenseJudged;
		_inputJudge.AttackJudged += OnAttackJudged;

		_inputJudge.DefenseResolved += OnDefenseResolved;
		_inputJudge.AttackResolved += OnAttackResolved;

		_inputJudge.ElementPressed += OnElementPressed;

		double now = AudioClock.GetSongTimeSeconds(_music);

		_enemyCycle?.Start(now);
		_turnManager.StartFirstTurn(now);

		ApplyTurnVisuals((_turnManager.CurrentSide == TurnSide.Player) ? 1 : 0);

		if (DebugLogs)
			GD.Print($"[Battle] LateBlock config: Perfect allow={AllowLatePerfectBlock} grace={LateBlockGraceSecondsPerfect:0.000}s | Good allow={AllowLateGoodBlock} grace={LateBlockGraceSecondsGood:0.000}s");
	}

	private void InitHpBars()
	{
		_hud.MageHP.SetHp(_mage.Hp, _mage.MaxHp);
		_hud.EnemyHP.SetHp(_enemy.Hp, _enemy.MaxHp);
	}

	public override void _Process(double delta)
	{
		if (_broken) return;
		if (Phase == null) return;

		double now = AudioClock.GetSongTimeSeconds(_music);
		AttackRingController.SongNowSec = now;

		_beatScheduler.Update(now);
		_turnManager.Update(now);

		_enemyCycle?.UpdateNow(now);

		_inputJudge.SetSongTime(now);
		_hud.ElementBar.SetSongTime(now);

		_inputJudge.UpdateJudge();

		_hud.SetTurnProgress(now, _turnManager.TurnStartSec, _turnManager.TurnEndSec);
		_hud.SetFlow(_flow.Stacks, Phase.FlowMaxStacks);

		if (_attackCircle != null)
		{
			Vector2 g = (_enemyGround != null) ? _enemyGround.GlobalPosition : _enemy.GetGroundPointGlobal();
			_attackCircle.GlobalPosition = g;
		}
	}

	// ===== elemento atual do inimigo =====
	private int GetEnemyCurrentElement()
	{
		if (_enemyCycle != null) return _enemyCycle.CurrentElement;
		if (_enemyProtection != null) return _enemyProtection.CurrentElement;
		if (_attackCircle != null) return _attackCircle.CurrentElementId;
		return 1;
	}

	// =========================
	// FLOW helpers
	// =========================
	private void ResetFlow()
	{
		// reseta exatamente para 0 (sem depender de clamp interno)
		int cur = _flow.Stacks;
		if (cur != 0) _flow.Add(-cur);
	}

	private void AddFlowFromGrade(JudgementGrade grade)
	{
		if (grade == JudgementGrade.Perfect) _flow.Add(PerfectFlowGain); // +2
		else if (grade == JudgementGrade.Good) _flow.Add(GoodFlowGain);  // +1
		else ResetFlow(); // Miss => zera
	}

	// =========================
	// INPUT (só feedback de animação; o julgamento é no InputJudge)
	// =========================
	private void OnElementPressed(int elementId)
	{
		_hud.ElementBar.SetSelectedElement(elementId);

		if (DebugLogs)
			GD.Print($"[Battle] Press e{elementId} side={_turnManager.CurrentSide}");

		if (_turnManager.CurrentSide == TurnSide.Enemy)
		{
			_mage.SetShieldElement(elementId);
			_mage.PlayRandomDefendAnim();
			return;
		}

		_mage.PlayRandomAttackAnim();
	}

	// =========================
	// PROJECTILES SAFETY
	// =========================
	private void CleanupEnemyProjectiles()
	{
		foreach (var kv in _enemyProjectiles)
		{
			var proj = kv.Value;
			if (!IsInstanceValid(proj)) continue;
			proj.CancelAndDespawn();
		}
		_enemyProjectiles.Clear();
	}

	// =========================
	// TURN / BEATS
	// =========================
	private void OnTurnStarted(int sideId, double startSec, double endSec)
	{
		double now = AudioClock.GetSongTimeSeconds(_music);

		CleanupEnemyProjectiles();

		_inputJudge.ClearPending();
		_defenseGrade.Clear();
		_attackGrade.Clear();
		_beatOwner.Clear();
		_requiredElementByBeat.Clear();
		_enemyElementLockedByBeat.Clear();

		_playerRequiredElementNow = -1;

		_hud.ElementBar.SetMode(sideId);

		ApplyTurnVisuals(sideId);
		_turnBanner?.ShowTurn(sideId);

		_beatScheduler.OnTurnWindow(startSec, endSec, Phase.PrepareLeadSeconds, now);

		if (_attackCircle != null)
		{
			_attackCircle.Visible = true;
			_attackCircle.Start(now);
			_attackCircle.SetElement(GetEnemyCurrentElement());
		}
	}

	private void OnBeatPrepare(int beatIndex, double beatSec)
	{
		double now = AudioClock.GetSongTimeSeconds(_music);

		// 1) Safety: se não há lead suficiente, não cria hint/projétil (evita "quase em cima da hora")
		if (DelayNewTurnCuesForFullLead)
		{
			double lead = beatSec - now;
			double need = Phase.PrepareLeadSeconds;

			// tolerância pequena
			if (lead < need * 0.90)
			{
				if (DebugLogs)
					GD.Print($"[Battle] SKIP BeatPrepare beat={beatIndex} lead={lead:0.000}s need={need:0.000}s side={_turnManager.CurrentSide}");
				return;
			}
		}

		// 2) Agora sim gravamos owner/lock (só para beats que realmente vamos usar)
		var side = _turnManager.CurrentSide;
		_beatOwner[beatIndex] = side;

		int lockedEnemyElem = GetEnemyCurrentElement();
		_enemyElementLockedByBeat[beatIndex] = lockedEnemyElem;

		_attackCircle?.SetElement(lockedEnemyElem);

		if (side == TurnSide.Enemy)
		{
			int requiredDefenseElement = DefenseCounter(lockedEnemyElem);
			_requiredElementByBeat[beatIndex] = requiredDefenseElement;

			bool changed = lockedEnemyElem != _enemyStanceElementNow;
			_enemyStanceElementNow = lockedEnemyElem;
			_enemy.SetStanceElementHint(lockedEnemyElem, pulse: changed);

			_enemy.PlayPrepare();

			_mage.ArmDefenseWindow(Phase.PrepareLeadSeconds + EnemyHoldOnBlockSeconds);
			_inputJudge.QueueDefense(beatIndex, beatSec, requiredDefenseElement);

			SpawnEnemyProjectileForBeat(beatIndex, lockedEnemyElem);
			return;
		}

		// player turn
		int requiredAttackElement = AttackCounter(lockedEnemyElem);
		_requiredElementByBeat[beatIndex] = requiredAttackElement;
		_playerRequiredElementNow = requiredAttackElement;

		_hud.ElementBar.CueElement(requiredAttackElement, (float)Phase.PrepareLeadSeconds, beatSec, now);
		_inputJudge.QueueAttack(beatIndex, beatSec, requiredAttackElement);
	}

	private void OnBeatFire(int beatIndex, double beatSec)
	{
		if (_requiredElementByBeat.TryGetValue(beatIndex, out int reqElem))
			_hud.ElementBar.BeatPop(reqElem);

		if (_beatOwner.TryGetValue(beatIndex, out var owner) && owner == TurnSide.Enemy)
			_enemy.PlayShoot();
	}

	// =========================
	// ENEMY PROJECTILES
	// =========================
	private void SpawnEnemyProjectileForBeat(int beatIndex, int enemyElement)
	{
		PackedScene scene = null;

		if (_enemy != null && _enemy.HasMethod("GetProjectileSceneForElement"))
		{
			var v = _enemy.Call("GetProjectileSceneForElement", enemyElement);
			if (v.VariantType == Variant.Type.Object && v.AsGodotObject() is PackedScene ps)
				scene = ps;
		}

		scene ??= _enemy.RhythmProjectileScene;

		if (scene == null)
		{
			GD.PushWarning("BattleController: nenhum projétil configurado (elemental e fallback null).");
			return;
		}

		var inst = scene.Instantiate();
		if (inst is not RhythmProjectile proj)
		{
			inst.QueueFree();
			GD.PushWarning("BattleController: projétil instanciado não é RhythmProjectile.");
			return;
		}

		_projectilesParent.AddChild(proj);

		Vector2 startWorld = _enemy.GetMuzzleGlobal();
		Vector2 blockWorld = _mage.GetBlockPointGlobal();
		Vector2 hitWorld = _mage.GetHitPointGlobal();

		int baseDmg = _enemy.BaseDamage;

		proj.BeatIndexDebug = beatIndex;
		proj.DebugLogs = DebugLogs;

		proj.SetTimings((float)Phase.PrepareLeadSeconds, EnemyTravelToHitSeconds, EnemyHoldOnBlockSeconds);
		proj.Launch(startWorld, _mage, blockWorld, hitWorld, baseDmg);

		_enemyProjectiles[beatIndex] = proj;
	}

	// =========================
	// JUDGES
	// =========================
	private void OnDefenseJudged(int beatIndex, int gradeId, double absErr)
	{
		var grade = (JudgementGrade)gradeId;
		_defenseGrade[beatIndex] = grade;
		_hud.ShowJudgement(grade);
		_hud.OnJudgement(grade);
		if (_requiredElementByBeat.TryGetValue(beatIndex, out int reqElem))
			_hud.ElementBar.Resolve(reqElem, gradeId);

		if (DebugLogs)
			GD.Print($"[Battle] DEF JUDGED beat={beatIndex} grade={grade} absErr={absErr:0.0000}");

		if (!_enemyProjectiles.TryGetValue(beatIndex, out var proj) || !IsInstanceValid(proj))
			return;

		if (grade == JudgementGrade.Miss)
			return;

		int dmgOnBlock = 0;

		bool allowLate =
			(grade == JudgementGrade.Perfect && AllowLatePerfectBlock) ||
			(grade == JudgementGrade.Good && AllowLateGoodBlock);

		float grace =
			grade == JudgementGrade.Perfect ? LateBlockGraceSecondsPerfect :
			LateBlockGraceSecondsGood;

		bool blocked = proj.TryBlock(dmgOnBlock, allowLate, grace);
		if (blocked) _mage.HoldShield(EnemyHoldOnBlockSeconds);
	}

	private void OnAttackJudged(int beatIndex, int gradeId, double absErr)
	{
		var grade = (JudgementGrade)gradeId;
		_attackGrade[beatIndex] = grade;
		_hud.ShowJudgement(grade);
		_hud.OnJudgement(grade);
		if (_requiredElementByBeat.TryGetValue(beatIndex, out int reqElem))
			_hud.ElementBar.Resolve(reqElem, gradeId);

		if (DebugLogs)
			GD.Print($"[Battle] ATK JUDGED beat={beatIndex} grade={grade} absErr={absErr:0.0000}");
	}

	// =========================
	// RESOLVED (onde a regra do Flow e o dano ficam 100% coerentes)
	// =========================
	private void OnDefenseResolved(int beatIndex, bool success)
	{
		_defenseGrade.TryGetValue(beatIndex, out var grade);

		if (!success || grade == JudgementGrade.Miss)
		{
			_mage.OnDefendFail();
			ResetFlow(); // miss reseta
			return;
		}

		_mage.OnDefendSuccess();
		AddFlowFromGrade(grade);

		float mult = (grade == JudgementGrade.Perfect) ? PerfectTurnReduceMult : GoodTurnReduceMult;
		ReduceTurnEndSafe(Phase.DefenseSuccessReduceEnemySeconds * mult);
	}

	private void OnAttackResolved(int beatIndex, bool success)
	{
		_attackGrade.TryGetValue(beatIndex, out var grade);

		if (!success || grade == JudgementGrade.Miss)
		{
			ResetFlow(); // miss reseta
			return;
		}

		AddFlowFromGrade(grade);

		// Dano por grade
		float dmgMult = (grade == JudgementGrade.Perfect) ? 1.0f : PlayerGoodDamageMultiplier;
		int dmg = Mathf.RoundToInt(PlayerBaseDamage * dmgMult);
		_enemy.ApplyDamage(dmg);

		// VFX no inimigo (random até 3 variações)
		if (_requiredElementByBeat.TryGetValue(beatIndex, out int elemId))
		{
			var parent = GetNodeOrNull<Node>(WorldVfxParentPath) ?? this;
			Vector2 hit = (_enemyHit != null) ? _enemyHit.GlobalPosition : _enemy.GetHitPointGlobal();

			_vfxLib.SpawnAttackImpactRandom(elemId, parent, hit);
		}

		_mage.PlayIdle();
	}

	// =========================
	// "SAFE REDUCE" para não cortar lead do próximo beat
	// =========================
	private double GetNextBeatAfter(double now)
	{
		if (_beats == null || _beats.Length == 0) return double.PositiveInfinity;

		int lo = 0, hi = _beats.Length - 1, ans = _beats.Length;
		while (lo <= hi)
		{
			int mid = (lo + hi) >> 1;
			if (_beats[mid] > now) { ans = mid; hi = mid - 1; }
			else lo = mid + 1;
		}

		return (ans < _beats.Length) ? _beats[ans] : double.PositiveInfinity;
	}

	private void ReduceTurnEndSafe(double reduceBySeconds)
	{
		if (reduceBySeconds <= 0) return;

		if (!DelayNewTurnCuesForFullLead)
		{
			_turnManager.ReduceCurrentTurnEnd(reduceBySeconds);
			return;
		}

		double now = AudioClock.GetSongTimeSeconds(_music);
		double nextBeat = GetNextBeatAfter(now);
		if (double.IsInfinity(nextBeat))
		{
			_turnManager.ReduceCurrentTurnEnd(reduceBySeconds);
			return;
		}

		double minEnd = nextBeat - Phase.PrepareLeadSeconds;
		double proposedEnd = _turnManager.TurnEndSec - reduceBySeconds;

		if (proposedEnd < minEnd)
			reduceBySeconds = Math.Max(0.0, _turnManager.TurnEndSec - minEnd);

		if (reduceBySeconds > 0)
			_turnManager.ReduceCurrentTurnEnd(reduceBySeconds);
	}

	// =========================
	// TURN VISUALS HELPERS
	// =========================
	private void ApplyTurnVisuals(int sideId)
	{
		Color target = NeutralBgModulate;
		if (sideId == 1) target = AttackBgModulate;
		else target = DefendBgModulate;

		if (_background == null) return;

		if (_bgTween != null && GodotObject.IsInstanceValid(_bgTween)) _bgTween.Kill();
		_bgTween = CreateTween();
		_bgTween.TweenProperty(_background, "modulate", target, Mathf.Max(0.01f, TurnOverlayTweenTime))
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
	}

	// =========================
	// TABLES
	// =========================
	private int DefenseCounter(int enemyAttackElement)
	{
		return enemyAttackElement switch
		{
			2 => 5,
			3 => 1,
			4 => 3,
			1 => 2,
			5 => 3,
			6 => 7,
			7 => 6,
			_ => 1
		};
	}

	private int AttackCounter(int enemyProtectionElement)
	{
		return enemyProtectionElement switch
		{
			1 => 2,
			7 => 6,
			2 => 5,
			3 => 1,
			5 => 3,
			4 => 5,
			6 => 7,
			_ => 1
		};
	}

	// =========================
	// FAIL
	// =========================
	private void Fail(string msg)
	{
		GD.PushError(msg);
		_broken = true;
	}

	/*
	1 Fogo
	2 Água
	3 Terra
	4 Ar/Vento
	5 Raio
	6 Luz
	7 Trevas
	*/
}
