using Godot;
using System;

public interface ICastVfxPlayable
{
	void Play(Vector2 from, Vector2 to, float travelSec);
}
