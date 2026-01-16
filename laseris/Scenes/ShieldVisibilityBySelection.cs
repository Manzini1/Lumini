using Godot;

public partial class ShieldVisibilityBySelection : Node
{
	private Enemy _enemy;

	public override void _Ready()
	{
		_enemy = GetParent() as Enemy;
		if (_enemy == null)
		{
			_enemy = GetOwner() as Enemy;
		}

		if (_enemy == null)
		{
			GD.PushWarning("[ShieldVisibilityBySelection] Não achei Enemy (parent/owner).");
			return;
		}

		// estado inicial
		GetParent<Node>()?.SetDeferred("visible", _enemy.IsSelected);

	//	_enemy.SelectedChanged += OnSelectedChanged;
	}

	public override void _ExitTree()
	{
//		if (_enemy != null)
//			_enemy.SelectedChanged -= OnSelectedChanged;
	}

	private void OnSelectedChanged(Enemy who, bool selected)
	{
		// esse node é o pai do visual do shield (ou o próprio visual)
		GetParent<Node>()?.SetDeferred("visible", selected);
	}
}
