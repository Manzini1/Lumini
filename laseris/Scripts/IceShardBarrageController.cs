using Godot;
using System.Collections.Generic;

namespace Game.Vfx
{
	public partial class IceShardBarrageController : Node2D
	{
		[ExportGroup("Scenes/Assets")]
		[Export] public PackedScene ShardScene;         // IceShardProjectile.tscn
		[Export] public Texture2D[] ShardTextures;

		[ExportGroup("AAA Wisp (optional)")]
		[Export] public PackedScene SpawnWispScene;     // SpawnWisp2D.tscn (opcional)
		[Export] public Color WispTint = new Color(0.55f, 0.85f, 1.0f, 1f);
		[Export] public float WispScale = 0.90f;
		[Export] public float WispLifetimeSec = 0.30f;

		[ExportGroup("Counts/Timing")]
		[Export] public int TotalShards = 52;

		// 6 frames @ 10fps => 0.60s de forming
		[Export] public float FormSec = 0.60f;

		// se quiser ver formar antes de atirar, deixe >= FormSec
		[Export] public float BuildUpSec = 0.62f;
		[Export] public float FireDuration = 0.75f;
		[Export] public float ShardTravelSec = 0.18f;

		[ExportGroup("BuildUp (Freeze the air)")]
		[Export] public int BuildUpBreaths = 2;
		[Export] public float BuildUpWobblePx = 10f;
		[Export] public float BuildUpScaleAmp = 0.08f;
		[Export] public float BuildUpRotAmpDeg = 10f;
		[Export] public float BuildUpStaggerMax = 0.10f;

		[ExportGroup("Arc Spawn (acima do mago)")]
		[Export] public Vector2 ArcCenterOffset = new Vector2(10, -110);
		[Export] public float ArcRadiusMin = 80f;
		[Export] public float ArcRadiusMax = 200f;
		[Export] public float ArcMinDeg = -160f;
		[Export] public float ArcMaxDeg = -20f;
		[Export] public float SpawnJitterRadius = 18f;

		[ExportGroup("Fire Order (AAA)")]
		[Export] public bool FireOrderCircular = true;
		[Export] public bool FireOrderAlternateEnds = true;
		[Export] public int FireOrderJitterWindow = 6;

		[ExportGroup("Target Spread")]
		[Export] public float TargetRadius = 70f;

		[ExportGroup("Feel")]
		[Export] public float SpawnScaleMin = 0.55f;
		[Export] public float SpawnScaleMax = 0.95f;
		[Export] public float RotationJitterDeg = 18f;

		[ExportGroup("Optional impact burst")]
		[Export] public PackedScene ImpactBurstScene;

		[ExportGroup("SFX (optional)")]
		[Export] public AudioStream ShootSfx;
		[Export] public AudioStream ImpactSfx;
		[Export] public float SfxMinInterval = 0.035f;

		[ExportGroup("Damage Instances (visual)")]
		[Export] public PackedScene DamageInstanceScene;     // ex: DamagePopup2D.tscn
		[Export] public Vector2 DamageOffset = new Vector2(0, -12);
		[Export] public float DamageJitterPx = 10f;
		[Export] public float DamageLifetimeSec = 0.55f;

		[ExportGroup("Damage Notify (real HP) - optional")]
		[Export] public NodePath DamageReceiverPath;         // node com método pra aplicar dano real
		[Export] public string DamageReceiverMethod = "OnShardDamage"; // (int shardIndex, Vector2 hitPos)
		[Export] public bool NotifyDamageOnImpact = true;

		[ExportGroup("Debug")]
		[Export] public bool DebugLogs = false;

		private readonly RandomNumberGenerator _rng = new();

		private Vector2 _from;
		private Vector2 _target;

		private readonly List<IceShardProjectile> _shards = new();
		private readonly List<IceShardProjectile> _fireList = new();

		private readonly Dictionary<IceShardProjectile, Vector2> _basePos = new();
		private readonly Dictionary<IceShardProjectile, Tween> _buildTween = new();
		private readonly List<Node> _wisps = new();

		private float _fireEvery;
		private int _fireCursor;
		private bool _running;

		private AudioStreamPlayer _sfx;
		private ulong _lastSfxMs = 0;

		private Node _damageReceiver;

		private bool _initialized;

		public override void _Ready()
		{
			_initialized = true;
			_rng.Randomize();

			_sfx = GetNodeOrNull<AudioStreamPlayer>("Sfx");
			if (_sfx == null)
			{
				_sfx = new AudioStreamPlayer();
				_sfx.Name = "Sfx";
				AddChild(_sfx);
			}

			_damageReceiver = (DamageReceiverPath != null && !DamageReceiverPath.IsEmpty)
				? GetNodeOrNull<Node>(DamageReceiverPath)
				: null;

			SetProcess(false);
		}

		public void Play(Vector2 from, Vector2 to, float _ignoredTravelSec)
		{
			_from = from;
			_target = to;

			// safety: se Play vier antes de _Ready
			if (!_initialized)
			{
				CallDeferred(nameof(Play), from, to, _ignoredTravelSec);
				return;
			}

			_damageReceiver = (DamageReceiverPath != null && !DamageReceiverPath.IsEmpty)
				? GetNodeOrNull<Node>(DamageReceiverPath)
				: _damageReceiver;

			StopSequence();
			ClearOld();

			int count = Mathf.Max(1, TotalShards);
			_fireEvery = Mathf.Max(0.004f, FireDuration / count);
			_fireCursor = 0;
			_running = true;

			Vector2 arcCenter = _from + ArcCenterOffset;

			SpawnArcShards(arcCenter, count);
			StartBuildUpOnAll();
			BuildFireOrder(arcCenter);

			// espera pelo forming antes de começar a metralhar
			float wait = Mathf.Max(BuildUpSec, FormSec);
			if (DebugLogs) GD.Print($"[IceBarrage] start in {wait:0.00}s fireEvery={_fireEvery:0.000}");
			ScheduleNextFire(wait);
		}

		private void ScheduleNextFire(float delaySec)
		{
			var t = GetTree().CreateTimer(Mathf.Max(0.001f, delaySec));
			t.Timeout += () =>
			{
				if (!GodotObject.IsInstanceValid(this) || !_running) return;
				FireStep();
			};
		}

		private void FireStep()
		{
			if (_fireList.Count == 0)
			{
				_running = false;
				ScheduleCleanupCheck();
				return;
			}

			if (_fireCursor < _fireList.Count)
			{
				FireOne(_fireCursor);
				_fireCursor++;
			}

			if (_fireCursor < _fireList.Count)
				ScheduleNextFire(_fireEvery);
			else
			{
				_running = false;
				ScheduleCleanupCheck();
			}
		}

		private void SpawnArcShards(Vector2 arcCenter, int count)
		{
			if (ShardScene == null) return;

			for (int i = 0; i < count; i++)
			{
				var raw = ShardScene.Instantiate();
				if (raw is not IceShardProjectile shard)
				{
					raw.QueueFree();
					continue;
				}

				AddChild(shard);

				Texture2D tex = null;
				if (ShardTextures != null && ShardTextures.Length > 0)
					tex = ShardTextures[_rng.RandiRange(0, ShardTextures.Length - 1)];
				shard.Setup(tex, i);

				float u = (count == 1) ? 0.5f : (i / (float)(count - 1));
				u = EaseInOut(u);

				float angDeg = Mathf.Lerp(ArcMinDeg, ArcMaxDeg, u) + _rng.RandfRange(-6f, 6f);
				float r = _rng.RandfRange(ArcRadiusMin, ArcRadiusMax);
				float ang = Mathf.DegToRad(angDeg);
				Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));

				Vector2 p = arcCenter + dir * r + RandomInCircle(SpawnJitterRadius);
				float scale = _rng.RandfRange(SpawnScaleMin, SpawnScaleMax);

				_basePos[shard] = p;

				// ✅ chama a animação de formação (Form) do seu IceShardProjectile
				shard.ArmAt(p, scale);

				float wispLife = Mathf.Max(WispLifetimeSec, FormSec);
				SpawnWispAt(p, scale, wispLife);

				// ✅ quando chega no alvo: impact + “damage instance” (52 vezes) + opcional notify de dano real
				shard.ReachedTarget += (idx, at) =>
				{
					SpawnImpactBurstAt(at);
					PlayImpactSfxThrottled();

					SpawnDamageInstanceAt(at, idx);

					if (NotifyDamageOnImpact)
						NotifyDamageReceiver(idx, at);
				};

				_shards.Add(shard);
			}
		}

		private void StartBuildUpOnAll()
		{
			if (_shards.Count == 0) return;
			if (BuildUpSec <= 0.001f) return;

			float total = Mathf.Max(0.01f, BuildUpSec);
			int breaths = Mathf.Max(1, BuildUpBreaths);
			float perBreath = total / breaths;

			float delayMax = Mathf.Min(BuildUpStaggerMax, total * 0.45f);
			float wobble = Mathf.Max(0f, BuildUpWobblePx);
			float scaleAmp = Mathf.Max(0f, BuildUpScaleAmp);
			float rotAmp = Mathf.DegToRad(Mathf.Max(0f, BuildUpRotAmpDeg));

			foreach (var shard in _shards)
			{
				if (shard == null || !GodotObject.IsInstanceValid(shard)) continue;

				if (!_basePos.TryGetValue(shard, out var baseP))
					baseP = shard.GlobalPosition;

				if (_buildTween.TryGetValue(shard, out var oldTw) && oldTw != null && GodotObject.IsInstanceValid(oldTw))
					oldTw.Kill();

				float delay = (delayMax <= 0f) ? 0f : _rng.RandfRange(0f, delayMax);

				Vector2 baseScale = shard.Scale;
				float baseRot = shard.Rotation;

				float wobbleMul = _rng.RandfRange(0.85f, 1.15f);
				float scaleMul = _rng.RandfRange(0.85f, 1.15f);
				float rotMul = _rng.RandfRange(0.85f, 1.15f);

				var tw = shard.CreateTween();
				tw.SetTrans(Tween.TransitionType.Sine);
				tw.SetEase(Tween.EaseType.InOut);

				if (delay > 0.001f)
					tw.TweenInterval(delay);

				for (int b = 0; b < breaths; b++)
				{
					float half = Mathf.Max(0.01f, perBreath * 0.5f);

					Vector2 upPos = baseP + new Vector2(0f, -wobble * wobbleMul);
					Vector2 upScale = baseScale * (1f + scaleAmp * scaleMul);
					float upRot = baseRot + rotAmp * rotMul;

					tw.Parallel().TweenProperty(shard, "global_position", upPos, half);
					tw.Parallel().TweenProperty(shard, "scale", upScale, half);
					tw.Parallel().TweenProperty(shard, "rotation", upRot, half);

					tw.Parallel().TweenProperty(shard, "global_position", baseP, half);
					tw.Parallel().TweenProperty(shard, "scale", baseScale, half);
					tw.Parallel().TweenProperty(shard, "rotation", baseRot, half);
				}

				_buildTween[shard] = tw;
			}
		}

		private void BuildFireOrder(Vector2 arcCenter)
		{
			_fireList.Clear();
			if (_shards.Count == 0) return;

			if (!FireOrderCircular)
			{
				_fireList.AddRange(_shards);
				return;
			}

			var temp = new List<(IceShardProjectile shard, float ang)>(_shards.Count);

			foreach (var s in _shards)
			{
				if (s == null || !GodotObject.IsInstanceValid(s)) continue;
				float a = (s.GlobalPosition - arcCenter).Angle();
				temp.Add((s, a));
			}

			temp.Sort((a, b) => a.ang.CompareTo(b.ang));

			if (FireOrderAlternateEnds)
			{
				int lo = 0;
				int hi = temp.Count - 1;
				while (lo <= hi)
				{
					_fireList.Add(temp[lo].shard);
					if (lo != hi) _fireList.Add(temp[hi].shard);
					lo++;
					hi--;
				}
			}
			else
			{
				for (int i = 0; i < temp.Count; i++)
					_fireList.Add(temp[i].shard);
			}

			int w = Mathf.Max(0, FireOrderJitterWindow);
			if (w > 0 && _fireList.Count > 2)
			{
				for (int i = 0; i < _fireList.Count; i++)
				{
					int j = Mathf.Clamp(i + _rng.RandiRange(-w, w), 0, _fireList.Count - 1);
					(_fireList[i], _fireList[j]) = (_fireList[j], _fireList[i]);
				}
			}
		}

		private void FireOne(int i)
		{
			if (i < 0 || i >= _fireList.Count) return;

			var shard = _fireList[i];
			if (shard == null || !GodotObject.IsInstanceValid(shard)) return;

			if (_buildTween.TryGetValue(shard, out var bt) && bt != null && GodotObject.IsInstanceValid(bt))
				bt.Kill();

			Vector2 targetPos = _target + RandomInCircle(TargetRadius);
			float rotJit = Mathf.DegToRad(_rng.RandfRange(-RotationJitterDeg, RotationJitterDeg));

			PlayShootSfxThrottled();

			if (DebugLogs)
				GD.Print($"[IceBarrage] FIRE {i + 1}/{_fireList.Count} pos={shard.GlobalPosition} -> {targetPos}");

			shard.Fire(targetPos, Mathf.Max(0.01f, ShardTravelSec), rotJit);
		}

		private void SpawnDamageInstanceAt(Vector2 hitPos, int shardIndex)
		{
			if (DamageInstanceScene == null) return;

			var raw = DamageInstanceScene.Instantiate();
			if (raw is not Node node)
			{
				raw.QueueFree();
				return;
			}

			AddChild(node);

			Vector2 p = hitPos + DamageOffset + RandomInCircle(DamageJitterPx);
			TrySetGlobalPosition(node, p);

			// tenta passar info se o node suportar
			// (deixa seu popup escolher valor/texto internamente)
			if (node.HasMethod("SetShardIndex"))
				node.Call("SetShardIndex", shardIndex);

			if (node.HasMethod("Play"))
				node.Call("Play");
			else if (node.HasMethod("PlaySimple"))
				node.Call("PlaySimple");
			else
				TryAutoPlayInHierarchy(node);

			float life = Mathf.Max(0.05f, DamageLifetimeSec);
			GetTree().CreateTimer(life).Timeout += () =>
			{
				if (GodotObject.IsInstanceValid(node))
					node.QueueFree();
			};
		}

		private void NotifyDamageReceiver(int shardIndex, Vector2 hitPos)
		{
			if (_damageReceiver == null || !GodotObject.IsInstanceValid(_damageReceiver)) return;
			if (string.IsNullOrWhiteSpace(DamageReceiverMethod)) return;

			if (_damageReceiver.HasMethod(DamageReceiverMethod))
				_damageReceiver.Call(DamageReceiverMethod, shardIndex, hitPos);
		}

		private void SpawnWispAt(Vector2 globalPos, float scale, float lifeSec)
		{
			if (SpawnWispScene == null) return;

			var raw = SpawnWispScene.Instantiate();
			if (raw is not Node node)
			{
				raw.QueueFree();
				return;
			}

			AddChild(node);
			_wisps.Add(node);

			TrySetGlobalPosition(node, globalPos);

			float s = WispScale * Mathf.Max(0.35f, scale);
			if (node is Node2D n2)
			{
				n2.Scale *= s;
				n2.Modulate = WispTint;
			}
			else if (node is Control c)
			{
				c.Scale *= s;
				c.Modulate = WispTint;
			}

			TryAutoPlayInHierarchy(node);

			float life = Mathf.Max(0.05f, lifeSec);
			GetTree().CreateTimer(life).Timeout += () =>
			{
				if (GodotObject.IsInstanceValid(node))
					node.QueueFree();
			};
		}

		private void SpawnImpactBurstAt(Vector2 atGlobal)
		{
			if (ImpactBurstScene == null) return;

			var raw = ImpactBurstScene.Instantiate();
			if (raw is not Node node)
			{
				raw.QueueFree();
				return;
			}

			AddChild(node);
			TrySetGlobalPosition(node, atGlobal);
			TryAutoPlayInHierarchy(node);
		}

		private bool SfxCanPlay()
		{
			ulong now = Time.GetTicksMsec();
			ulong minMs = (ulong)Mathf.RoundToInt(Mathf.Max(0f, SfxMinInterval) * 1000f);
			return (now - _lastSfxMs) >= minMs;
		}

		private void PlayShootSfxThrottled()
		{
			if (ShootSfx == null) return;
			if (!SfxCanPlay()) return;

			_lastSfxMs = Time.GetTicksMsec();
			_sfx.Stream = ShootSfx;
			_sfx.PitchScale = _rng.RandfRange(0.92f, 1.08f);
			_sfx.Play();
		}

		private void PlayImpactSfxThrottled()
		{
			if (ImpactSfx == null) return;
			if (!SfxCanPlay()) return;

			_lastSfxMs = Time.GetTicksMsec();
			_sfx.Stream = ImpactSfx;
			_sfx.PitchScale = _rng.RandfRange(0.92f, 1.08f);
			_sfx.Play();
		}

		private void ScheduleCleanupCheck()
		{
			var t = GetTree().CreateTimer(0.05f);
			t.Timeout += () =>
			{
				if (!GodotObject.IsInstanceValid(this)) return;

				for (int i = _wisps.Count - 1; i >= 0; i--)
					if (_wisps[i] == null || !GodotObject.IsInstanceValid(_wisps[i]))
						_wisps.RemoveAt(i);

				if (!HasAnyShardChild() && _wisps.Count == 0)
					QueueFree();
				else
					ScheduleCleanupCheck();
			};
		}

		private bool HasAnyShardChild()
		{
			foreach (var c in GetChildren())
				if (c is IceShardProjectile) return true;
			return false;
		}

		private void StopSequence()
		{
			_running = false;
			_fireCursor = 0;
		}

		private void ClearOld()
		{
			foreach (var s in _shards)
				if (s != null && GodotObject.IsInstanceValid(s))
					s.QueueFree();

			for (int i = 0; i < _wisps.Count; i++)
				if (_wisps[i] != null && GodotObject.IsInstanceValid(_wisps[i]))
					_wisps[i].QueueFree();

			_shards.Clear();
			_fireList.Clear();
			_basePos.Clear();
			_wisps.Clear();
			_buildTween.Clear();
		}

		private Vector2 RandomInCircle(float r)
		{
			if (r <= 0f) return Vector2.Zero;

			float a = _rng.RandfRange(0f, Mathf.Tau);
			float m = Mathf.Sqrt(_rng.Randf()) * r;
			return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * m;
		}

		private static float EaseInOut(float x) => x * x * (3f - 2f * x);

		private static bool TrySetGlobalPosition(Node node, Vector2 globalPos)
		{
			if (node is Node2D n2) { n2.GlobalPosition = globalPos; return true; }
			if (node is Control c) { c.GlobalPosition = globalPos; return true; }

			foreach (var childObj in node.GetChildren())
				if (childObj is Node child && TrySetGlobalPosition(child, globalPos))
					return true;

			return false;
		}

		private static bool TryAutoPlayInHierarchy(Node node)
		{
			if (node == null) return false;

			if (node is AnimatedSprite2D asp)
			{
				if (!asp.IsPlaying()) asp.Play();
				return true;
			}

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

			if (node is AnimationPlayer ap)
			{
				var list = ap.GetAnimationList();
				if (list != null && list.Length > 0)
				{
					ap.Play(list[0]);
					return true;
				}
			}

			foreach (var childObj in node.GetChildren())
				if (childObj is Node child && TryAutoPlayInHierarchy(child))
					return true;

			return false;
		}
	}
}
