using Godot;
using System;

public interface ISpellVfxConfigurable
{
	void Configure(SpellVfxEntry entry, Node2D caster, Node2D target);
}
