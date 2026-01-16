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

	private float[] _beats;
	private readonly Dictionary<int, bool> _defenseSuccess = new();

	[ExportGroup("Damage")]
	[Export] public int PlayerBaseDamage = 12;

	private bool _broken;

	// stance atual do inimigo (pra pulsar só na troca)
	private int _enemyStanceElementNow = -1;

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

		_hud.SetPhaseName(Phase.PhaseName);
		_pattern.ElementCount = 4;
		_flow.Configure(Phase.FlowMaxStacks, Phase.FlowDamagePerStack);

		_beats = BeatmapData.LoadBeatsFromJson(Phase.BeatmapJsonPath);
		_beatScheduler.SetBeatmap(_beats);

		_music.Stream = Phase.Music;
		_music.Play();
		_mage.HealthChanged += (cur, max) => _hud.MageHP.SetHp(cur, max);
		_enemy.HealthChanged += (cur, max) => _hud.EnemyHP.SetHp(cur, max);

		// inicializa
		_hud.MageHP.SetHp(_mage.Hp, _mage.MaxHp);
		_hud.EnemyHP.SetHp(_enemy.Hp, _enemy.MaxHp);
		_turnManager.Configure(Phase.EnemyTurnBaseSeconds, Phase.PlayerTurnBaseSeconds);

		_inputJudge.Configure(Phase.HitWindowSeconds, _hud.ElementBar);

		_turnManager.TurnStarted += OnTurnStarted;
		_beatScheduler.BeatPrepare += OnBeatPrepare;
		_beatScheduler.BeatFire += OnBeatFire;

		_inputJudge.DefenseResolved += OnDefenseResolved;
		_inputJudge.AttackResolved += OnAttackResolved;

		// ✅ se você já tem os sinais "Judged" no InputJudge (Perfect/Good/Miss), usa eles:
		if (_inputJudge.HasSignal("DefenseJudged"))
		{
			_inputJudge.Connect("DefenseJudged", Callable.From<int, int, double>((beatIndex, gradeId, absErr) =>
			{
				_hud.ShowJudgement((JudgementGrade)gradeId);
			}));
		}
		if (_inputJudge.HasSignal("AttackJudged"))
		{
			_inputJudge.Connect("AttackJudged", Callable.From<int, int, double>((beatIndex, gradeId, absErr) =>
			{
				_hud.ShowJudgement((JudgementGrade)gradeId);
			}));
		}

		double now = AudioClock.GetSongTimeSeconds(_music);
		_turnManager.StartFirstTurn(now);
	}

	public override void _Process(double delta)
	{
		if (_broken) return;
		if (Phase == null) return;

		double now = AudioClock.GetSongTimeSeconds(_music);
		AttackRingController.SongNowSec = now;

		_turnManager.Update(now);
		_beatScheduler.Update(now);

		_inputJudge.SetSongTime(now);
		_inputJudge.UpdateJudge();

		_hud.SetTurnProgress(now, _turnManager.TurnStartSec, _turnManager.TurnEndSec);
		_hud.SetFlow(_flow.Stacks, Phase.FlowMaxStacks);
	}

	private void OnTurnStarted(int sideId, double startSec, double endSec)
	{
		double now = AudioClock.GetSongTimeSeconds(_music);

		_inputJudge.ClearPending();
		_beatScheduler.OnTurnWindow(startSec, endSec, Phase.PrepareLeadSeconds, now);

		// ✅ stance icon ON só no turno do inimigo
		if (_turnManager.CurrentSide == TurnSide.Enemy)
		{
			_enemy.SetStanceIconVisible(true);

			_enemyStanceElementNow = _enemy.GetStanceElementForTurnProgress(0.0);
			_enemy.SetStanceElementHint(_enemyStanceElementNow, pulse: false);
		}
		else
		{
			_enemy.SetStanceIconVisible(false);
			_enemyStanceElementNow = -1;
		}
	}

	private void OnBeatPrepare(int beatIndex, double beatSec)
	{
		var side = _turnManager.CurrentSide;
		double start = beatSec - Phase.PrepareLeadSeconds;

		if (side == TurnSide.Enemy)
		{
			_defenseSuccess.Remove(beatIndex);

			// ✅ stance por tempo dentro do turno
			double dur = Math.Max(0.0001, _turnManager.TurnEndSec - _turnManager.TurnStartSec);
			double progress = (beatSec - _turnManager.TurnStartSec) / dur;
			progress = Math.Clamp(progress, 0.0, 1.0);

			int required = _enemy.GetStanceElementForTurnProgress(progress);

			bool changed = required != _enemyStanceElementNow;
			_enemyStanceElementNow = required;
			_enemy.SetStanceElementHint(required, pulse: changed);

			_enemy.PlayPrepare();

			_hud.SpawnRing(start, beatSec, Phase.HitWindowSeconds);

			_mage.ArmDefenseWindow(Phase.PrepareLeadSeconds);
			_inputJudge.QueueDefense(beatIndex, beatSec, required);
			return;
		}

		// Player attack: continua usando pattern (se quiser stance do player depois)
		int requiredPlayer = _pattern.GetRequiredElement(beatIndex, side);
		_hud.SpawnRing(start, beatSec, Phase.HitWindowSeconds);
		_inputJudge.QueueAttack(beatIndex, beatSec, requiredPlayer);
	}

	private void OnBeatFire(int beatIndex, double beatSec)
	{
		if (_turnManager.CurrentSide != TurnSide.Enemy) return;

		_enemy.PlayShoot();

		bool blocked = _defenseSuccess.TryGetValue(beatIndex, out bool ok) && ok;

		int dmg = _enemy.BaseDamage;
		_enemy.ShootAt(_projectilesParent, _mage, dmg, blocked);

		_defenseSuccess.Remove(beatIndex);
	}

	private void OnDefenseResolved(int beatIndex, bool success)
	{
		_defenseSuccess[beatIndex] = success;

		if (_turnManager.CurrentSide != TurnSide.Enemy) return;

		if (success)
		{
			_mage.OnDefendSuccess();
			_turnManager.ReduceCurrentTurnEnd(Phase.DefenseSuccessReduceEnemySeconds);
		}
		else
		{
			_mage.OnDefendFail();
		}
	}

	private void OnAttackResolved(int beatIndex, bool success)
	{
		if (_turnManager.CurrentSide != TurnSide.Player) return;

		if (success)
		{
			_mage.OnAttackSuccess();
			_flow.OnAttackHit();

			float mult = _flow.GetDamageMultiplier();
			int dmg = Mathf.RoundToInt(PlayerBaseDamage * mult);

			DamageService.Deal(_enemy, dmg);
		}
		else
		{
			_mage.OnAttackFail();
			_flow.OnAttackMiss();
			_turnManager.ReduceCurrentTurnEnd(Phase.PlayerMissReducePlayerSeconds);
		}
	}

	private void Fail(string msg)
	{
		GD.PushError(msg);
		_broken = true;
	}
}
