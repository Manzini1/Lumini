using Godot;

namespace Game.Combat;

public partial class ElementVfxLibrary : Node
{
	// 0 ignorado; 1..7 (array tamanho 8)
	[ExportGroup("Mage cast animations (AnimationPlayer)")]
	[Export] public string[] MageCastAnimByElementId = new string[8];

	[ExportGroup("VFX Scenes")]
	[Export] public PackedScene[] CastVfxByElementId = new PackedScene[8];

	// Base + até 2 alternates (total 3 variações)
	[Export] public PackedScene[] ImpactVfxByElementId = new PackedScene[8];
	[Export] public PackedScene[] ImpactVfxAlt1ByElementId = new PackedScene[8];
	[Export] public PackedScene[] ImpactVfxAlt2ByElementId = new PackedScene[8];

	[ExportGroup("Impact Fallback (optional)")]
	[Export] public PackedScene DefaultAttackImpact; // usado se tudo estiver null

	[ExportGroup("Earth special")]
	[Export] public PackedScene EarthRockScene;

	private RandomNumberGenerator _rng = new();

	public override void _Ready()
	{
		_rng.Randomize();
	}
	
	public string GetMageCastAnim(int elementId)
	{
		if (MageCastAnimByElementId == null) return "";
		if (elementId < 1 || elementId >= MageCastAnimByElementId.Length) return "";
		return MageCastAnimByElementId[elementId] ?? "";
	}

	public void SpawnCastVfx(int elementId, Node parent, Vector2 globalPos)
	{
		var scene = GetScene(CastVfxByElementId, elementId);
		SpawnScene(scene, parent, globalPos);
	}

	public void SpawnImpactVfx(int elementId, Node parent, Vector2 globalPos)
	{
		var scene = GetScene(ImpactVfxByElementId, elementId);
		SpawnScene(scene, parent, globalPos);
	}

	// ✅ NOVO: random impact (até 3 variações; funciona com 1 também)
	public void SpawnAttackImpactRandom(int elementId, Node parent, Vector2 globalPos)
	{
		if (parent == null) return;

		PackedScene a = GetScene(ImpactVfxByElementId, elementId);
		PackedScene b = GetScene(ImpactVfxAlt1ByElementId, elementId);
		PackedScene c = GetScene(ImpactVfxAlt2ByElementId, elementId);

		// conta quantos existem
		int count = 0;
		if (a != null) count++;
		if (b != null) count++;
		if (c != null) count++;

		PackedScene chosen = null;

		if (count == 0)
		{
			chosen = DefaultAttackImpact;
		}
		else if (count == 1)
		{
			chosen = (a ?? b) ?? c;
		}
		else
		{
			// escolhe um índice entre os existentes
			int pick = (int)_rng.RandiRange(0, count - 1);
			// mapeia pick para (a,b,c) pulando null
			chosen = PickNthNonNull(a, b, c, pick);
		}

		SpawnScene(chosen, parent, globalPos);
	}

	private static PackedScene PickNthNonNull(PackedScene a, PackedScene b, PackedScene c, int n)
	{
		// n é 0..count-1 considerando apenas não-nulos
		if (a != null)
		{
			if (n == 0) return a;
			n--;
		}
		if (b != null)
		{
			if (n == 0) return b;
			n--;
		}
		if (c != null)
		{
			if (n == 0) return c;
			n--;
		}
		return null;
	}

public void SpawnEarthRock(Node parent, Vector2 ground, Vector2 hit)
{
	if (EarthRockScene == null || parent == null) return;

	var inst = EarthRockScene.Instantiate();
	if (inst is not Node node) { inst.QueueFree(); return; }

	parent.AddChild(node);

	if (node is Node2D n2) n2.GlobalPosition = ground;

	// prioridade: Play(ground, hit)
	if (node.HasMethod("Play"))
	{
		node.Call("Play", ground, hit);
		return;
	}

	// fallback: PlaySimple
	if (node.HasMethod("PlaySimple"))
	{
		node.Call("PlaySimple");
		return;
	}

	// fallback final: autoplay hierarquia
	TryAutoPlayInHierarchy(node);
}


	private static PackedScene GetScene(PackedScene[] arr, int id)
	{
		if (arr == null) return null;
		if (id < 1 || id >= arr.Length) return null;
		return arr[id];
	}

	private static void SpawnScene(PackedScene scene, Node parent, Vector2 globalPos)
{
	if (scene == null || parent == null) return;

	var inst = scene.Instantiate();
	if (inst is not Node node)
	{
		inst.QueueFree();
		return;
	}

	parent.AddChild(node);

	// posiciona se der
	if (node is Node2D n2) n2.GlobalPosition = globalPos;
	else if (node is Control c) c.GlobalPosition = globalPos;

	// ✅ tenta autoplay (root + filhos)
	if (TryAutoPlayInHierarchy(node))
		return;

	// fallback “seguro” pra cenas custom
	if (node.HasMethod("PlaySimple"))
		node.Call("PlaySimple");
}

// ✅ procura e dá Play no primeiro “componente animável” que encontrar
private static bool TryAutoPlayInHierarchy(Node node)
{
	if (node == null) return false;

	// AnimatedSprite2D
	if (node is AnimatedSprite2D asp)
	{
		if (!asp.IsPlaying()) asp.Play();
		return true;
	}

	// Particles
	if (node is GpuParticles2D gpu)
	{
		gpu.Emitting = true;
		return true;
	}
	if (node is CpuParticles2D cpu)
	{
		cpu.Emitting = true;
		return true;
	}

	// AnimationPlayer (toca a primeira animação disponível)
	if (node is AnimationPlayer ap)
	{
		var list = ap.GetAnimationList();
		if (list != null && list.Length > 0)
		{
			ap.Play(list[0]);
			return true;
		}
	}

	// recursivo nos filhos
	foreach (var childObj in node.GetChildren())
	{
		if (childObj is Node child && TryAutoPlayInHierarchy(child))
			return true;
	}

	return false;
}

}
