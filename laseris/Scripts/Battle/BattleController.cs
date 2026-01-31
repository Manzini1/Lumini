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

	// ✅ qual elemento deve ser apertado nesse beat (defesa OU ataque)
	private readonly Dictionary<int, int> _requiredElementByBeat = new();

	// ✅ trava o elemento “fonte” por beat pra não ficar mudando no meio do preparo
	private readonly Dictionary<int, int> _enemyElementLockedByBeat = new();

	private readonly Dictionary<int, RhythmProjectile> _enemyProjectiles = new();

	// ✅ elemento “atual” exigido no turno do player (pra OnElementPressed não ficar lendo o ciclo ao vivo)
	private int _playerRequiredElementNow = -1;

	[ExportGroup("Damage")]
	[Export] public int PlayerBaseDamage = 12;

	[ExportGroup("Projectile Timing")]
	[Export] public float EnemyTravelToHitSeconds = 0.10f;
	[Export] public float EnemyHoldOnBlockSeconds = 0.12f;

	[ExportGroup("Defense Rewards")]
	[Export] public int PerfectFlowGain = 2;
	[Export] public int GoodFlowGain = 1;
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

		// Proteção só influencia visual se NÃO tiver cycle
		if (_enemyProtection != null)
		{
			_enemyProtection.ProtectionChanged += (int elem) =>
			{
				if (_enemyCycle == null)
					_attackCircle.SetElement(elem);
			};
		}

		// Cycle sempre manda no círculo (elemento atual do inimigo)
		if (_enemyCycle != null)
		{
			_enemyCycle.ElementChanged += (int elem) =>
			{
				_attackCircle.SetElement(elem);
				if (DebugLogs) GD.Print($"[EnemyCycle] elem -> {elem}");
			};
		}

		_hud.SetPhaseName(Phase.PhaseName);
		_pattern.ElementCount = 7; // agora temos 1..7 (com Darkness)
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

		// inicia o cycle já no começo (círculo sempre estável)
		_enemyCycle?.Start(now);

		_turnManager.StartFirstTurn(now);

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
		double nowSec = now;
//		_elementBar.SetSongTime(nowSec);
		_hud.SetTurnProgress(now, _turnManager.TurnStartSec, _turnManager.TurnEndSec);
		_hud.SetFlow(_flow.Stacks, Phase.FlowMaxStacks);

		// círculo sempre colado no chão do inimigo
		if (_attackCircle != null)
		{
			Vector2 g = (_enemyGround != null) ? _enemyGround.GlobalPosition : _enemy.GetGroundPointGlobal();
			_attackCircle.GlobalPosition = g;
		}
	}

	// ===== elemento “atual” do inimigo (visual / lógica fonte) =====
	private int GetEnemyCurrentElement()
	{
		if (_enemyCycle != null) return _enemyCycle.CurrentElement;
		if (_enemyProtection != null) return _enemyProtection.CurrentElement;
		if (_attackCircle != null) return _attackCircle.CurrentElementId;
		return 1;
	}

	// =========================
	// INPUT
	// =========================
	private void OnElementPressed(int elementId)
	{
		_hud.ElementBar.SetSelectedElement(elementId);

		GD.Print($"[Battle] OnElementPressed e{elementId} side={_turnManager.CurrentSide} playerReqNow={_playerRequiredElementNow} enemyElemNow={GetEnemyCurrentElement()}");
	

		// DEFESA: player escolhe shield; o Judge já vai avaliar o timing no beat
		if (_turnManager.CurrentSide == TurnSide.Enemy)
		{
			_mage.SetShieldElement(elementId);
			_mage.PlayRandomDefendAnim();
			return;
		}
		_mage.PlayRandomAttackAnim();
		// ATAQUE: usa o required “travado” do último BeatPrepare do turno do player
		int required = _playerRequiredElementNow;
		if (required < 1)
		{
			// fallback (não deveria acontecer, mas evita null logic)
			int lockedProtection = GetEnemyCurrentElement();
			required = AttackCounter(lockedProtection);
		}

		if (elementId != required)
		{
			GD.Print($"[Battle] ATK PRESS mismatch: pressed={elementId} required={required} (playerReqNow={_playerRequiredElementNow})");
			_hud.ElementBar.Resolve(elementId, (int)JudgementGrade.Miss);
			return;
		}

		// por enquanto: “hit” instantâneo (você pode migrar isso pro OnAttackResolved depois)
		_hud.ElementBar.Resolve(elementId, (int)JudgementGrade.Good);
		GD.Print($"[Battle] ATK PRESS OK: pressed={elementId} required={required} -> will play anim/vfx + dmg={PlayerBaseDamage}");
		GD.Print($"[Battle] VFX refs: vfxLib={(_vfxLib!=null)} mageAnim={(_mageAnim!=null)} mageCast={(_mageCast!=null)} enemyHit={(_enemyHit!=null)} enemyGround={(_enemyGround!=null)} parentPath={WorldVfxParentPath}");
		// anima do mago por elemento (AnimationPlayer)
		

		_enemy.ApplyDamage(PlayerBaseDamage);

		var parent = GetNodeOrNull<Node>(WorldVfxParentPath) ?? this;

		Vector2 hit = (_enemyHit != null) ? _enemyHit.GlobalPosition : _enemy.GetHitPointGlobal();
		Vector2 ground = (_enemyGround != null) ? _enemyGround.GlobalPosition : _enemy.GetGroundPointGlobal();
		Vector2 cast = (_mageCast != null) ? _mageCast.GlobalPosition : _mage.GlobalPosition;

		_vfxLib.SpawnCastVfx(elementId, parent, cast);
		GD.Print($"[Battle] Calling SpawnCastVfx elem={elementId} castPos={cast} parent={(parent!=null ? parent.GetPath() : "<null>")}");
		GD.Print($"[Battle] Calling SpawnImpactVfx elem={elementId} hitPos={hit} (earth? {elementId==3})");
		if (elementId == 3) // Terra
			_vfxLib.SpawnEarthRock(parent, ground, hit);
		else
			_vfxLib.SpawnImpactVfx(elementId, parent, hit);
		
		_mage.PlayIdle();
	}


	// =========================
	// PROJECTILES SAFETY (troca de turno)
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

		// ✅ primeiro cancela projéteis pendentes (pra não levar dano “de graça” ao virar turno)
		CleanupEnemyProjectiles();

		_inputJudge.ClearPending();
		_defenseGrade.Clear();
		_attackGrade.Clear();
		_beatOwner.Clear();
		_requiredElementByBeat.Clear();
		_enemyElementLockedByBeat.Clear();

		_playerRequiredElementNow = -1;

		_hud.ElementBar.ClearAll();
		_hud.ElementBar.SetMode(sideId);
		
		_beatScheduler.OnTurnWindow(startSec, endSec, Phase.PrepareLeadSeconds, now);

		// círculo sempre visível e apontando o elemento atual do inimigo
		if (_attackCircle != null)
		{
			_attackCircle.Visible = true;
			_attackCircle.Start(now); // assinatura Start(double) (sem CS7036)
			_attackCircle.SetElement(GetEnemyCurrentElement());
		}
	}

	private void OnBeatPrepare(int beatIndex, double beatSec)
	{
		var side = _turnManager.CurrentSide;
		_beatOwner[beatIndex] = side;
	
		// ✅ trava o elemento “fonte” nesse beat
		int lockedEnemyElem = GetEnemyCurrentElement();
		_enemyElementLockedByBeat[beatIndex] = lockedEnemyElem;
//		_elementBar.CueElement(reqElement, leadSeconds, beatSec, nowSec);
		// círculo SEMPRE reflete o elemento travado do beat (para parar “hint adoidados”)
		_attackCircle?.SetElement(lockedEnemyElem);
		double now = AudioClock.GetSongTimeSeconds(_music);
		
		if (side == TurnSide.Enemy)
		{
			// INIMIGO ATACA COM lockedEnemyElem => DEFENDO COM DefenseCounter
			int requiredDefenseElement = DefenseCounter(lockedEnemyElem);
			_requiredElementByBeat[beatIndex] = requiredDefenseElement;

			bool changed = lockedEnemyElem != _enemyStanceElementNow;
			_enemyStanceElementNow = lockedEnemyElem;
			_enemy.SetStanceElementHint(lockedEnemyElem, pulse: changed);

			_enemy.PlayPrepare();

		
			//_hud.ElementBar.CueElement(requiredDefenseElement, (float)Phase.PrepareLeadSeconds, beatSec, now);


			_mage.ArmDefenseWindow(Phase.PrepareLeadSeconds + EnemyHoldOnBlockSeconds);
			_inputJudge.QueueDefense(beatIndex, beatSec, requiredDefenseElement);

			SpawnEnemyProjectileForBeat(beatIndex, lockedEnemyElem);
			return;
		}

		// PLAYER ATACA a proteção/elemento lockedEnemyElem => usa AttackCounter
		int requiredAttackElement = AttackCounter(lockedEnemyElem);
		_requiredElementByBeat[beatIndex] = requiredAttackElement;

		// ✅ atualiza o “required atual” para o OnElementPressed não ficar lendo o ciclo ao vivo
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
	// ENEMY PROJECTILES (por elemento)
	// =========================
	private void SpawnEnemyProjectileForBeat(int beatIndex, int enemyElement)
	{
		PackedScene scene = null;

		// ✅ sem depender de método C# (compila mesmo se o EnemyController não tiver a função)
		if (_enemy != null && _enemy.HasMethod("GetProjectileSceneForElement"))
		{
			var v = _enemy.Call("GetProjectileSceneForElement", enemyElement);
			if (v.VariantType == Variant.Type.Object && v.AsGodotObject() is PackedScene ps)
				scene = ps;
		}

		// fallback
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

		if (_requiredElementByBeat.TryGetValue(beatIndex, out int reqElem))
			_hud.ElementBar.Resolve(reqElem, gradeId);

		if (DebugLogs)
			GD.Print($"[Battle] ATK JUDGED beat={beatIndex} grade={grade} absErr={absErr:0.0000}");
	}

	private void OnDefenseResolved(int beatIndex, bool success)
	{
		if (_turnManager.CurrentSide != TurnSide.Enemy) return;

		_defenseGrade.TryGetValue(beatIndex, out var grade);
		bool blocked = grade != JudgementGrade.Miss;

		if (blocked)
		{
			_mage.OnDefendSuccess();

			if (grade == JudgementGrade.Perfect)
			{
				_flow.Add(PerfectFlowGain);
				_turnManager.ReduceCurrentTurnEnd(Phase.DefenseSuccessReduceEnemySeconds * PerfectTurnReduceMult);
			}
			else
			{
				_flow.Add(GoodFlowGain);
				_turnManager.ReduceCurrentTurnEnd(Phase.DefenseSuccessReduceEnemySeconds * GoodTurnReduceMult);
			}
		}
		else
		{
			_mage.OnDefendFail();
		}
	}

	private void OnAttackResolved(int beatIndex, bool success)
	{
		// Por enquanto o ataque é instantâneo no OnElementPressed.
		// Depois a gente move dano+VFX pra cá com timing correto.
	}

	// =========================
	// TABLES
	// =========================

	// INIMIGO ATACA COM X => DEFENDO COM Y
	private int DefenseCounter(int enemyAttackElement)
	{
		return enemyAttackElement switch
		{
			2 => 5, // Água -> Raio
			3 => 1, // Terra -> Fogo
			4 => 3, // Vento/Ar -> Terra
			1 => 2, // Fogo -> Água
			5 => 3, // Raio -> Terra
			6 => 7, // Luz -> Trevas
			7 => 6, // Trevas -> Luz
			_ => 1
		};
	}

	// PROTEÇÃO / ELEMENTO ATIVO DO INIMIGO = X => ATACO COM Y
	private int AttackCounter(int enemyProtectionElement)
	{
		return enemyProtectionElement switch
		{
			1 => 2, // Proteção Fogo -> Água
			7 => 6, // Proteção Trevas -> Luz
			2 => 5, // Proteção Água -> Raio
			3 => 1, // Proteção Terra -> Fogo
			5 => 3, // Proteção Raio -> Terra
			4 => 5, // Proteção Vento -> Raio
			6 => 7, // Proteção Luz -> Trevas (se quiser)
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
	IDs oficiais (referência):
	1 Fogo
	2 Água
	3 Terra
	4 Ar/Vento
	5 Raio
	6 Luz
	7 Trevas/Darkness
	*/
}
