using Godot;

public partial class HealthComponent : Node
{
	[Signal] public delegate void HpChangedEventHandler(int current, int max);
	[Signal] public delegate void DiedEventHandler();

	public int MaxHp { get; private set; }
	public int CurrentHp { get; private set; }

	private bool _dead = false;

	public void Init(int maxHp)
	{
		MaxHp = Mathf.Max(1, maxHp);
		CurrentHp = MaxHp;
		_dead = false;
		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
	}

	public bool IsAlive()
	{
		return !_dead && CurrentHp > 0;
	}

	public void ApplyDamage(int amount)
	{
		if (_dead) return;

		amount = Mathf.Max(0, amount);
		CurrentHp = Mathf.Max(0, CurrentHp - amount);

		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);

		if (CurrentHp <= 0)
		{
			_dead = true;
			EmitSignal(SignalName.Died);
		}
	}

	public void Heal(int amount)
	{
		if (_dead) return;

		amount = Mathf.Max(0, amount);
		CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);

		EmitSignal(SignalName.HpChanged, CurrentHp, MaxHp);
	}
}
