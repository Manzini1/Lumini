using Godot;

namespace Game.Battle;

public partial class AttackPattern : Node
{
	[Export] public int ElementCount = 4;

	public int GetRequiredElement(int beatIndex, TurnSide side)
	{
		// determinístico e simples (sem RNG instável)
		// muda o offset dependendo do lado, mas mantém previsibilidade
		int offset = (side == TurnSide.Enemy) ? 1 : 3;
		int v = beatIndex * 1103515245 + offset * 12345;
		if (ElementCount <= 0) ElementCount = 4;
		return (Mathf.Abs(v) % ElementCount) + 1; // 1..ElementCount
	}
}
