using Godot;
using System;

namespace Game.Vfx
{
	public partial class LightBladeBarrage : Node2D
	{
		[ExportGroup("Blade Setup")]
		[Export] public PackedScene BladeScene;
		[Export] public Texture2D[] BladeTextures = Array.Empty<Texture2D>();

		[ExportGroup("Spawn Formation (Arc)")]
		[Export] public Vector2 SpawnCenterOffset = new Vector2(0, -110); // sobe acima do mago
		[Export] public float ArcRadius = 140f;
		[Export] public float ArcStartAngleDeg = -160f; // mais pra esquerda
		[Export] public float ArcEndAngleDeg = -20f;    // mais pra direita

		[ExportGroup("Timing (Total ~0.8-1.0s)")]
		[Export] public int BladeCount = 4;

		// tempo que elas ficam visíveis "paradas" antes de começar a atirar
		[Export] public float PreWarmSeconds = 0.12f;

		// intervalo entre cada lâmina
		[Export] public float FireInterval = 0.10f;

		// duração do voo até o inimigo (IGNORA o travelSec externo se OverrideTravel=true)
		[Export] public bool OverrideTravel = true;
		[Export] public float BladeTravelSeconds = 0.28f;

		[Export] public float StickSeconds = 0.06f;
		[Export] public float FadeOutSeconds = 0.10f;

		[ExportGroup("Optional")]
		[Export] public NodePath BladesParentPath = "Blades";

		private Node2D _bladesParent;
		private int _token;

		public override void _Ready()
		{
			_bladesParent = GetNodeOrNull<Node2D>(BladesParentPath) ?? this;
		}

		// Chamado pela ElementVfxLibrary (Play(from,to,sec))
		public void Play(Vector2 from, Vector2 to, float travelSec)
		{
			int my = ++_token;

			if (BladeScene == null)
			{
				GD.PushWarning("[LightBladeBarrage] BladeScene NULL.");
				QueueFree();
				return;
			}

			int count = Mathf.Max(1, BladeCount);
			float usedTravel = OverrideTravel ? BladeTravelSeconds : Mathf.Max(0.01f, travelSec);

			// 1) spawn em formação (ficam paradas)
			for (int i = 0; i < count; i++)
				SpawnAndScheduleLaunch(i, count, from, to, usedTravel, my);

			// 2) despawn do container depois do fim
			float totalLife =
				PreWarmSeconds +
				(count - 1) * FireInterval +
				usedTravel +
				StickSeconds + FadeOutSeconds +
				0.10f;

			GetTree().CreateTimer(totalLife).Timeout += () =>
			{
				if (!GodotObject.IsInstanceValid(this)) return;
				if (my != _token) return;
				QueueFree();
			};
		}

		private void SpawnAndScheduleLaunch(int i, int count, Vector2 from, Vector2 to, float travelSec, int token)
		{
			var inst = BladeScene.Instantiate();
			if (inst is not LightBladeProjectile blade)
			{
				inst.QueueFree();
				GD.PushWarning("[LightBladeBarrage] BladeScene root não é LightBladeProjectile.");
				return;
			}

			_bladesParent.AddChild(blade);

			// textura i (se faltar, repete)
			Texture2D tex = null;
			if (BladeTextures != null && BladeTextures.Length > 0)
				tex = BladeTextures[Mathf.PosMod(i, BladeTextures.Length)];

			blade.Setup(tex, i);

			// posição no arco acima do mago
			Vector2 arcPos = ComputeArcPos(from, i, count);
			blade.GlobalPosition = arcPos;

			// aguarda PreWarm + i*interval e dispara
			float delay = Mathf.Max(0f, PreWarmSeconds + (i * FireInterval));

			GetTree().CreateTimer(delay).Timeout += () =>
			{
				if (!GodotObject.IsInstanceValid(this)) return;
				if (token != _token) return;
				if (!GodotObject.IsInstanceValid(blade)) return;

				blade.Launch(arcPos, to, travelSec, StickSeconds, FadeOutSeconds);
			};
		}

		private Vector2 ComputeArcPos(Vector2 from, int i, int count)
		{
			float t = (count <= 1) ? 0.5f : (float)i / (count - 1); // 0..1

			float angDeg = Mathf.Lerp(ArcStartAngleDeg, ArcEndAngleDeg, t);
			float ang = Mathf.DegToRad(angDeg);

			Vector2 center = from + SpawnCenterOffset;
			Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

			return center + dir * ArcRadius;
		}
	}
}
	
