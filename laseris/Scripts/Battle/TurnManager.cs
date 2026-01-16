using Godot;

namespace Game.Battle;

public enum TurnSide { Enemy, Player }

public partial class TurnManager : Node
{
	[Signal] public delegate void TurnStartedEventHandler(int sideId, double startSec, double endSec);
	[Signal] public delegate void TurnEndedEventHandler(int sideId, double endSec);

	public TurnSide CurrentSide { get; private set; } = TurnSide.Enemy;

	public double TurnStartSec { get; private set; }
	public double TurnEndSec { get; private set; }

	private double _enemyBase;
	private double _playerBase;

	public void Configure(double enemyBaseSeconds, double playerBaseSeconds)
	{
		_enemyBase = enemyBaseSeconds;
		_playerBase = playerBaseSeconds;
	}

	public void StartFirstTurn(double songNowSec)
	{
		StartTurn(TurnSide.Enemy, songNowSec);
	}

	public void StartTurn(TurnSide side, double songNowSec)
	{
		CurrentSide = side;
		TurnStartSec = songNowSec;

		double dur = (side == TurnSide.Enemy) ? _enemyBase : _playerBase;
		TurnEndSec = TurnStartSec + dur;

		EmitSignal(SignalName.TurnStarted, (int)side, TurnStartSec, TurnEndSec);
	}

	public void ReduceCurrentTurnEnd(double seconds)
	{
		TurnEndSec -= seconds;
		if (TurnEndSec < TurnStartSec + 0.25)
			TurnEndSec = TurnStartSec + 0.25;
	}

	public void Update(double songNowSec)
	{
		if (songNowSec >= TurnEndSec)
		{
			EmitSignal(SignalName.TurnEnded, (int)CurrentSide, TurnEndSec);

			var next = (CurrentSide == TurnSide.Enemy) ? TurnSide.Player : TurnSide.Enemy;
			StartTurn(next, songNowSec);
		}
	}
}
