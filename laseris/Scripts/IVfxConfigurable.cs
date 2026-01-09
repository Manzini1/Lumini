using Godot;

public interface IVfxConfigurable
{
	void Configure(SpellVfxEntry entry, Node2D caster, Node2D target);
}
