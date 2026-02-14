using Godot;
using System;

using Game.Characters; // MageController

namespace Game.Combat;

public partial class RhythmProjectile : Node2D
{
	[Signal] public delegate void HitMageEventHandler(int beatIndex, int damage);
	[Signal] public delegate void DeflectHitEnemyEventHandler(int beatIndex, int damage);

	[ExportGroup("Timing")]
	[Export] public float TravelToBlockSeconds = 0.40f;
	[Export] public float TravelToHitSeconds = 0.10f;
	[Export] public float HoldOnBlockSeconds = 0.12f;

	[ExportGroup("Debug")]
	[Export] public bool DebugLogs = false;

	public int BeatIndexDebug = -1;

	[ExportGroup("VFX")]
	[Export] public NodePath TrailParticlesPath = "TrailParticles";
	private GpuParticles2D _trail;

	public bool PassedBlock { get; private set; }
	public bool Blocked { get; private set; }
	public bool ReachedHit { get; private set; }

	private MageController _mage;
	private Vector2 _start;
	private Vector2 _block;
	private Vector2 _hit;

	private int _dmgHit;

	private double _t0;
	private double _tBlockAbs;
	private double _tHitAbs;

	private bool _launched;
	private bool _cancelled;

	// block state
	private bool _blockDamageApplied;
	private int _dmgOnBlockPending;

	// deflect state
	private bool _deflecting;
	private Vector2 _deflectFrom;
	private Vector2 _deflectTo;
	private double _tDeflect0;
	private double _tDeflectHitAbs;
	private int _deflectDamage;

	public void SetTimings(float toBlock, float toHit, float hold)
	{
		TravelToBlockSeconds = Mathf.Max(0.01f, toBlock);
		TravelToHitSeconds = Mathf.Max(0.01f, toHit);
		HoldOnBlockSeconds = Mathf.Max(0.0f, hold);
	}

	public void CancelAndDespawn()
	{
		_cancelled = true;

		if (_trail != null)
			_trail.Emitting = false;

		QueueFree();
	}

	public override void _Ready()
	{
		_trail = GetNodeOrNull<GpuParticles2D>(TrailParticlesPath);
		if (_trail != null) _trail.Emitting = false; // liga só quando lançar
	}

	public void Launch(Vector2 start, MageController mage, Vector2 block, Vector2 hit, int damageOnHit)
	{
		_start = start;
		_block = block;
		_hit = hit;

		_mage = mage;
		_dmgHit = Mathf.Max(0, damageOnHit);

		GlobalPosition = _start;

		_t0 = Time.GetTicksMsec() / 1000.0;
		_tBlockAbs = _t0 + TravelToBlockSeconds;
		_tHitAbs = _tBlockAbs + TravelToHitSeconds;

		_launched = true;

		if (_trail != null)
		{
			_trail.Restart();
			_trail.Emitting = true;
		}

		if (DebugLogs)
			GD.Print($"[Proj] Launch beat={BeatIndexDebug} start={_start} block={_block} hit={_hit} dmgHit={_dmgHit} tBlock={TravelToBlockSeconds:0.000} tHit={TravelToHitSeconds:0.000}");
	}

	public bool TryBlock(int dmgOnBlock, bool allowLate, float graceSeconds)
	{
		if (!_launched || Blocked) return false;

		double now = Time.GetTicksMsec() / 1000.0;

		bool passedBlockNow = now >= _tBlockAbs;
		bool insideLateGrace = allowLate && passedBlockNow && (now - _tBlockAbs) <= graceSeconds;

		if (passedBlockNow && !insideLateGrace)
		{
			if (DebugLogs)
				GD.Print($"[Proj] TryBlock FAIL (too late) beat={BeatIndexDebug} now={now:0.000} tBlockAbs={_tBlockAbs:0.000}");
			return false;
		}

		Blocked = true;
		PassedBlock = passedBlockNow;

		if (_cancelled) return true;

		_dmgOnBlockPending = Mathf.Max(0, dmgOnBlock);
		if (_mage != null && _dmgOnBlockPending > 0 && !_blockDamageApplied)
		{
			_blockDamageApplied = true;
			_mage.ApplyDamage(_dmgOnBlockPending);

			if (DebugLogs)
				GD.Print($"[Proj] Apply BLOCK dmg immediately beat={BeatIndexDebug} dmg={_dmgOnBlockPending}");
		}
		else
		{
			_blockDamageApplied = true;
		}

		// se bloqueou depois do tempo mas dentro do grace, snap pra leitura
		if (PassedBlock && insideLateGrace)
		{
			GlobalPosition = _block;
			if (DebugLogs)
				GD.Print($"[Proj] TryBlock OK (late) beat={BeatIndexDebug} grace={graceSeconds:0.000} -> snapToBlock");
		}
		else
		{
			if (DebugLogs)
				GD.Print($"[Proj] TryBlock OK (early) beat={BeatIndexDebug} dmgBlock={_dmgOnBlockPending}");
		}

		return true;
	}

	/// <summary>
	/// Ativa deflect: projétil volta do BlockPoint pro inimigo.
	/// Só funciona se já estiver Blocked.
	/// </summary>
	public bool DeflectToEnemy(Vector2 enemyHitGlobal, float travelSec, int damageOverride = -1)
	{
		if (!_launched) return false;
		if (!Blocked) return false;
		if (_deflecting) return true;
		if (_cancelled) return false;

		_deflecting = true;

		// snap pro block pra leitura e pra não "voltar do meio do caminho"
		GlobalPosition = _block;

		_deflectFrom = _block;
		_deflectTo = enemyHitGlobal;

		_deflectDamage = (damageOverride >= 0) ? damageOverride : _dmgHit;

		double now = Time.GetTicksMsec() / 1000.0;
		_tDeflect0 = now;

		float t = Mathf.Max(0.01f, travelSec);
		_tDeflectHitAbs = now + t;

		if (DebugLogs)
			GD.Print($"[Proj] DEFLECT armed beat={BeatIndexDebug} dmg={_deflectDamage} to={_deflectTo} t={t:0.000}");

		return true;
	}

	public override void _Process(double delta)
	{
		if (!_launched) return;

		double now = Time.GetTicksMsec() / 1000.0;

		// ===== DEFLECT takes priority =====
		if (_deflecting)
		{
			if (now < _tDeflectHitAbs)
			{
				float t = (float)((now - _tDeflect0) / (_tDeflectHitAbs - _tDeflect0));
				t = Mathf.Clamp(t, 0f, 1f);
				GlobalPosition = _deflectFrom.Lerp(_deflectTo, t);
				return;
			}

			if (DebugLogs)
				GD.Print($"[Proj] DEFLECT HIT enemy beat={BeatIndexDebug} dmg={_deflectDamage}");

			EmitSignal(SignalName.DeflectHitEnemy, BeatIndexDebug, _deflectDamage);

			QueueFree();
			return;
		}

		// 1) antes do block
		if (now < _tBlockAbs)
		{
			float t = (float)((now - _t0) / TravelToBlockSeconds);
			t = Mathf.Clamp(t, 0f, 1f);
			GlobalPosition = _start.Lerp(_block, t);
			return;
		}

		if (!PassedBlock)
		{
			PassedBlock = true;
			if (DebugLogs)
				GD.Print($"[Proj] Passed BLOCK time beat={BeatIndexDebug} blocked={Blocked}");
		}

		// 2) bloqueado: segura no block e some depois do hold
		if (Blocked)
		{
			GlobalPosition = _block;

			if (now >= _tBlockAbs + HoldOnBlockSeconds)
			{
				if (DebugLogs)
					GD.Print($"[Proj] Free after BLOCK hold beat={BeatIndexDebug}");
				QueueFree();
			}
			return;
		}

		// 3) não bloqueou: vai pro hit
		if (now < _tHitAbs)
		{
			float t = (float)((now - _tBlockAbs) / TravelToHitSeconds);
			t = Mathf.Clamp(t, 0f, 1f);
			GlobalPosition = _block.Lerp(_hit, t);
			return;
		}

		// 4) hit
		if (!ReachedHit)
		{
			ReachedHit = true;

			if (DebugLogs)
				GD.Print($"[Proj] Reached HIT beat={BeatIndexDebug} -> apply dmgHit={_dmgHit}");

			EmitSignal(SignalName.HitMage, BeatIndexDebug, _dmgHit);
			_mage?.ApplyDamage(_dmgHit);
		}

		QueueFree();
	}
}
