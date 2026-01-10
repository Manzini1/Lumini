using Godot;
using System;

public partial class TugManager : Node
{
	/// <summary>
	/// Pontos líquidos do tug: negativo favorece o inimigo (player perto de stun),
	/// positivo favorece o player (inimigo perto de stun).
	/// </summary>
	public float Points { get; private set; } = 0f;

	/// <summary>
	/// Valor normalizado (-1..+1) usado pelo HUD.
	/// </summary>
	public float Value
	{
		get
		{
			float d = Mathf.Max(0.0001f, PointsToBreak);
			return Mathf.Clamp(Points / d, -1f, 1f);
		}
	}

	[ExportCategory("Config (Balance)")]
	[Export] public float PointsToBreak = 7f; // <- “pressure = 5” que você quer

	[ExportCategory("Decay (opcional)")]
	[Export] public bool EnableDecay = false;
	[Export] public float DecayPointsPerSecond = 0.0f; // pontos por segundo voltando ao 0
	[Export] public float DeadZonePoints = 0.01f;

	[ExportCategory("Break")]
	[Export] public float BreakLockSeconds = 0.6f;
	[Export] public bool AutoCenterAfterBreak = true;

	[ExportCategory("Freeze")]
	[Export] public bool IsFrozen { get; private set; } = false;
	private float _freezeTimer = 0f;

	private float _lockTimer = 0f;

	public event Action<float> Changed;     // envia Value (-1..+1)
	public event Action<TugBreak> Broken;   // bateu no limite

	public override void _Ready()
	{
		AddToGroup("tug_manager");
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		// freeze
		if (IsFrozen)
		{
			_freezeTimer = Mathf.Max(0f, _freezeTimer - dt);
			if (_freezeTimer <= 0f)
				IsFrozen = false;
		}

		// lock de break
		if (_lockTimer > 0f)
			_lockTimer = Mathf.Max(0f, _lockTimer - dt);

		// decay (só se não estiver frozen)
		if (IsFrozen) return;
		if (!EnableDecay) return;

		if (Mathf.Abs(Points) <= DeadZonePoints)
		{
			if (Points != 0f)
				SetPoints(0f, "decay->zero");
			return;
		}

		float sign = Mathf.Sign(Points);
		float next = Points - sign * DecayPointsPerSecond * dt;

		if (Mathf.Sign(next) != sign)
			next = 0f;

		SetPoints(next, "decay");
	}

	public void Freeze(float seconds, string reason = "")
	{
		IsFrozen = true;
		_freezeTimer = Mathf.Max(_freezeTimer, Mathf.Max(0.05f, seconds));
		// GD.Print($"[Tug] FREEZE {seconds:0.00}s reason={reason}");
	}

	public void ResetToCenter(string reason = "reset")
	{
		SetPoints(0f, reason);
	}

	/// <summary>
	/// Push em PONTOS (não normalizado).
	/// Ex: +1 hit, -1 miss, -2 absorbed, etc.
	/// </summary>
	public void Push(float points, string reason = "")
	{
		if (Mathf.Abs(points) <= 0.0001f) return;
		if (IsFrozen) return;

		float cap = Mathf.Max(0.0001f, PointsToBreak);
		float next = Mathf.Clamp(Points + points, -cap, +cap);
		SetPoints(next, reason);

		// break?
		if (_lockTimer > 0f) return;
		if (Mathf.Abs(Points) >= cap)
		{
			_lockTimer = BreakLockSeconds;

			var loser = (Points >= 0f) ? TugLoser.Enemy : TugLoser.Player;
			Broken?.Invoke(new TugBreak(loser, Points, reason));

			if (AutoCenterAfterBreak)
				SetPoints(0f, "auto-center after break");
		}
	}

	private void SetPoints(float p, string reason)
	{
		if (Mathf.Abs(p - Points) <= 0.000001f) return;
		Points = p;
		Changed?.Invoke(Value);
		// GD.Print($"[Tug] Points={Points:0.00}/{PointsToBreak:0.00} Value={Value:0.000} reason={reason}");
	}

	public readonly struct TugBreak
	{
		public readonly TugLoser Loser;
		public readonly float FinalPoints;
		public readonly string Reason;

		public TugBreak(TugLoser loser, float finalPoints, string reason)
		{
			Loser = loser;
			FinalPoints = finalPoints;
			Reason = reason;
		}
	}

	public enum TugLoser
	{
		Player,
		Enemy
	}
}
