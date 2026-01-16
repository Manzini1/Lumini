using Godot;

namespace Game.Combat;

public static class DamageService
{
	public static void Deal(Node target, int amount)
	{
		if (target == null) return;

		int dmg = Mathf.Max(0, amount);
		if (target.HasMethod("ApplyDamage"))
			target.Call("ApplyDamage", dmg);
	}
}
