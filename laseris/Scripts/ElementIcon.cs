using Godot;

public partial class ElementIcon : AnimatedSprite2D
{
	protected bool isActive = false;
	protected ElementController controller;

	public override void _Ready()
	{
		controller = GetNode<ElementController>("../../ElementController");
		Play("idle");
	}

	public void TryActivate()
	{
		if (isActive)
			return;

		if (!controller.CanActivate())
			return;

		controller.ActivateElement(this);
	}

	public void SetActive(bool value)
	{
		isActive = value;

		if (isActive)
			Play("activate");
		else
			Play("idle");
	}

	public void ResetElement()
	{
		isActive = false;
		Play("idle");
	}
}
