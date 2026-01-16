using Godot;

namespace Game.Battle;

public static class AudioClock
{
	// Tempo mais confiável pra sincronizar (compensa mix + latency)
	public static double GetSongTimeSeconds(AudioStreamPlayer player)
	{
		if (player == null) return 0;

		double t = player.GetPlaybackPosition();
		t += AudioServer.GetTimeSinceLastMix();
		t -= AudioServer.GetOutputLatency();
		return t;
	}
}
