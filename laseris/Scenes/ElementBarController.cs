using Godot;
using System;
using System.Collections.Generic;
using Game.Combat;

namespace Game.UI
{
	public partial class ElementBarController : Control
	{
		public enum HintDifficulty { Normal = 0, Hard = 1 }

		[ExportGroup("Difficulty")]
		[Export] public HintDifficulty DifficultyMode = HintDifficulty.Normal;

		[ExportGroup("Elements")]
		[Export] public NodePath[] ElementNodesPaths = Array.Empty<NodePath>();

		[ExportGroup("Animations")]
		[Export] public string IdleAnim = "idle";
		[Export] public string ActivateAnim = "activate";
		[Export] public float ActivateHoldSeconds = 0.08f;

		[ExportGroup("Hints")]
		[Export] public PackedScene HintProjectileScene;   // ElementHintProjectile.tscn (root=Control + script)
		[Export] public NodePath HintParentPath;           // Control dentro do CanvasLayer (recomendado)
		[Export] public bool EnableHints = true;
		[Export] public bool HideHintsOnDefense = true;

		[ExportGroup("Hint Timing")]
		[Export] public float DefaultLeadSeconds = 0.50f;

		[ExportGroup("Hard Mode Decoys")]
		[Export] public float HardDecoyFadeOut = 0.08f;     // os 5 decoys somem rápido sem shake

		[ExportGroup("VFX")]
		[Export] public PackedScene RuneHitVfxScene;
		[Export] public NodePath VfxParentPath = "";
		[Export] public bool SpawnVfxOnGood = true;
		[Export] public bool SpawnVfxOnPerfect = true;

		private bool _hintsEnabledThisTurn = true;
		private int _modeSideId = -1;

		public int SelectedElementId { get; private set; } = 1;

		private AnimatedSprite2D[] _elems = Array.Empty<AnimatedSprite2D>();
		private Vector2[] _baseScale = Array.Empty<Vector2>();
		private Tween[] _scaleTween = Array.Empty<Tween>();

		private int[] _selectToken = Array.Empty<int>();
		private int[] _beatToken = Array.Empty<int>();
		private int[] _resolveToken = Array.Empty<int>();

		private Node _hintParent;

		// ===== Active hints management =====
		private class ActiveHint
		{
			public ElementHintProjectile Proj;
			public int RuneIdx;              // 0..N-1 (coluna)
			public int CounterElementId;     // elemento do counter desta nota
			public double BeatSec;
		}

		private class CueBatch
		{
			public int CounterElementId;
			public double BeatSec;
			public readonly List<ActiveHint> Hints = new();
		}

		private readonly List<ActiveHint> _activeHints = new();
		private readonly Queue<CueBatch> _pendingCueBatches = new();

		private ulong _lastResolveFrame = ulong.MaxValue;

		public override void _Ready()
		{
			LoadElements();
			CacheBaseScales();
			ResolveHintParent();
			ClearAll();
		}

		private void ResolveHintParent()
		{
			_hintParent = this;

			if (HintParentPath == null || HintParentPath.IsEmpty)
			{
				GD.PushWarning("[ElementBar] HintParentPath vazio. Vou usar o próprio ElementBar como parent.");
				return;
			}

			var p = GetNodeOrNull<Node>(HintParentPath);
			if (p == null)
			{
				GD.PushWarning($"[ElementBar] Não achei parent em HintParentPath: {HintParentPath}. Vou usar ElementBar.");
				return;
			}

			_hintParent = p;
		}

		// ============================
		// Public API
		// ============================

		public void SetDifficulty(HintDifficulty difficulty) => DifficultyMode = difficulty;

		public void SetMode(int sideId)
		{
			_modeSideId = sideId;
			bool isPlayerTurn = sideId == 1;
			_hintsEnabledThisTurn = isPlayerTurn;

			if (HideHintsOnDefense && !isPlayerTurn)
				StopAllHints();
		}

		public void ClearAll()
		{
			for (int i = 0; i < _elems.Length; i++)
			{
				var s = _elems[i];
				if (s == null) continue;

				_selectToken[i]++;
				_beatToken[i]++;
				_resolveToken[i]++;

				KillScaleTween(i);
				s.Scale = _baseScale[i];
				PlayIdle(i);
			}

			StopAllHints();
		}

		public void SetSongTime(double nowSec)
		{
			for (int i = _activeHints.Count - 1; i >= 0; i--)
			{
				var h = _activeHints[i];
				if (h?.Proj == null || !GodotObject.IsInstanceValid(h.Proj))
				{
					_activeHints.RemoveAt(i);
					continue;
				}

				h.Proj.UpdateNow(nowSec);
			}
		}

		public void SetSelectedElement(int elementId)
		{
			if (!TryGetIdx(elementId, out int idx)) return;
			SelectedElementId = elementId;

			PlayActivateShort(idx);
			MicroPunch(idx, 1.04f, 0.03f, 0.06f);
		}

		public void CueElement(int counterElementId, float leadSeconds, double beatSec, double nowSec)
		{
			if (!EnableHints) return;
			if (!_hintsEnabledThisTurn) return;

			if (HintProjectileScene == null)
			{
				GD.PushWarning("[ElementBar] HintProjectileScene NULL.");
				return;
			}

			float usedLead = leadSeconds > 0 ? leadSeconds : DefaultLeadSeconds;

			var batch = new CueBatch
			{
				CounterElementId = counterElementId,
				BeatSec = beatSec
			};

			if (DifficultyMode == HintDifficulty.Normal)
			{
				// NORMAL: nasce só na coluna do counter, com cor do counter
				if (!TryGetIdx(counterElementId, out int idx)) return;

				Color counterColor = GetElementColor(counterElementId);
				var h = SpawnHintOnRune(idx, counterElementId, beatSec, nowSec, usedLead, counterColor);
				if (h != null) batch.Hints.Add(h);
			}
			else
			{
				// HARD: nasce nas 6 colunas, cada uma com sua cor respectiva
				for (int i = 0; i < _elems.Length; i++)
				{
					if (_elems[i] == null) continue;

					Color laneColor = GetElementColor(i + 1); // <<< cor da coluna
					var h = SpawnHintOnRune(i, counterElementId, beatSec, nowSec, usedLead, laneColor);
					if (h != null) batch.Hints.Add(h);
				}
			}

			if (batch.Hints.Count > 0)
				_pendingCueBatches.Enqueue(batch);
		}

		public void BeatPop(int elementId)
		{
			if (!TryGetIdx(elementId, out int idx)) return;

			_beatToken[idx]++;
			int my = _beatToken[idx];

			BeatPopNow(idx, my);
		}

		public void Resolve(int pressedElementId, int gradeId)
		{
			ulong frame = Engine.GetProcessFrames();
			if (_lastResolveFrame == frame)
				return;
			_lastResolveFrame = frame;

			// feedback na barra do botão apertado
			if (TryGetIdx(pressedElementId, out int idx))
			{
				_resolveToken[idx]++;
				int my = _resolveToken[idx];

				bool success = (JudgementGrade)gradeId != JudgementGrade.Miss;
				ResolvePunch(idx, my, success);

				if (success)
				{
					var grade = (JudgementGrade)gradeId;
					bool spawn =
						(grade == JudgementGrade.Perfect && SpawnVfxOnPerfect) ||
						(grade == JudgementGrade.Good && SpawnVfxOnGood);

					if (spawn) SpawnRuneVfxAt(idx);
				}
			}

			// aplica no batch FIFO (1 só)
			if (_pendingCueBatches.Count == 0)
				return;

			var batch = _pendingCueBatches.Dequeue();
			var gradeFinal = (JudgementGrade)gradeId;

			ApplyHintFeedback(batch, gradeFinal);
			RemoveBatchFromActive(batch);
		}

		private void ApplyHintFeedback(CueBatch batch, JudgementGrade grade)
		{
			if (batch == null) return;

			// NORMAL: batch tem 1 hint só
			if (DifficultyMode == HintDifficulty.Normal)
			{
				for (int i = 0; i < batch.Hints.Count; i++)
				{
					var h = batch.Hints[i];
					if (h?.Proj == null || !GodotObject.IsInstanceValid(h.Proj)) continue;

					if (grade == JudgementGrade.Miss) h.Proj.ResolveMiss();
					else h.Proj.ResolveGoodOrPerfect(grade == JudgementGrade.Perfect);
				}
				return;
			}

			// HARD:
			// Só o hint da coluna do counter recebe hit/miss.
			int counterIdx = batch.CounterElementId - 1;

			for (int i = 0; i < batch.Hints.Count; i++)
			{
				var h = batch.Hints[i];
				if (h?.Proj == null || !GodotObject.IsInstanceValid(h.Proj)) continue;

				if (h.RuneIdx == counterIdx)
				{
					if (grade == JudgementGrade.Miss) h.Proj.ResolveMiss();
					else h.Proj.ResolveGoodOrPerfect(grade == JudgementGrade.Perfect);
				}
				else
				{
					SoftKillDecoy(h.Proj);
				}
			}
		}

		private void SoftKillDecoy(ElementHintProjectile proj)
		{
			if (proj == null || !GodotObject.IsInstanceValid(proj)) return;

			var tw = CreateTween();
			tw.TweenProperty(proj, "modulate:a", 0.0f, Mathf.Max(0.01f, HardDecoyFadeOut));
			tw.TweenCallback(Callable.From(() =>
			{
				if (proj != null && GodotObject.IsInstanceValid(proj))
					proj.QueueFree();
			}));
		}

		private void RemoveBatchFromActive(CueBatch batch)
		{
			if (batch == null) return;

			for (int i = _activeHints.Count - 1; i >= 0; i--)
			{
				var a = _activeHints[i];
				if (a?.Proj == null) { _activeHints.RemoveAt(i); continue; }

				for (int j = 0; j < batch.Hints.Count; j++)
				{
					var b = batch.Hints[j];
					if (b?.Proj == null) continue;

					if (a.Proj == b.Proj)
					{
						_activeHints.RemoveAt(i);
						break;
					}
				}
			}
		}

		private ActiveHint SpawnHintOnRune(
			int runeIdx,
			int counterElementId,
			double beatSec,
			double nowSec,
			float leadSeconds,
			Color spawnColor) // <<< cor que realmente vai no projétil
		{
			var rune = _elems[runeIdx];
			if (rune == null) return null;

			var inst = HintProjectileScene.Instantiate();
			if (inst is not ElementHintProjectile proj)
			{
				GD.PushWarning("[ElementBar] HintProjectileScene root não é ElementHintProjectile.");
				inst.QueueFree();
				return null;
			}

			_hintParent.AddChild(proj);

			Vector2 runeCenter = rune.GetGlobalTransformWithCanvas().Origin;

			proj.LeadSeconds = leadSeconds;
			proj.Arm(runeCenter, beatSec, nowSec, spawnColor); // <<< usa a cor passada

			var h = new ActiveHint
			{
				Proj = proj,
				RuneIdx = runeIdx,
				CounterElementId = counterElementId,
				BeatSec = beatSec
			};

			_activeHints.Add(h);
			return h;
		}

		private Color GetElementColor(int elementId)
		{
			return elementId switch
			{
				1 => new Color(1.00f, 0.40f, 0.15f),
				2 => new Color(0.35f, 0.65f, 1.00f),
				3 => new Color(0.35f, 1.00f, 0.45f),
				4 => new Color(0.80f, 0.85f, 0.90f),
				5 => new Color(0.95f, 0.90f, 0.35f),
				6 => new Color(0.95f, 0.85f, 0.25f),
				7 => new Color(0.55f, 0.35f, 0.90f),
				_ => Colors.White
			};
		}

		private void StopAllHints()
		{
			for (int i = _activeHints.Count - 1; i >= 0; i--)
			{
				var h = _activeHints[i];
				if (h?.Proj != null && GodotObject.IsInstanceValid(h.Proj))
					h.Proj.QueueFree();
			}

			_activeHints.Clear();
			_pendingCueBatches.Clear();
		}

		// ============================
		// Existing internals (igual ao seu)
		// ============================

		private void LoadElements()
		{
			if (ElementNodesPaths != null && ElementNodesPaths.Length > 0)
			{
				_elems = new AnimatedSprite2D[ElementNodesPaths.Length];
				for (int i = 0; i < ElementNodesPaths.Length; i++)
					_elems[i] = GetNodeOrNull<AnimatedSprite2D>(ElementNodesPaths[i]);
			}
			else
			{
				_elems = new AnimatedSprite2D[6];
				for (int i = 0; i < 6; i++)
					_elems[i] = GetNodeOrNull<AnimatedSprite2D>($"Elem{i + 1}");
			}

			int n = _elems.Length;
			_baseScale = new Vector2[n];
			_scaleTween = new Tween[n];

			_selectToken = new int[n];
			_beatToken = new int[n];
			_resolveToken = new int[n];

			for (int i = 0; i < _elems.Length; i++)
				if (_elems[i] == null)
					GD.PushWarning($"ElementBarController: Elem {i + 1} está null (confira paths/nome).");
		}

		private void CacheBaseScales()
		{
			for (int i = 0; i < _elems.Length; i++)
			{
				var s = _elems[i];
				_baseScale[i] = (s != null) ? s.Scale : Vector2.One;
			}
		}

		private bool TryGetIdx(int elementId, out int idx)
		{
			idx = elementId - 1;
			if (_elems.Length == 0) return false;
			if (idx < 0 || idx >= _elems.Length) return false;
			if (_elems[idx] == null) return false;
			return true;
		}

		private void KillScaleTween(int idx)
		{
			if (_scaleTween[idx] != null && GodotObject.IsInstanceValid(_scaleTween[idx]))
				_scaleTween[idx].Kill();
			_scaleTween[idx] = null;
		}

		private void PlayIdle(int idx)
		{
			var s = _elems[idx];
			if (s?.SpriteFrames == null) return;

			if (s.SpriteFrames.HasAnimation(IdleAnim))
				s.Play(IdleAnim);
			else
				s.Stop();
		}

		private void PlayActivateShort(int idx)
		{
			var s = _elems[idx];
			if (s?.SpriteFrames == null) return;

			_selectToken[idx]++;

			if (s.SpriteFrames.HasAnimation(ActivateAnim))
				s.Play(ActivateAnim);
			else
				PlayIdle(idx);

			float delay = Mathf.Max(0f, ActivateHoldSeconds);
			GetTree().CreateTimer(delay).Timeout += () => PlayIdle(idx);
		}

		private void MicroPunch(int idx, float scaleMult, float tin, float tout)
		{
			var s = _elems[idx];
			if (s == null) return;

			KillScaleTween(idx);

			var tw = CreateTween();
			_scaleTween[idx] = tw;

			Vector2 b = _baseScale[idx];
			tw.TweenProperty(s, "scale", b * scaleMult, tin);
			tw.TweenProperty(s, "scale", b, tout);
		}

		private void BeatPopNow(int idx, int token)
		{
			var s = _elems[idx];
			if (s == null) return;

			KillScaleTween(idx);

			var tw = CreateTween();
			_scaleTween[idx] = tw;

			Vector2 b = _baseScale[idx];
			tw.TweenProperty(s, "scale", b * 1.10f, 0.05f);
			tw.TweenProperty(s, "scale", b, 0.10f);
		}

		private void ResolvePunch(int idx, int token, bool success)
		{
			var s = _elems[idx];
			if (s == null) return;

			KillScaleTween(idx);

			float mult = success ? 1.12f : 1.06f;

			var tw = CreateTween();
			_scaleTween[idx] = tw;

			Vector2 b = _baseScale[idx];
			tw.TweenProperty(s, "scale", b * mult, 0.06f);
			tw.TweenProperty(s, "scale", b, 0.10f);

			if (s.SpriteFrames != null && s.SpriteFrames.HasAnimation(ActivateAnim))
				s.Play(ActivateAnim);

			float delay = Mathf.Max(0.01f, 0.06f + 0.10f);
			GetTree().CreateTimer(delay).Timeout += () => PlayIdle(idx);
		}

		private void SpawnRuneVfxAt(int idx)
		{
			if (RuneHitVfxScene == null) return;

			var inst = RuneHitVfxScene.Instantiate();
			if (inst is not Control vfx) { inst.QueueFree(); return; }

			Node parent = this;
			if (VfxParentPath != null && !VfxParentPath.IsEmpty)
			{
				var p = GetNodeOrNull<Node>(VfxParentPath);
				if (p != null) parent = p;
			}

			parent.AddChild(vfx);

			var rune = _elems[idx];
			Vector2 center = rune.GetGlobalTransformWithCanvas().Origin;
			vfx.Position = center;

			if (inst is RuneHitVfx r) r.Play();
			else if (inst.HasMethod("Play")) inst.Call("Play");
		}
	}
}
