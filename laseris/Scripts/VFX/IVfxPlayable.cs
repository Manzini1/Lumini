using System;
using Godot;

public interface IVfxPlayable
{
	/// Dispara no momento em que a magia “acertou” (impact timing).
	event Action Impacted;

	/// Configuração com entry + caster/target atuais.
	void Configure(SpellVfxEntry entry, Node2D caster, Node2D target);
}
