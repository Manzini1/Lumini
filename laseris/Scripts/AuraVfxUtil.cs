using Godot;

namespace Game.Combat
{
	public static class AuraVfxUtil
	{
		public static void ApplyAuraColor(Sprite2D body, GpuParticles2D drops, GpuParticles2D wisps, Color c)
		{
			// 1) Shader do "corpo líquido"
			if (body?.Material is ShaderMaterial sm)
				sm.SetShaderParameter("aura_color", c);

			// 2) Partículas (gotas/fiapos)
			ApplyParticlesColor(drops, c);
			ApplyParticlesColor(wisps, c);
		}

		private static void ApplyParticlesColor(GpuParticles2D p, Color c)
		{
			if (p == null) return;
			if (p.ProcessMaterial is ParticleProcessMaterial ppm)
				ppm.Color = c;
		}
	}
}
