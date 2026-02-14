using Godot;

public partial class DualScoreBarsController : Control
{
	[Export] public NodePath PlayerLinePath = "ScoreLine";
	[Export] public NodePath EnemyLinePath = "EnemyLine";

	public enum FillMode { ShareOfTotal, FixedMax, RollingMax }

	[ExportGroup("Fill Mode")]
	[Export] public FillMode Mode = FillMode.ShareOfTotal;

	// ShareOfTotal: cada barra = score / (player+enemy)
	[Export] public long ShareMinTotalForScale = 200;

	// FixedMax: cada barra = score / FixedMaxScore
	[Export] public long FixedMaxScore = 1000;

	// RollingMax: cada barra = score / (max(player,enemy)*headroom)
	[Export] public float RollingHeadroom = 1.15f;
	[Export] public long RollingMinMax = 200;

	[ExportGroup("Anim")]
	[Export] public float FillAnimMin = 0.08f;
	[Export] public float FillAnimMax = 0.18f;

	private ScoreLineController _player;
	private ScoreLineController _enemy;

	private long _playerScore;
	private long _enemyScore;

	public override void _Ready()
	{
		_player = GetNodeOrNull<ScoreLineController>(PlayerLinePath);
		_enemy = GetNodeOrNull<ScoreLineController>(EnemyLinePath);

		CallDeferred(nameof(ApplyImmediate));
	}

	public void SetImmediate(long player, long enemy)
	{
		_playerScore = Mathf.Max(0, (int)player);
		_enemyScore = Mathf.Max(0, (int)enemy);
		CallDeferred(nameof(ApplyImmediate));
	}

	public void AddPlayerDamage(int amount)
	{
		if (amount <= 0) return;
		_playerScore += amount;
		ApplyAnimated(amount);
	}

	public void AddEnemyDamage(int amount)
	{
		if (amount <= 0) return;
		_enemyScore += amount;
		ApplyAnimated(amount);
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

		float t = Mathf.Clamp(amount / 200f, 0f, 1f);
		float dur = Mathf.Lerp(FillAnimMin, FillAnimMax, t);

		_player?.AnimateFillTo(pf, dur);
		_enemy?.AnimateFillTo(ef, dur);

		_player?.SetLeading(_playerScore > _enemyScore);
		_enemy?.SetLeading(_enemyScore > _playerScore);
	}

	private (float, float) ComputeFill(long player, long enemy)
	{
		switch (Mode)
		{
			case FillMode.FixedMax:
			{
				float max = Mathf.Max(1f, FixedMaxScore);
				return (Mathf.Clamp((float)player / max, 0f, 1f),
						Mathf.Clamp((float)enemy / max, 0f, 1f));
			}
			case FillMode.RollingMax:
			{
				float dyn = Mathf.Max(RollingMinMax, Mathf.Max(player, enemy) * RollingHeadroom);
				return (Mathf.Clamp((float)player / dyn, 0f, 1f),
						Mathf.Clamp((float)enemy / dyn, 0f, 1f));
			}
			default: // ShareOfTotal
			{
				float total = Mathf.Max(ShareMinTotalForScale, player + enemy);
				return ((float)player / total, (float)enemy / total);
			}
		}
	}
}
