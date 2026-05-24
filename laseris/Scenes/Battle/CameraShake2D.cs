using Godot;

namespace Game.Vfx
{
	public partial class CameraShake2D : Camera2D
	{
		[ExportGroup("Defaults")]
		[Export] public float DefaultAmplitudePx = 6f;
		[Export] public float DefaultDurationSec = 0.10f;
		[Export] public float Smooth = 28f; // maior = mais “firme” (menos tremedeira solta)

		private readonly RandomNumberGenerator _rng = new();

		private Vector2 _baseOffset;
		private Vector2 _currentOffset;

		private float _timeLeft;
		private float _duration;
		private float _amplitude;

		public override void _Ready()
		{
			_rng.Randomize();
			_baseOffset = Offset;
			_currentOffset = Offset;
			SetProcess(false);
		}

		/// <summary>
		/// Shake curto e leve. amplitude em pixels, duration em segundos.
		/// </summary>
		public void Shake(float amplitudePx = -1f, float durationSec = -1f)
		{
			float amp = (amplitudePx > 0f) ? amplitudePx : DefaultAmplitudePx;
			float dur = (durationSec > 0f) ? durationSec : DefaultDurationSec;

			// se já está tremendo, mantém o maior
			_amplitude = Mathf.Max(_amplitude, amp);
			_duration = Mathf.Max(_duration, dur);
			_timeLeft = Mathf.Max(_timeLeft, dur);

			SetProcess(true);
		}

		public override void _Process(double delta)
		{
			float dt = (float)delta;

			_timeLeft -= dt;
			if (_timeLeft <= 0f)
			{
				_timeLeft = 0f;
				_amplitude = 0f;

				// volta suave pro offset base
				_currentOffset = _currentOffset.Lerp(_baseOffset, 1f - Mathf.Exp(-Smooth * dt));
				Offset = _currentOffset;

				if (_currentOffset.DistanceTo(_baseOffset) < 0.05f)
				{
					Offset = _baseOffset;
					SetProcess(false);
				}
				return;
			}

			float t01 = (_duration <= 0.0001f) ? 0f : (_timeLeft / _duration);
			// decaimento “snappy”
			float strength = t01 * t01;

			Vector2 rnd = new Vector2(_rng.RandfRange(-1f, 1f), _rng.RandfRange(-1f, 1f)).Normalized();
			Vector2 target = _baseOffset + rnd * (_amplitude * strength);

			_currentOffset = _currentOffset.Lerp(target, 1f - Mathf.Exp(-Smooth * dt));
			Offset = _currentOffset;
		}
	}
}
