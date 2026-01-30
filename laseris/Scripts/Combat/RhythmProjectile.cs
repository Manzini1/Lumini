using Godot;
using System;

using Game.Characters; // MageController

namespace Game.Combat;

public partial class RhythmProjectile : Node2D
{
	[ExportGroup("Timing")]
	[Export] public float TravelToBlockSeconds = 0.40f;
	[Export] public float TravelToHitSeconds = 0.10f;
	[Export] public float HoldOnBlockSeconds = 0.12f;
	private bool _cancelled;
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

	// block state
	private bool _blockDamageApplied;
	private int _dmgOnBlockPending;

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

	// ✅ chamada pelo BattleController
	// ✅ chamada pelo BattleController
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

	if (_cancelled) return true; // ✅ já cancelado: considera como resolvido

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

	if (PassedBlock && insideLateGrace)
	{
		GlobalPosition = _block;
		if (DebugLogs)
			GD.Print($"[Proj] TryBlock OK (late) beat={BeatIndexDebug} grace={graceSeconds:0.000} -> forceSnapToBlock");
	}
	else
	{
		if (DebugLogs)
			GD.Print($"[Proj] TryBlock OK (early) beat={BeatIndexDebug} dmgBlock={_dmgOnBlockPending}");
	}

	return true;
}


	public override void _Process(double delta)
	{
		if (!_launched) return;

		double now = Time.GetTicksMsec() / 1000.0;

		// 1) antes do block
		if (now < _tBlockAbs)
		{
			float t = (float)((now - _t0) / TravelToBlockSeconds);
			t = Mathf.Clamp(t, 0f, 1f);
			GlobalPosition = _start.Lerp(_block, t);
			return;
		}

		// marca que passou do block (pra logs)
		if (!PassedBlock)
		{
			PassedBlock = true;
			if (DebugLogs)
				GD.Print($"[Proj] Passed BLOCK time beat={BeatIndexDebug} blocked={Blocked}");
		}

		// 2) chegou no block (uma vez)
		// se bloqueado: segura no block e some depois do hold
		if (Blocked)
		{
			GlobalPosition = _block;

			// segura um tempo e depois libera (free)
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

			_mage?.ApplyDamage(_dmgHit);
		}

		QueueFree();
	}
}
