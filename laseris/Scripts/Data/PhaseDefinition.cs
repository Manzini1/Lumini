using Godot;

namespace Game.Data;

[GlobalClass]
public partial class PhaseDefinition : Resource
{
	[Export] public string PhaseName = "Phase 01";
	[Export] public AudioStream Music;
	[Export] public string BeatmapJsonPath = "res://Data/Beatmaps/phase_01.json";

	[ExportGroup("Turns")]
	[Export] public float EnemyTurnBaseSeconds = 20f;
	[Export] public float PlayerTurnBaseSeconds = 20f;
	[Export] public float DefenseSuccessReduceEnemySeconds = 0.75f;
	[Export] public float PlayerMissReducePlayerSeconds = 0.75f;

	[ExportGroup("Timing")]
	[Export] public float PrepareLeadSeconds = 0.60f;
	[Export] public float HitWindowSeconds = 0.12f;

	[ExportGroup("Flow")]
	[Export] public int FlowMaxStacks = 10;
	[Export] public float FlowDamagePerStack = 0.08f;
}
