using Godot;
using System;

public partial class EarthRockVfx : Node2D
{
	[Export] public NodePath SpritePath = "AnimatedSprite2D";

	[ExportGroup("Timing")]
	[Export] public float RiseOffsetPx = -18f;
	[Export] public float RiseTime = 0.10f;
	[Export] public float FlyMinTime = 0.05f;
	[Export] public float FlyMaxTime = 0.14f;
	[Export] public float FlySpeedPxPerSec = 2600f;

	[ExportGroup("Optional Extra Impact")]
	[Export] public PackedScene BreakImpactScene;  // <- coloque aqui um impactzinho (particles/anim)
	[Export] public bool SpawnImpactUnderParent = true;

	private AnimatedSprite2D _spr;
	private Tween _tween;

	public override void _Ready()
	{
		_spr = GetNodeOrNull<AnimatedSprite2D>(SpritePath);
	}

	private void KillTween()
	{
		if (_tween != null && GodotObject.IsInstanceValid(_tween))
			_tween.Kill();
		_tween = null;
	}

	private void SpawnBreakImpact(Vector2 hit)
	{
		if (BreakImpactScene == null) return;

		var inst = BreakImpactScene.Instantiate();
		if (inst is not Node node) { inst.QueueFree(); return; }

		Node parent = SpawnImpactUnderParent ? GetParent() : this;
		parent.AddChild(node);

		if (node is Node2D n2) n2.GlobalPosition = hit;
		else if (node is Control c) c.GlobalPosition = hit;

		// tenta autoplay
		if (node is GpuParticles2D gpu) gpu.Emitting = true;
		else if (node is CpuParticles2D cpu) cpu.Emitting = true;
		else if (node is AnimatedSprite2D asp) asp.Play();
		else if (node is AnimationPlayer ap)
		{
			var list = ap.GetAnimationList();
			if (list.Length > 0) ap.Play(list[0]);
		}
	}

	public async void Play(Vector2 ground, Vector2 hit)
	{
		if (_spr == null) return;

		KillTween();
		GlobalPosition = ground;
		Visible = true;

		if (_spr.SpriteFrames != null && _spr.SpriteFrames.HasAnimation("rise"))
			_spr.Play("rise");

		_tween = CreateTween();
		_tween.TweenProperty(this, "global_position", ground + new Vector2(0, RiseOffsetPx), RiseTime);
		await ToSignal(_tween, Tween.SignalName.Finished);

		if (_spr.SpriteFrames != null && _spr.SpriteFrames.HasAnimation("fly"))
			_spr.Play("fly");

		float dist = (ground - hit).Length();
		float flyTime = Mathf.Clamp(dist / FlySpeedPxPerSec, FlyMinTime, FlyMaxTime);

		KillTween();
		_tween = CreateTween();
		_tween.TweenProperty(this, "global_position", hit, flyTime);
		await ToSignal(_tween, Tween.SignalName.Finished);

		// ✅ chegou no hit: spawn impact extra + break
		SpawnBreakImpact(hit);

		if (_spr.SpriteFrames != null && _spr.SpriteFrames.HasAnimation("break"))
			_spr.Play("break");

		await ToSignal(_spr, AnimatedSprite2D.SignalName.AnimationFinished);
		QueueFree();
	}
}
