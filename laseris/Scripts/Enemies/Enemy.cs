using Godot;

public partial class Enemy : Node2D
{
	[Export] public EnemyData Data;
	[Export] public Sprite2D Sprite;

	private int _currentHP;
	private bool _isSelected = false;

	public override void _Ready()
	{
		if (Data == null)
		{
			GD.PushError($"{Name} está sem EnemyData!");
			return;
		}

		_currentHP = Data.MaxHP;
		GD.Print($"{Data.EnemyName} spawned with HP {_currentHP}");
	}

	public void TakeDamage(int baseDamage, float elementMultiplier = 1f)
	{
		int damage = Mathf.RoundToInt(baseDamage * elementMultiplier);
		_currentHP -= damage;
		_currentHP = Mathf.Max(_currentHP, 0);

		GD.Print($"{Data.EnemyName} tomou {damage} de dano. HP: {_currentHP}/{Data.MaxHP}");

		if (_currentHP <= 0)
			Die();
	}

	private void Die()
	{
		GD.Print($"{Data.EnemyName} morreu!");
		QueueFree();
	}

	public void SetSelected(bool selected)
	{
		_isSelected = selected;

		if (Sprite != null)
		{
			Sprite.Modulate = selected
				? new Color(1f, 1f, 0.6f) // highlight
				: Colors.White;
		}
	}
}
