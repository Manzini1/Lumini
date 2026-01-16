using Godot;

namespace Game.Data;

[GlobalClass]
public partial class CampaignDefinition : Resource
{
	[Export] public Godot.Collections.Array<PhaseDefinition> Phases = new();
}
