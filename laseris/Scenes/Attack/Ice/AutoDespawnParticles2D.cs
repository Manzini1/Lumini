using Godot;

public partial class AutoDespawnParticles2D : Node
{
	[Export] public NodePath ParticlesPath = "P"; // filho GpuParticles2D (ou CpuParticles2D)
	[Export] public float ExtraSeconds = 0.10f;

	[Export] public bool AutoStart = true; // se true, liga emissão automaticamente no spawn

	public override void _Ready()
	{
		// 1) resolve o node de partículas
		Node p = null;

		if (ParticlesPath != null && !ParticlesPath.IsEmpty)
			p = GetNodeOrNull<Node>(ParticlesPath);

		// fallback: procura o primeiro GpuParticles2D/CpuParticles2D na cena
		p ??= FindFirstParticles(this);

		if (p == null)
		{
			GD.PushWarning("[AutoDespawnParticles2D] Não achei GpuParticles2D/CpuParticles2D na cena.");
			QueueFree();
			return;
		}

		// 2) liga e calcula tempo de vida
		float life = 0.4f;

		if (p is GpuParticles2D gpu)
		{
			gpu.OneShot = true;

			if (AutoStart)
			{
				gpu.Restart();
				gpu.Emitting = true;
			}

			life = (float)gpu.Lifetime; // ✅ cast double -> float
		}
		else if (p is CpuParticles2D cpu)
		{
			cpu.OneShot = true;

			if (AutoStart)
			{
				cpu.Restart();
				cpu.Emitting = true;
			}

			life = (float)cpu.Lifetime; // ✅ cast double -> float
		}
		else
		{
			GD.PushWarning($"[AutoDespawnParticles2D] Node encontrado não é Particles2D: {p.GetType().Name}");
			QueueFree();
			return;
		}

		// 3) agenda o despawn (com uma folga)
		float t = Mathf.Max(0.05f, life + ExtraSeconds);
		GetTree().CreateTimer(t).Timeout += () =>
		{
			if (GodotObject.IsInstanceValid(this))
				QueueFree();
		};
	}

	private static Node FindFirstParticles(Node start)
	{
		// tenta o próprio node
		if (start is GpuParticles2D || start is CpuParticles2D)
			return start;

		// recursivo nos filhos
		foreach (var obj in start.GetChildren())
		{
			if (obj is not Node child) continue;

			if (child is GpuParticles2D || child is CpuParticles2D)
				return child;

			var found = FindFirstParticles(child);
			if (found != null) return found;
		}

		return null;
	}
}
