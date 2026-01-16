//using Godot;
//using System;
//using System.Threading.Tasks;
//
//public partial class StunFeedbackController : Node
//{
	//[ExportCategory("Find")]
	//[Export] public string MageGroup = "mage";
	//[Export] public string CameraGroup = "main_camera";
	//[Export] public string VfxRootGroup = "vfx_root";
//
	//[ExportCategory("Overlay")]
	//[Export] public NodePath OverlayPath; // aponta pro StunOverlay (Control) no HUD
//
	//[ExportCategory("Camera Shake")]
	//[Export] public float ShakeStrength = 12f;
	//[Export] public float ShakeDuration = 0.28f;
	//[Export] public float ShakeDecay = 60f;
//
	//[ExportCategory("Hit Stop")]
	//[Export] public bool EnableHitStop = true;
	//[Export] public float HitStopDuration = 0.08f;
	//[Export] public float HitStopTimeScale = 0.06f;
//
	//[ExportCategory("Ring VFX")]
	//[Export] public PackedScene StunRingScene;
	//[Export] public Vector2 RingOffset = new Vector2(0, 24);
	//[Export] public Vector2 RingScale = new Vector2(1.0f, 1.0f);
	//[Export] public int RingZIndex = 999;
//
	//[ExportCategory("Audio")]
	//[Export] public NodePath StunSfxPath; // AudioStreamPlayer
	//[Export] public string LowPassBusName = "Master";
	//[Export] public float LowPassCutoffOnStun = 1200f;
	//[Export] public float LowPassCutoffNormal = 22000f;
	//[Export] public float LowPassFadeSeconds = 0.25f;
//
	//private Mage _mage;
	//private CameraShake2D _camShake;
	//private StunOverlay _overlay;
	//private AudioStreamPlayer _stunSfx;
//
	//private int _busIndex = -1;
	//private int _lowPassEffectIndex = -1;
	//private AudioEffectLowPassFilter _lowPass;
//
	//private int _hitStopToken = 0;
//
	//public override void _Ready()
	//{
		//_mage = GetTree().GetFirstNodeInGroup(MageGroup) as Mage;
		//if (_mage == null)
		//{
			//GD.PushWarning("[StunFeedbackController] Mage não encontrado (group).");
			//return;
		//}
//
		//_overlay = !OverlayPath.IsEmpty ? GetNodeOrNull<StunOverlay>(OverlayPath) : null;
		//_stunSfx = !StunSfxPath.IsEmpty ? GetNodeOrNull<AudioStreamPlayer>(StunSfxPath) : null;
//
		//_camShake = GetTree().GetFirstNodeInGroup(CameraGroup) as CameraShake2D;
		//SetupLowPass();
//
		//_mage.StunChanged += OnStunChanged;
	//}
//
	//public override void _ExitTree()
	//{
		//if (_mage != null)
			//_mage.StunChanged -= OnStunChanged;
	//}
//
	//private void OnStunChanged(bool isStunned)
	//{
		//if (isStunned)
			//_ = OnStunStart();
		//else
			//_ = OnStunEnd();
	//}
//
	//private async Task OnStunStart()
	//{
		//// 1) overlay
		//_overlay?.PulseStart();
//
		//// 2) shake
		//_camShake?.Shake(ShakeStrength, ShakeDuration, ShakeDecay);
//
		//// 3) ring vfx no chão
		//SpawnRing();
//
		//// 4) stinger
		//_stunSfx?.Play();
//
		//// 5) low-pass
		//await FadeLowPass(toCutoff: LowPassCutoffOnStun, seconds: 0.08f);
//
		//// 6) hit stop (micro)
		//if (EnableHitStop)
			//await DoHitStop(HitStopDuration, HitStopTimeScale);
	//}
//
	//private async Task OnStunEnd()
	//{
		//_overlay?.PulseEnd();
		//await FadeLowPass(toCutoff: LowPassCutoffNormal, seconds: LowPassFadeSeconds);
	//}
//
	//private void SpawnRing()
	//{
		//if (StunRingScene == null) return;
		//if (_mage == null || !GodotObject.IsInstanceValid(_mage)) return;
//
		//var parent = GetTree().GetFirstNodeInGroup(VfxRootGroup) as Node;
		//parent ??= GetTree().CurrentScene;
//
		//var ring = StunRingScene.Instantiate<Node2D>();
		//parent.AddChild(ring);
//
		//// spawn no "pé" da mage se existir Marker2D VfxGround, senão usa posição + offset
		//var ground = _mage.GetNodeOrNull<Marker2D>("VfxGround");
		//var pos = ground != null ? ground.GlobalPosition : _mage.GlobalPosition;
//
		//ring.GlobalPosition = pos + RingOffset;
		//ring.Scale = RingScale;
		//ring.ZIndex = RingZIndex;
	//}
//
	//private async System.Threading.Tasks.Task DoHitStop(float duration, float timeScale)
//{
	//int token = ++_hitStopToken;
//
	//// Engine.TimeScale é double no Godot 4
	//double old = Engine.TimeScale;
//
	//Engine.TimeScale = Mathf.Clamp(timeScale, 0.01f, 1f);
//
	//// ignoreTimeScale = true pra duração real não “esticAR”
	//await ToSignal(GetTree().CreateTimer(duration, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
//
	//if (token == _hitStopToken)
		//Engine.TimeScale = old;
//}
//
	//private void SetupLowPass()
	//{
		//_busIndex = AudioServer.GetBusIndex(LowPassBusName);
		//if (_busIndex < 0)
		//{
			//GD.PushWarning($"[StunFeedbackController] Bus '{LowPassBusName}' não existe. Sem low-pass.");
			//return;
		//}
//
		//// acha efeito low-pass ou cria
		//int count = AudioServer.GetBusEffectCount(_busIndex);
		//for (int i = 0; i < count; i++)
		//{
			//var e = AudioServer.GetBusEffect(_busIndex, i);
			//if (e is AudioEffectLowPassFilter lp)
			//{
				//_lowPassEffectIndex = i;
				//_lowPass = lp;
				//break;
			//}
		//}
//
		//if (_lowPass == null)
		//{
			//_lowPass = new AudioEffectLowPassFilter();
			//AudioServer.AddBusEffect(_busIndex, _lowPass, 0);
			//_lowPassEffectIndex = 0;
		//}
//
		//_lowPass.CutoffHz = LowPassCutoffNormal;
	//}
//
	//private async Task FadeLowPass(float toCutoff, float seconds)
	//{
		//if (_lowPass == null) return;
//
		//float from = _lowPass.CutoffHz;
		//float t = 0f;
		//float dur = Mathf.Max(0.01f, seconds);
//
		//while (t < dur)
		//{
			//await ToSignal(GetTree().CreateTimer(0.016, ignoreTimeScale: true), SceneTreeTimer.SignalName.Timeout);
			//t += 0.016f;
			//float a = Mathf.Clamp(t / dur, 0f, 1f);
			//_lowPass.CutoffHz = Mathf.Lerp(from, toCutoff, a);
		//}
//
		//_lowPass.CutoffHz = toCutoff;
	//}
//}
