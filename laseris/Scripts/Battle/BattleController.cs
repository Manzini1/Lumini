using Godot;
using System;
using System.Collections.Generic;

using Game.Data;
using Game.UI;
using Game.Characters;
using Game.Combat;
using Game.Vfx;

namespace Game.Battle
{
	public partial class BattleController : Node2D
	{
		[Export] public PhaseDefinition Phase;
		[Export] public bool DebugRequirePressToSpawnOnMiss = true;
		[Export] public bool DebugUsePressedElementForVfx = true;
		[ExportGroup("Score HUD")]
[Export] public NodePath ScoreHudPath = "HUD/ScoreBarsHUD";
private DualScoreBarsController _scoreHud;
		// ------------------- SCORE / DAMAGE TOTAL -------------------
		[ExportGroup("Damage Total (Score)")]
		[Export] public bool EnableDamageTotal = true;
		[Export] public NodePath DamageTotalLabelPath = "HUD/DamageTotalLabel";
		[Export] public string DamageTotalPrefix = "DMG ";
		[Export] public bool DamageTotalUseThousandsSeparator = true;

		private long _damageTotal = 0;
		private Label _damageTotalLabel;
		// ------------------------------------------------------------
		[ExportGroup("Damage Total (UI AAA)")]
		[Export] public NodePath DamageTotalRootPath = "HUD/DamageTotalRoot";
		private DamageTotalLabelController _damageUi;
		[ExportGroup("Enemy Aura Draw")]
		[Export] public int EnemyAuraZIndex = -10;
		[Export] public bool EnemyAuraTopLevel = true;
		[Export] public bool EnemyAuraForceScaleOne = true;

		private int _auraResolveTries = 0;

		private AudioStreamPlayer _music;
		private BeatScheduler _beatScheduler;
		private TurnManager _turnManager;
		private InputJudge _inputJudge;
		private AttackPattern _pattern;
		private FlowMeter _flow;

		private HudController _hud;
		private MageController _mage;
		private EnemyController _enemy;
		private Node2D _projectilesParent;

		[ExportGroup("Enemy Indicator")]
		[Export] public NodePath EnemyAuraPath = "World/Vfx/EnemyAura";
		private ElementAuraController _enemyAura;

		[ExportGroup("Debug / VFX Testing")]
		[Export] public bool DebugSpawnCastVfxEvenOnMiss = false;
		[Export] public bool DebugSpawnImpactEvenOnMiss = false;

		private readonly Dictionary<int, int> _pressedElementByBeat = new();
		private int _currentPlayerAttackBeatIndex = -1;

		[ExportGroup("Enemy Element System")]
		[Export] public NodePath EnemyElementCyclePath = "World/Characters/Enemy/ElementCycle";
		private EnemyElementCycleController _enemyCycle;

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

		[ExportGroup("Player Projectile Timing")]
		[Export] public float PlayerProjectileToHitSeconds = 0.06f;

		[ExportGroup("Flow Points")]
		[Export] public int PerfectFlowGain = 2;
		[Export] public int GoodFlowGain = 1;

		[ExportGroup("Defense Rewards")]
		[Export] public float PerfectTurnReduceMult = 1.0f;
		[Export] public float GoodTurnReduceMult = 0.6f;

		[ExportGroup("Enemy Indicator (Aura/Circle)")]
		[Export] public NodePath EnemyIndicatorPath = "World/Vfx/AttackCircle";
		[Export] public NodePath ElementVfxLibraryPath = "Systems/ElementVfxLibrary";

		[ExportGroup("World VFX")]
		[Export] public NodePath WorldVfxParentPath = "World/Vfx";

		// ✅ NOVO: Director (apresentação)
		[ExportGroup("VFX Director")]
		[Export] public NodePath BattleVfxDirectorPath = "Systems/BattleVfxDirector";
		private BattleVfxDirector _vfx;

		private IElementIndicator _enemyIndicator;
		private Node2D _enemyIndicatorNode2D;

		private ElementVfxLibrary _vfxLib; // fallback / sanity

		private bool _broken;
		private int _enemyStanceElementNow = -1;

		[ExportGroup("Debug")]
		[Export] public bool DebugLogs = true;

		[ExportGroup("Turn Visuals")]
		[Export] public NodePath BackgroundPath = "Background";
		[Export] public NodePath TurnBannerPath = "HUD/TurnBanner";

		[Export] public Color NeutralBgModulate = new Color(1, 1, 1, 1);
		[Export] public Color AttackBgModulate = new Color(1.10f, 0.92f, 0.92f, 1);
		[Export] public Color DefendBgModulate = new Color(0.92f, 0.98f, 1.10f, 1);
		[Export] public float TurnOverlayTweenTime = 0.18f;

		private Sprite2D _background;
		private TurnBanner _turnBanner;
		private Tween _bgTween;

		[ExportGroup("Turn Transition Safety")]
		[Export] public bool DelayNewTurnCuesForFullLead = true;

		// ---------------- Aura ----------------
		private void EnsureEnemyAura()
		{
			if (_enemyAura == null || !GodotObject.IsInstanceValid(_enemyAura))
			{
				_enemyAura = GetNodeOrNull<ElementAuraController>(EnemyAuraPath);

				if (_enemyAura == null)
				{
					var found = FindChild("EnemyAura", recursive: true, owned: false);
					if (found is ElementAuraController auraFound)
						_enemyAura = auraFound;
				}

				if (_enemyAura == null)
				{
					_auraResolveTries++;
					if (_auraResolveTries <= 3)
						GD.PushWarning($"[Battle] EnemyAura ainda NULL. Path='{EnemyAuraPath}'. (tentativa {_auraResolveTries})");
					return;
				}
			}

			if (!_enemyAura.Visible) _enemyAura.Visible = true;
			_enemyAura.SetEnabled(true);

			_enemyAura.TopLevel = EnemyAuraTopLevel;

			if (EnemyAuraForceScaleOne)
				_enemyAura.Scale = Vector2.One;

			_enemyAura.ZAsRelative = false;
			_enemyAura.ZIndex = EnemyAuraZIndex;

			Node p = _enemyAura.GetParent();
			while (p != null)
			{
				if (p is CanvasItem ci && !ci.Visible)
					ci.Visible = true;
				p = p.GetParent();
			}

			_enemyAura.SetElement(GetEnemyCurrentElement());
		}

		// ---------------- Score helpers ----------------
		private void ResolveDamageTotalLabel()
		{
			_damageTotalLabel = null;
			if (!EnableDamageTotal) return;

			if (DamageTotalLabelPath != null && !DamageTotalLabelPath.IsEmpty)
				_damageTotalLabel = GetNodeOrNull<Label>(DamageTotalLabelPath);

			if (_damageTotalLabel == null)
			{
				var found = FindChild("DamageTotalLabel", recursive: true, owned: false);
				if (found is Label l) _damageTotalLabel = l;
			}

			if (_damageTotalLabel == null && DebugLogs)
				GD.PushWarning($"[Battle] DamageTotalLabel não encontrado. Path='{DamageTotalLabelPath}'.");
		}
		private void AddDamageTotal(int amount, bool bigHit = false)
		{
			if (!EnableDamageTotal) return;
			if (amount <= 0) return;

			_damageTotal += amount;

			// ✅ player score
			_scoreHud?.AddPlayerDamage(amount);

			_damageUi?.Add(amount, bigHit);
		}
		private void ResetDamageTotal()
		{
			_damageTotal = 0;
			_damageUi?.SetImmediate(0);
			_scoreHud?.SetImmediate(0, 0);
		}


		private void UpdateDamageTotalLabel()
		{
			if (!EnableDamageTotal) return;
			if (_damageTotalLabel == null || !GodotObject.IsInstanceValid(_damageTotalLabel)) return;

			string num = DamageTotalUseThousandsSeparator
				? _damageTotal.ToString("N0")
				: _damageTotal.ToString("0");

			_damageTotalLabel.Text = $"{DamageTotalPrefix}{num}";
		}

		// ---------------- Flow helpers ----------------
		private bool IsFlowFull(int stacks)
		{
			if (_flow == null) return false;
			if (_flow.MaxStacks <= 0) return false;
			return stacks >= _flow.MaxStacks;
		}

		private int PredictFlowStacksAfterGrade(JudgementGrade grade, int curStacks)
		{
			if (_flow == null || _flow.MaxStacks <= 0) return 0;

			int s = curStacks;

			if (grade == JudgementGrade.Perfect) s += PerfectFlowGain;
			else if (grade == JudgementGrade.Good) s += GoodFlowGain;
			else s = 0;

			return Mathf.Clamp(s, 0, _flow.MaxStacks);
		}

		public override void _Ready()
		{
			_music = GetNodeOrNull<AudioStreamPlayer>("Systems/MusicPlayer");
			_beatScheduler = GetNodeOrNull<BeatScheduler>("Systems/BeatScheduler");
			_turnManager = GetNodeOrNull<TurnManager>("Systems/TurnManager");
			_inputJudge = GetNodeOrNull<InputJudge>("Systems/InputJudge");
			_pattern = GetNodeOrNull<AttackPattern>("Systems/AttackPattern");
			_flow = GetNodeOrNull<FlowMeter>("Systems/FlowMeter");

			_hud = GetNodeOrNull<HudController>("HUD");
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
			
			_scoreHud = GetNodeOrNull<DualScoreBarsController>(ScoreHudPath);
			if (_scoreHud == null && DebugLogs)
				GD.PushWarning($"[Battle] ScoreBarsHUD não encontrado em {ScoreHudPath}");
	
			_damageUi = GetNodeOrNull<DamageTotalLabelController>(DamageTotalRootPath);
			if (_damageUi == null && DebugLogs)
				GD.PushWarning($"[Battle] DamageTotalRoot não encontrado: {DamageTotalRootPath}");
			_damageUi?.SetImmediate(0);
			_vfxLib = GetNodeOrNull<ElementVfxLibrary>(ElementVfxLibraryPath);
			if (_vfxLib == null) { Fail($"BattleController: não achei ElementVfxLibrary em {ElementVfxLibraryPath}"); return; }

			_vfx = GetNodeOrNull<BattleVfxDirector>(BattleVfxDirectorPath);	
			if (_vfx == null)
				GD.PushWarning($"[Battle] BattleVfxDirector não encontrado em '{BattleVfxDirectorPath}'. Vou cair no fallback direto da ElementVfxLibrary.");

			_enemyProtection = GetNodeOrNull<EnemyProtectionController>(EnemyProtectionPath);
			_enemyGround = GetNodeOrNull<Marker2D>(EnemyGroundPointPath);
			_enemyHit = GetNodeOrNull<Marker2D>(EnemyHitPointPath);
			_mageCast = GetNodeOrNull<Marker2D>(MageVfxCastPath);
			_mageAnim = GetNodeOrNull<AnimationPlayer>(MageAnimPlayerPath);

			_enemyCycle = GetNodeOrNull<EnemyElementCycleController>(EnemyElementCyclePath);
			if (_enemyCycle == null) GD.PushWarning($"BattleController: não achei EnemyElementCycle em {EnemyElementCyclePath}");

			_background = GetNodeOrNull<Sprite2D>(BackgroundPath);
			_turnBanner = GetNodeOrNull<TurnBanner>(TurnBannerPath);

			var indNode = GetNodeOrNull<Node>(EnemyIndicatorPath);
			_enemyIndicator = indNode as IElementIndicator;
			_enemyIndicatorNode2D = indNode as Node2D;

			if (_enemyIndicator == null || _enemyIndicatorNode2D == null)
			{
				Fail($"BattleController: EnemyIndicator em {EnemyIndicatorPath} não implementa IElementIndicator ou não é Node2D.");
				return;
			}

			EnsureEnemyAura();
			CallDeferred(nameof(EnsureEnemyAura));

			ResolveDamageTotalLabel();
			ResetDamageTotal();

			_hud.SetPhaseName(Phase.PhaseName);
			_pattern.ElementCount = 7;

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
		}

		private void InitHpBars()
		{
			_hud.MageHP.SetHp(_mage.Hp, _mage.MaxHp);
			_hud.EnemyHP.SetHp(_enemy.Hp, _enemy.MaxHp);
		}

		private Vector2 GetEnemyVfxCenterGlobal()
		{
			if (_enemy == null || !GodotObject.IsInstanceValid(_enemy))
				return Vector2.Zero;

			var m = _enemy.GetNodeOrNull<Marker2D>("VfxCenter");
			if (m != null) return m.GlobalPosition;

			if (_enemyHit != null) return _enemyHit.GlobalPosition;

			return _enemy.GetHitPointGlobal();
		}

		private Vector2 GetMageCastGlobal()
		{
			if (_mageCast != null) return _mageCast.GlobalPosition;

			var m = _mage?.GetNodeOrNull<Marker2D>("VfxCast");
			if (m != null) return m.GlobalPosition;

			return _mage != null ? _mage.GlobalPosition : Vector2.Zero;
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

			EnsureEnemyAura();
			if (_enemyAura != null && GodotObject.IsInstanceValid(_enemyAura))
			{
				Vector2 g = (_enemyGround != null) ? _enemyGround.GlobalPosition : _enemy.GetGroundPointGlobal();
				_enemyAura.SetTargetGlobal(g);
			}

			if (_enemyIndicatorNode2D != null)
			{
				Vector2 g = (_enemyGround != null) ? _enemyGround.GlobalPosition : _enemy.GetGroundPointGlobal();
				_enemyIndicatorNode2D.GlobalPosition = g;
			}

			if (_enemyIndicatorNode2D is AttackCircleController legacyCircle)
				legacyCircle.UpdateNow(now);
		}

		private int GetEnemyCurrentElement()
		{
			if (_enemyCycle != null) return _enemyCycle.CurrentElement;
			if (_enemyProtection != null) return _enemyProtection.CurrentElement;
			if (_enemyIndicator != null) return _enemyIndicator.CurrentElementId;
			return 1;
		}

		private void ResetFlow()
		{
			int cur = _flow.Stacks;
			if (cur != 0) _flow.Add(-cur);
		}

		private void AddFlowFromGrade(JudgementGrade grade)
		{
			if (grade == JudgementGrade.Perfect) _flow.Add(PerfectFlowGain);
			else if (grade == JudgementGrade.Good) _flow.Add(GoodFlowGain);
			else ResetFlow();
		}

		private void OnElementPressed(int elementId)
		{
			_hud.ElementBar.SetSelectedElement(elementId);

			if (_turnManager.CurrentSide == TurnSide.Enemy)
			{
				_mage.SetShieldElement(elementId);
				_mage.PlayRandomDefendAnim();
				return;
			}

			if (_currentPlayerAttackBeatIndex >= 0)
				_pressedElementByBeat[_currentPlayerAttackBeatIndex] = elementId;

			_mage.PlayRandomAttackAnim();
		}

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

		private void OnTurnStarted(int sideId, double startSec, double endSec)
		{
			double now = AudioClock.GetSongTimeSeconds(_music);

			CleanupEnemyProjectiles();

			_pressedElementByBeat.Clear();
			_currentPlayerAttackBeatIndex = -1;

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

			_enemyIndicator.SetEnabled(true);
			_enemyIndicator.SetElement(GetEnemyCurrentElement());

			EnsureEnemyAura();
			_enemyAura?.SetEnabled(true);
			_enemyAura?.SetElement(GetEnemyCurrentElement());

			if (_enemyIndicatorNode2D is AttackCircleController legacyCircle)
				legacyCircle.Start(now);
		}

		private void OnBeatPrepare(int beatIndex, double beatSec)
		{
			double now = AudioClock.GetSongTimeSeconds(_music);

			if (DelayNewTurnCuesForFullLead)
			{
				double lead = beatSec - now;
				double need = Phase.PrepareLeadSeconds;
				if (lead < need * 0.90) return;
			}

			var side = _turnManager.CurrentSide;
			_beatOwner[beatIndex] = side;

			int lockedEnemyElem = GetEnemyCurrentElement();
			_enemyElementLockedByBeat[beatIndex] = lockedEnemyElem;

			_enemyIndicator?.SetElement(lockedEnemyElem);
			EnsureEnemyAura();
			_enemyAura?.SetElement(lockedEnemyElem);

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

			int requiredAttackElement = AttackCounter(lockedEnemyElem);
			_requiredElementByBeat[beatIndex] = requiredAttackElement;
			_playerRequiredElementNow = requiredAttackElement;

			_currentPlayerAttackBeatIndex = beatIndex;

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

			// ✅ conecta sinais/eventos
			proj.HitMage += OnEnemyProjectileHitMage;
			proj.DeflectHitEnemy += OnEnemyProjectileDeflectHitEnemy;

			proj.SetTimings((float)Phase.PrepareLeadSeconds, EnemyTravelToHitSeconds, EnemyHoldOnBlockSeconds);
			proj.Launch(startWorld, _mage, blockWorld, hitWorld, baseDmg);

			_enemyProjectiles[beatIndex] = proj;
		}

			private void OnEnemyProjectileHitMage(int beatIndex, int damage)
			{
				// ✅ enemy score
				_scoreHud?.AddEnemyDamage(damage);

				if (DebugLogs)
					GD.Print($"[EnemyHitMage] beat={beatIndex} dmg={damage}");
			}
		private void OnEnemyProjectileDeflectHitEnemy(int beatIndex, int damage)
		{
			int elem = _enemyElementLockedByBeat.TryGetValue(beatIndex, out int e) ? e : GetEnemyCurrentElement();

			AddDamageTotal(damage, false);

			// impacto no inimigo quando chega
			if (_vfx != null) _vfx.PlayDeflectImpactOnEnemy(elem);
			else _vfxLib?.SpawnAttackImpactRandom(elem, GetNodeOrNull<Node>(WorldVfxParentPath) ?? this, GetEnemyVfxCenterGlobal());

			if (DebugLogs)
				GD.Print($"[DeflectHitEnemy] beat={beatIndex} elem={elem} dmg={damage}");
		}

		private void OnDefenseJudged(int beatIndex, int gradeId, double absErr)
		{
			var grade = (JudgementGrade)gradeId;
			_defenseGrade[beatIndex] = grade;

			_hud.ShowJudgement(grade);
			_hud.OnJudgement(grade);

			if (_requiredElementByBeat.TryGetValue(beatIndex, out int reqElem))
				_hud.ElementBar.Resolve(reqElem, gradeId);

			if (!_enemyProjectiles.TryGetValue(beatIndex, out var proj) || !IsInstanceValid(proj))
				return;

			if (grade == JudgementGrade.Miss)
				return;

			bool allowLate =
				(grade == JudgementGrade.Perfect && AllowLatePerfectBlock) ||
				(grade == JudgementGrade.Good && AllowLateGoodBlock);

			float grace =
				grade == JudgementGrade.Perfect ? LateBlockGraceSecondsPerfect :
				LateBlockGraceSecondsGood;

			bool blocked = proj.TryBlock(dmgOnBlock: 0, allowLate: allowLate, graceSeconds: grace);
			if (!blocked) return;

			_mage.HoldShield(EnemyHoldOnBlockSeconds);

			// ✅ DEFLECT: Perfect + flow ficará cheio após essa defesa
			int predictedStacks = PredictFlowStacksAfterGrade(grade, _flow.Stacks);
			bool flowFullAfter = IsFlowFull(predictedStacks);

			if (grade == JudgementGrade.Perfect && flowFullAfter)
			{
				Vector2 enemyHit = GetEnemyVfxCenterGlobal();
				bool ok = proj.DeflectToEnemy(enemyHit, EnemyTravelToHitSeconds);

				// fallback se por algum motivo não deu pra armar deflect
				if (!ok)
				{
					int elem = _enemyElementLockedByBeat.TryGetValue(beatIndex, out int el) ? el : GetEnemyCurrentElement();
					AddDamageTotal(_enemy.BaseDamage, false);

					if (_vfx != null) _vfx.PlayDeflectImpactOnEnemy(elem);
					else _vfxLib?.SpawnAttackImpactRandom(elem, GetNodeOrNull<Node>(WorldVfxParentPath) ?? this, enemyHit);
				}
			}
		}

		private void OnAttackJudged(int beatIndex, int gradeId, double absErr)
		{
			var grade = (JudgementGrade)gradeId;
			_attackGrade[beatIndex] = grade;

			_hud.ShowJudgement(grade);
			_hud.OnJudgement(grade);

			if (_requiredElementByBeat.TryGetValue(beatIndex, out int reqElem))
				_hud.ElementBar.Resolve(reqElem, gradeId);
		}

		private void OnDefenseResolved(int beatIndex, bool success)
		{
			_defenseGrade.TryGetValue(beatIndex, out var grade);

			if (!success || grade == JudgementGrade.Miss)
			{
				_mage.OnDefendFail();
				ResetFlow();
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
			bool isMiss = (!success || grade == JudgementGrade.Miss);

			bool hasPressed = _pressedElementByBeat.TryGetValue(beatIndex, out int pressedElem);

			if (!_requiredElementByBeat.TryGetValue(beatIndex, out int requiredElem))
				requiredElem = -1;

			int vfxElem = requiredElem;
			if (DebugUsePressedElementForVfx && hasPressed)
				vfxElem = pressedElem;

			bool allowCastOnMiss =
				DebugSpawnCastVfxEvenOnMiss &&
				(!DebugRequirePressToSpawnOnMiss || hasPressed);

			bool shouldSpawnCast = !isMiss || allowCastOnMiss;

			Vector2 hit = GetEnemyVfxCenterGlobal();
			Vector2 from = GetMageCastGlobal();

			// ✅ stacks “após o hit” para: (1) 2x no hit que enche (2) advanced cast
			int predictedStacks = PredictFlowStacksAfterGrade(isMiss ? JudgementGrade.Miss : grade, _flow.Stacks);
			bool flowFullAfterHit = IsFlowFull(predictedStacks);

			// VFX/skill
			if (shouldSpawnCast && vfxElem >= 1)
			{
				if (_vfx != null)
				{
					_vfx.PlayPlayerCast(vfxElem, flowFullAfterHit, PlayerProjectileToHitSeconds);
				}
				else
				{
					// fallback direto (mantém comportamento antigo)
					Node projParent = (Node)(_projectilesParent ?? this);
					_vfxLib?.SpawnCastProjectile(vfxElem, projParent, from, hit, PlayerProjectileToHitSeconds);
				}

				if (!isMiss || DebugSpawnImpactEvenOnMiss)
				{
					float t = Mathf.Max(0.0f, PlayerProjectileToHitSeconds);
					GetTree().CreateTimer(t).Timeout += () =>
					{
						if (!GodotObject.IsInstanceValid(this)) return;

						if (_vfx != null) _vfx.PlayImpactOnEnemy(vfxElem);
						else _vfxLib?.SpawnAttackImpactRandom(vfxElem, GetNodeOrNull<Node>(WorldVfxParentPath) ?? this, hit);
					};
				}
			}

			if (isMiss)
			{
				ResetFlow();
				return;
			}

			AddFlowFromGrade(grade);

			// ✅ DANO: base * (perfect/good) * (flow rule)
			float gradeMult = (grade == JudgementGrade.Perfect) ? 1.0f : PlayerGoodDamageMultiplier;
			float flowMult = _flow.GetSkillDamageMultiplier(predictedStacks);

			int dmg = Mathf.RoundToInt(PlayerBaseDamage * gradeMult * flowMult);

			AddDamageTotal(dmg, false);

			_mage.PlayIdle();
		}

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

		private void Fail(string msg)
		{
			GD.PushError(msg);
			_broken = true;
		}
	}
}
