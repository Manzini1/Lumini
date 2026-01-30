using Godot;

namespace Game.Combat;

public partial class ElementVfxLibrary : Node
{
	// 0 ignorado; 1..7 (seu array tem tamanho 8)
	[ExportGroup("Mage cast animations (AnimationPlayer)")]
	[Export] public string[] MageCastAnimByElementId = new string[8];

	[ExportGroup("VFX Scenes")]
	[Export] public PackedScene[] CastVfxByElementId = new PackedScene[8];
	[Export] public PackedScene[] ImpactVfxByElementId = new PackedScene[8];

	[ExportGroup("Earth special")]
	[Export] public PackedScene EarthRockScene;

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

	public void SpawnEarthRock(Node parent, Vector2 ground, Vector2 hit)
	{
		if (EarthRockScene == null || parent == null) return;

		var inst = EarthRockScene.Instantiate();
		if (inst is not Node2D n2) { inst.QueueFree(); return; }

		parent.AddChild(n2);

		// ✅ chama Play(ground, hit) explicitamente
		if (inst.HasMethod("Play"))
			inst.Call("Play", ground, hit);
		else
			GD.PushWarning("[ElementVfxLibrary] EarthRockScene não tem método Play(ground, hit).");
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
		parent.AddChild(inst);

		// posiciona se der
		if (inst is Node2D n2) n2.GlobalPosition = globalPos;

		// ✅ autoplay por tipo (seguro)
		if (inst is AnimatedSprite2D asp)
		{
			// se não tiver animação setada, Play() tenta a atual; tudo bem
			if (!asp.IsPlaying()) asp.Play();
			return;
		}

		if (inst is GpuParticles2D gpu)
		{
			gpu.Emitting = true;
			return;
		}

		if (inst is CpuParticles2D cpu)
		{
			cpu.Emitting = true;
			return;
		}

		// ✅ Não chamamos inst.Call("Play") aqui de propósito:
		// porque pode existir Play(Vector2, Vector2) e aí quebra.
		// Para VFX custom, faça autoplay no próprio script (_Ready) ou exponha um PlaySimple() sem args
		if (inst.HasMethod("PlaySimple"))
			inst.Call("PlaySimple");
	}
}
