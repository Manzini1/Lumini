using Godot;
using System.Collections.Generic;

public partial class DualScoreBarsController : Control
{
	[Export] public NodePath PlayerLinePath = "ScoreLine";
	[Export] public NodePath EnemyLinePath  = "EnemyLine";

	[ExportGroup("Orbs")]
	[Export] public PackedScene GainOrbScene;
	[Export] public NodePath OrbLayerPath = "OrbLayer";

	// ✅ Agora estes nodes são AREAS (Control) onde pode nascer
	[Export] public NodePath PlayerGainSpawnPath = "PlayerGainSpawn";
	[Export] public NodePath EnemyGainSpawnPath  = "EnemyGainSpawn";

	[Export] public float SpawnPadding = 8f;           // padding dentro do retângulo
	[Export] public float SpawnMinSeparation = 26f;    // tenta não nascer muito perto
	[Export] public int SpawnTries = 8;                // tentativas pra achar lugar livre
	[Export] public int RememberLastSpawns = 10;       // memória de posições recentes

	public enum FillMode { FixedMax, RollingMax }

	[ExportGroup("Fill Mode")]
	[Export] public FillMode Mode = FillMode.FixedMax;

	// FixedMax
	[Export] public long FixedMaxScore = 2000;

	// RollingMax
	[Export] public float RollingHeadroom = 1.15f;
	[Export] public long RollingMinMax = 200;

	[ExportGroup("Anim (Bars)")]
	[Export] public float FillAnimMin = 0.28f;
	[Export] public float FillAnimMax = 0.60f;
	[Export] public float AmountForMaxAnim = 260f;

	private ScoreLineController _player;
	private ScoreLineController _enemy;

	private Control _orbLayer;
	private Control _playerSpawn;
	private Control _enemySpawn;

	private long _playerScore;
	private long _enemyScore;

	private readonly RandomNumberGenerator _rng = new();
	private readonly List<Vector2> _recentSpawns = new();

	public override void _Ready()
	{
		_rng.Randomize();

		_player = GetNodeOrNull<ScoreLineController>(PlayerLinePath);
		_enemy  = GetNodeOrNull<ScoreLineController>(EnemyLinePath);

		_orbLayer    = GetNodeOrNull<Control>(OrbLayerPath) ?? this;
		_playerSpawn = GetNodeOrNull<Control>(PlayerGainSpawnPath);
		_enemySpawn  = GetNodeOrNull<Control>(EnemyGainSpawnPath);

		CallDeferred(nameof(ApplyImmediate));
	}

	public void SetImmediate(long player, long enemy)
	{
		_playerScore = player < 0 ? 0 : player;
		_enemyScore  = enemy  < 0 ? 0 : enemy;
		CallDeferred(nameof(ApplyImmediate));
	}

	public void AddPlayerDamage(int amount)
	{
		if (amount <= 0) return;
		SpawnGainOrb(isPlayer: true, amount);
	}

	public void AddEnemyDamage(int amount)
	{
		if (amount <= 0) return;
		SpawnGainOrb(isPlayer: false, amount);
	}

	private void SpawnGainOrb(bool isPlayer, int amount)
	{
		// escolhe origem random dentro da área
		Vector2 from = PickSpawnPoint(isPlayer ? _playerSpawn : _enemySpawn);

		// destino = tip da barra
		var line = isPlayer ? _player : _enemy;
		Vector2 target = (line != null) ? line.GetTipGlobalCenter() : from;

		// sem cena? aplica direto
		if (GainOrbScene == null)
		{
			ApplyGainNow(isPlayer, amount);
			return;
		}

		var inst = GainOrbScene.Instantiate();
		if (inst is not ScoreGainOrbController orb)
		{
			inst.QueueFree();
			ApplyGainNow(isPlayer, amount);
			return;
		}

		_orbLayer.AddChild(orb);

		// ✅ visual do orb: você pode escolher se “leading” é do placar ou do lado
		bool leadingFx = isPlayer ? (_playerScore >= _enemyScore) : (_enemyScore >= _playerScore);

		orb.Play(amount, from, target, leadingFx, () =>
		{
			ApplyGainNow(isPlayer, amount);
			line?.AbsorbPulse(strength: Mathf.Clamp(amount / 90f, 0.25f, 1.25f));
		});
	}

	private Vector2 PickSpawnPoint(Control area)
	{
		// fallback: centro da HUD
		Rect2 rect = (area != null && GodotObject.IsInstanceValid(area))
			? area.GetGlobalRect()
			: GetGlobalRect();

		// padding
		rect.Position += new Vector2(SpawnPadding, SpawnPadding);
		rect.Size -= new Vector2(SpawnPadding * 2f, SpawnPadding * 2f);

		if (rect.Size.X < 2 || rect.Size.Y < 2)
		{
			var me = GetGlobalRect();
			return me.Position + me.Size * 0.5f;
		}

		Vector2 best = rect.Position + rect.Size * 0.5f;
		float bestScore = -1f;

		for (int i = 0; i < Mathf.Max(1, SpawnTries); i++)
		{
			Vector2 p = rect.Position + new Vector2(_rng.Randf() * rect.Size.X, _rng.Randf() * rect.Size.Y);

			// mede distância mínima para posições recentes (espalhar)
			float minDist = 999999f;
			for (int k = 0; k < _recentSpawns.Count; k++)
				minDist = Mathf.Min(minDist, p.DistanceTo(_recentSpawns[k]));

			// tenta respeitar separação
			if (minDist >= SpawnMinSeparation)
			{
				RememberSpawn(p);
				return p;
			}

			// guarda o melhor (mais longe)
			if (minDist > bestScore)
			{
				bestScore = minDist;
				best = p;
			}
		}

		RememberSpawn(best);
		return best;
	}

	private void RememberSpawn(Vector2 p)
	{
		_recentSpawns.Add(p);
		while (_recentSpawns.Count > Mathf.Max(1, RememberLastSpawns))
			_recentSpawns.RemoveAt(0);
	}

	private void ApplyGainNow(bool isPlayer, int amount)
	{
		if (isPlayer) _playerScore += amount;
		else _enemyScore += amount;

		ApplyAnimated(amount);

		_player?.SetLeading(_playerScore > _enemyScore);
		_enemy?.SetLeading(_enemyScore > _playerScore);
	}

	private void ApplyImmediate()
	{
		var (pf, ef) = ComputeFill(_playerScore, _enemyScore);
		_player?.SetFillImmediate(pf);
		_enemy?.SetFillImmediate(ef);

		_player?.SetLeading(_playerScore > _enemyScore, immediate: true);
		_enemy?.SetLeading(_enemyScore > _playerScore, immediate: true);
	}

	private void ApplyAnimated(int amount)
	{
		var (pf, ef) = ComputeFill(_playerScore, _enemyScore);

		float t = Mathf.Clamp(amount / Mathf.Max(1f, AmountForMaxAnim), 0f, 1f);
		t = Mathf.Pow(t, 0.65f);
		float dur = Mathf.Lerp(FillAnimMin, FillAnimMax, t);

		_player?.AnimateFillTo(pf, dur);
		_enemy?.AnimateFillTo(ef, dur);
	}

	private (float, float) ComputeFill(long player, long enemy)
	{
		switch (Mode)
		{
			case FillMode.RollingMax:
			{
				float dyn = Mathf.Max((float)RollingMinMax, Mathf.Max(player, enemy) * RollingHeadroom);
				return (Mathf.Clamp((float)player / dyn, 0f, 1f),
						Mathf.Clamp((float)enemy  / dyn, 0f, 1f));
			}
			default:
			{
				float max = Mathf.Max(1f, (float)FixedMaxScore);
				return (Mathf.Clamp((float)player / max, 0f, 1f),
						Mathf.Clamp((float)enemy  / max, 0f, 1f));
			}
		}
	}
}
