using Godot;
using System;

public partial class TugManager : Node
{
	/// <summary>
	/// Valor do “puxar a corda”: -1 (inimigo venceu => player sofre) ... +1 (player venceu => inimigo sofre).
	/// </summary>
	public float Value { get; private set; } = 0f;

	[ExportCategory("Config")]
	[Export] public float ClampMin = -1f;
	[Export] public float ClampMax = 1f;

	[ExportCategory("Decay (opcional)")]
	[Export] public bool EnableDecay = true;
	[Export] public float DecayPerSecond = 0.01f; // puxa de volta pro 0
	[Export] public float DeadZone = 0.002f;

	[ExportCategory("Break (stun trigger)")]
	[Export] public bool EnableBreakEvents = true;
	[Export] public float BreakThreshold = 1.0f;     // quando |Value| >= threshold
	[Export] public float BreakLockSeconds = 0.6f;    // evita estourar várias vezes seguidas
	[Export] public bool AutoCenterAfterBreak = true; // reseta Value pro 0 ao estourar

	private float _lockTimer = 0f;

	public event Action<float> Changed;   // Value mudou
	public event Action<TugBreak> Broken; // bateu no limite

	public override void _Process(double delta)
	{
		float dt = (float)delta;

		if (_lockTimer > 0f)
			_lockTimer = Mathf.Max(0f, _lockTimer - dt);

		if (!EnableDecay) return;
		if (Mathf.Abs(Value) <= DeadZone)
		{
			if (Value != 0f)
				SetValue(0f, "decay->zero");
			return;
		}

		// decai em direção ao zero
		float sign = Mathf.Sign(Value);
		float next = Value - sign * DecayPerSecond * dt;

		// cruzou o zero -> zera
		if (Mathf.Sign(next) != sign)
			next = 0f;

		SetValue(next, "decay");
	}

	public void ResetToCenter(string reason = "reset")
	{
		SetValue(0f, reason);
	}

	/// <summary>
	/// Push positivo favorece o player (tende a +1 => inimigo toma stun).
	/// Push negativo favorece o inimigo (tende a -1 => player toma stun).
	/// </summary>
	public void Push(float amount, string reason = "")
	{
		if (Mathf.Abs(amount) <= 0.0001f) return;

		float next = Mathf.Clamp(Value + amount, ClampMin, ClampMax);
		SetValue(next, reason);

		if (!EnableBreakEvents) return;
		if (_lockTimer > 0f) return;

		if (Mathf.Abs(Value) >= BreakThreshold)
		{
			_lockTimer = BreakLockSeconds;

			var whoLost = (Value >= 0f) ? TugLoser.Enemy : TugLoser.Player;
			Broken?.Invoke(new TugBreak(whoLost, Value, reason));

			if (AutoCenterAfterBreak)
				SetValue(0f, "auto-center after break");
		}
	}

	private void SetValue(float v, string reason)
	{
		if (Mathf.Abs(v - Value) <= 0.000001f) return;
		Value = v;
		Changed?.Invoke(Value);
		// GD.Print($"[Tug] Value={Value:0.000} reason={reason}");
	}

	public readonly struct TugBreak
	{
		public readonly TugLoser Loser;
		public readonly float FinalValue;
		public readonly string Reason;

		public TugBreak(TugLoser loser, float finalValue, string reason)
		{
			Loser = loser;
			FinalValue = finalValue;
			Reason = reason;
		}
	}

	public enum TugLoser
	{
		Player,
		Enemy
	}
}
