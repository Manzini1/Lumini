using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class BeatMap : Resource
{
	[Export] public Godot.Collections.Array<float> Beats = new();

	public int Count => Beats?.Count ?? 0;

	public float Get(int idx)
	{
		if (Beats == null || idx < 0 || idx >= Beats.Count) return 0f;
		return Beats[idx];
	}

	// encontra o beat mais próximo do tempo atual
	public int FindNearestIndex(float t, int startFrom = 0)
	{
		if (Beats == null || Beats.Count == 0) return -1;

		int best = Mathf.Clamp(startFrom, 0, Beats.Count - 1);
		float bestDist = Mathf.Abs(Beats[best] - t);

		for (int i = best + 1; i < Beats.Count; i++)
		{
			float d = Mathf.Abs(Beats[i] - t);
			if (d <= bestDist) { best = i; bestDist = d; }
			else break; // como beats são crescentes, d vai começar a piorar
		}
		return best;
	}
}
