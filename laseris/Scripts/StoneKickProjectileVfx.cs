using Godot;
using System;
using System.Threading.Tasks;

namespace Game.Vfx
{
	public partial class StoneKickProjectileVfx : Node2D
	{
		[ExportGroup("Refs")]
		[Export] public NodePath FormAnimPath = "FormAnim";     // AnimatedSprite2D (pedra formando)
		[Export] public NodePath RockPath = "Rock";             // Node2D (Sprite2D / AnimatedSprite2D / etc.)
		[Export] public NodePath TrailPath = "Trail";           // opcional (Particles)
		[Export] public NodePath DustKickPath = "DustKick";     // opcional (Particles)
		[Export] public NodePath DustFlightPath = "DustFlight"; // opcional (Particles)

		[ExportGroup("Dust Kick Timing")]
		[Export] public float DustKickStartOffsetSec = 0.00f; // relativo ao momento do chute (>=0 recomendado)
		[Export] public float DustKickDurationSec = 0.08f;    // tempo da poeira no chão
		[ExportGroup("Impact")]
		[Export] public PackedScene ImpactScene;
		[Export] public NodePath ImpactParentPath = "/root/Main/Battle/World/Vfx"; // opcional
		[Export] public Vector2 ImpactOffset = Vector2.Zero;
		[ExportGroup("Form (Stone)")]
		[Export] public string FormAnimName = "Form";
		[Export] public float FormFallbackSec = 0.20f; // se não der pra estimar pelo SpriteFrames
		[Export] public bool HideRockWhileForming = true;
		[Export] public bool HideFormAnimAfterForm = true;

		[ExportGroup("Mage Kick (sync)")]
		// Pode apontar para AnimatedSprite2D OU AnimationPlayer na cena principal
		[Export] public NodePath MageAnimPlayerPath = "World/Characters/Mage/Sprite";
		[Export] public string MageKickAnimName = "KickRock";

		// Quanto tempo após o chute a pedra é lançada
		[Export] public float KickImpactDelaySec = 0.10f;

		[ExportGroup("Projectile Move")]
		[Export] public float DefaultTravelSec = 0.20f;
		[Export] public bool RotateToTravelDir = false; // false se você já ajustou a rotação na cena
		[Export] public Tween.TransitionType MoveTrans = Tween.TransitionType.Quad;
		[Export] public Tween.EaseType MoveEase = Tween.EaseType.Out;

		[ExportGroup("Debug")]
		[Export] public bool DebugLogs = false;

		private AnimatedSprite2D _formAnim;
		private Node2D _rock;
		private Node _trail;
		private Node _dustKick;
		private Node _dustFlight;

		private Tween _tw;
		private Vector2 _from;
		private Vector2 _to;
		private float _travelSec;

		// token para invalidar timers/awaits antigos se Play() for chamado novamente
		private ulong _playSeq = 0;

		public override void _Ready()
		{
			_formAnim = GetNodeOrNull<AnimatedSprite2D>(FormAnimPath);
			_rock = GetNodeOrNull<Node2D>(RockPath);

			_trail = GetNodeOrNull<Node>(TrailPath);
			_dustKick = GetNodeOrNull<Node>(DustKickPath);
			_dustFlight = GetNodeOrNull<Node>(DustFlightPath);

			// estado inicial seguro
			SetNodeEmitting(_trail, false, restartWhenOn: false);
			SetNodeEmitting(_dustKick, false, restartWhenOn: false);
			SetNodeEmitting(_dustFlight, false, restartWhenOn: false);
		}

		/// <summary>
		/// Compatível com ElementVfxLibrary.SpawnCastProjectile / SpawnPlayerCast
		/// </summary>
		public async void Play(Vector2 from, Vector2 to, float travelSec = -1f)
		{
			_playSeq++;
			ulong seq = _playSeq;

			_from = from;
			_to = to;
			_travelSec = (travelSec > 0f) ? travelSec : DefaultTravelSec;

			_tw?.Kill();
			GlobalPosition = from;

			// reset visual/particles
			SetNodeEmitting(_trail, false, restartWhenOn: false);
			SetNodeEmitting(_dustKick, false, restartWhenOn: false);
			SetNodeEmitting(_dustFlight, false, restartWhenOn: false);

			if (_rock != null && HideRockWhileForming)
				_rock.Visible = false;

			if (_formAnim != null)
				_formAnim.Visible = true;

			float formSec = PlayFormAndGetDuration();

			if (DebugLogs)
				GD.Print($"[StoneKick] Play from={from} to={to} formSec={formSec:0.000} kickDelay={KickImpactDelaySec:0.000}");

			// 1) Espera formar a pedra
			if (!await WaitSecondsSeq(formSec, seq)) return;

			if (!IsSeqValid(seq)) return;

			if (_rock != null)
				_rock.Visible = true;

			if (_formAnim != null && HideFormAnimAfterForm)
			{
				_formAnim.Stop();
				_formAnim.Visible = false;
			}

			// 2) Toca animação da maga (chute)
			PlayMageKick();

			// 3) Poeira do chute (sincronizada com o chute, não com a formação)
			float dustOffset = Mathf.Max(0f, DustKickStartOffsetSec);
			if (dustOffset > 0f)
			{
				if (!await WaitSecondsSeq(dustOffset, seq)) return;
			}

			if (!IsSeqValid(seq)) return;
			StartDustKickBurst(seq);

			// 4) Após o "impact delay" do chute -> lança a pedra
			float afterKickToLaunch = Mathf.Max(0.001f, KickImpactDelaySec - dustOffset);
			if (!await WaitSecondsSeq(afterKickToLaunch, seq)) return;

			if (!IsSeqValid(seq)) return;
			LaunchRock(seq);
		}

		private float PlayFormAndGetDuration()
		{
			if (_formAnim == null || _formAnim.SpriteFrames == null)
				return Mathf.Max(0.01f, FormFallbackSec);

			if (!_formAnim.SpriteFrames.HasAnimation(FormAnimName))
				return Mathf.Max(0.01f, FormFallbackSec);

			_formAnim.Visible = true;
			_formAnim.Play(FormAnimName);

			// estimativa de duração: frames / fps * loops (aqui assumindo 1 loop)
			int frames = _formAnim.SpriteFrames.GetFrameCount(FormAnimName);
			float fps = (float)_formAnim.SpriteFrames.GetAnimationSpeed(FormAnimName);
			fps *= Mathf.Max(0.001f, _formAnim.SpeedScale);

			if (frames <= 0 || fps <= 0.001f)
				return Mathf.Max(0.01f, FormFallbackSec);

			float estimated = frames / fps;
			return Mathf.Max(0.05f, estimated);
		}

		private void PlayMageKick()
		{
			var root = GetTree()?.CurrentScene;
			if (root == null)
			{
				if (DebugLogs) GD.PushWarning("[StoneKick] CurrentScene é null.");
				return;
			}

			Node mageAnimNode = null;
			if (MageAnimPlayerPath != null && !MageAnimPlayerPath.IsEmpty)
				mageAnimNode = root.GetNodeOrNull<Node>(MageAnimPlayerPath);

			// 1) Se apontou direto para AnimatedSprite2D
			if (mageAnimNode is AnimatedSprite2D asp)
			{
				if (asp.SpriteFrames != null && asp.SpriteFrames.HasAnimation(MageKickAnimName))
				{
					asp.Play(MageKickAnimName);
					if (DebugLogs) GD.Print($"[StoneKick] Mage AnimatedSprite2D.Play('{MageKickAnimName}')");
				}
				else
				{
					GD.PushWarning($"[StoneKick] Mage AnimatedSprite2D não tem anim '{MageKickAnimName}'. Path='{MageAnimPlayerPath}'");
				}
				return;
			}

			// 2) Se apontou para AnimationPlayer
			if (mageAnimNode is AnimationPlayer ap)
			{
				if (ap.HasAnimation(MageKickAnimName))
				{
					ap.Play(MageKickAnimName);
					if (DebugLogs) GD.Print($"[StoneKick] Mage AnimationPlayer.Play('{MageKickAnimName}')");
				}
				else
				{
					GD.PushWarning($"[StoneKick] Mage AnimationPlayer não tem anim '{MageKickAnimName}'. Path='{MageAnimPlayerPath}'");
				}
				return;
			}

			// 3) Fallbacks úteis (caso o path esteja apontando pro nó errado)
			var mage = root.GetNodeOrNull<Node>("World/Characters/Mage");
			if (mage != null)
			{
				var foundAsp = mage.FindChild("Sprite", recursive: true, owned: false) as AnimatedSprite2D;
				if (foundAsp != null && foundAsp.SpriteFrames != null && foundAsp.SpriteFrames.HasAnimation(MageKickAnimName))
				{
					foundAsp.Play(MageKickAnimName);
					if (DebugLogs) GD.Print($"[StoneKick] Fallback Sprite.Play('{MageKickAnimName}')");
					return;
				}

				var foundAp = mage.FindChild("AnimationPlayer", recursive: true, owned: false) as AnimationPlayer;
				if (foundAp != null && foundAp.HasAnimation(MageKickAnimName))
				{
					foundAp.Play(MageKickAnimName);
					if (DebugLogs) GD.Print($"[StoneKick] Fallback AnimationPlayer.Play('{MageKickAnimName}')");
					return;
				}
			}

			GD.PushWarning($"[StoneKick] Não consegui resolver o nó de animação da maga. Path='{MageAnimPlayerPath}'.");
		}

		private void StartDustKickBurst(ulong seq)
		{
			if (!IsSeqValid(seq)) return;

			if (DebugLogs)
				GD.Print($"[StoneKick] DustKick ON (dur={DustKickDurationSec:0.000}s)");

			// liga e restarta para garantir burst visível
			SetNodeEmitting(_dustKick, true, restartWhenOn: true);

			float dur = Mathf.Max(0f, DustKickDurationSec);
			if (dur <= 0f)
			{
				SetNodeEmitting(_dustKick, false, restartWhenOn: false);
				return;
			}

			var timer = GetTree().CreateTimer(dur);
			timer.Timeout += () =>
			{
				if (!IsSeqValid(seq)) return;

				SetNodeEmitting(_dustKick, false, restartWhenOn: false);

				if (DebugLogs)
					GD.Print("[StoneKick] DustKick OFF");
			};
		}

		private void LaunchRock(ulong seq)
		{
			if (!IsSeqValid(seq)) return;

			// Poeira do chute só no chão / instante do chute
			SetNodeEmitting(_dustKick, false, restartWhenOn: false);

			// Efeitos de voo
			SetNodeEmitting(_dustFlight, true, restartWhenOn: true);
			SetNodeEmitting(_trail, true, restartWhenOn: true);

			if (RotateToTravelDir)
				Rotation = (_to - _from).Angle();

			if (DebugLogs)
				GD.Print($"[StoneKick] LaunchRock {_from} -> {_to} travel={_travelSec:0.000}");

			_tw?.Kill();
			_tw = CreateTween();
			_tw.SetTrans(MoveTrans);
			_tw.SetEase(MoveEase);

			_tw.TweenProperty(this, "global_position", _to, Mathf.Max(0.01f, _travelSec));
			SpawnImpactAt(_to + ImpactOffset);
			_tw.Finished += () =>
			{
				if (!GodotObject.IsInstanceValid(this)) return;
				if (!IsSeqValid(seq)) return;

				SetNodeEmitting(_trail, false, restartWhenOn: false);
				SetNodeEmitting(_dustFlight, false, restartWhenOn: false);

				if (DebugLogs)
					GD.Print("[StoneKick] Arrived -> QueueFree");
			
				QueueFree();
			};
		}
	private void SpawnImpactAt(Vector2 pos)
{
	if (ImpactScene == null) return;

	var raw = ImpactScene.Instantiate();
	if (raw is not Node node)
	{
		raw.QueueFree();
		return;
	}

	Node parent = null;

	// tenta parent configurado
	if (ImpactParentPath != null && !ImpactParentPath.IsEmpty)
		parent = GetTree()?.CurrentScene?.GetNodeOrNull<Node>(ImpactParentPath);

	// fallback bom
	parent ??= GetTree()?.CurrentScene?.GetNodeOrNull<Node>("World/Vfx");
	parent ??= GetParent();
	parent ??= this;

	parent.AddChild(node);

	if (node is Node2D n2) n2.GlobalPosition = pos;
	else if (node is Control c) c.GlobalPosition = pos;

	// chama Play se existir
	if (node.HasMethod("Play"))
		node.Call("Play");
}
		private async Task<bool> WaitSecondsSeq(float seconds, ulong seq)
		{
			if (!IsSeqValid(seq)) return false;

			float s = Mathf.Max(0f, seconds);
			if (s <= 0f) return IsSeqValid(seq);

			await ToSignal(GetTree().CreateTimer(s), SceneTreeTimer.SignalName.Timeout);
			return IsSeqValid(seq);
		}

		private bool IsSeqValid(ulong seq)
		{
			return GodotObject.IsInstanceValid(this) && seq == _playSeq;
		}

		private static void SetNodeEmitting(Node node, bool on, bool restartWhenOn)
		{
			if (node == null || !GodotObject.IsInstanceValid(node)) return;

			if (node is GpuParticles2D gpu)
			{
				if (on && restartWhenOn)
					gpu.Restart();

				gpu.Emitting = on;
				return;
			}

			if (node is CpuParticles2D cpu)
			{
				if (on && restartWhenOn)
					cpu.Restart();

				cpu.Emitting = on;
				return;
			}

			// Se for container com partículas dentro
			foreach (var c in node.GetChildren())
			{
				if (c is Node child)
					SetNodeEmitting(child, on, restartWhenOn);
			}
		}
	}
}
