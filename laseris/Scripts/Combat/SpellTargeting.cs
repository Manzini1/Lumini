using System;

[Flags]
public enum SpellTargeting
{
	None   = 0,
	Ground = 1 << 0,
	Air    = 1 << 1,
	Both   = Ground | Air
}
