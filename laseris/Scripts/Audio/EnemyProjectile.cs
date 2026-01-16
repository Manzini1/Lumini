//using Godot;
//using System;
//
//public partial class EnemyProjectile : Node2D
//{
	//[ExportCategory("Refs")]
	//[Export] public NodePath SpritePath = "Sprite";
//
	//[ExportCategory("Move")]
	//[Export] public float TravelSeconds = 0.12f;
//
	//[ExportCategory("Blocked")]
	//[Export] public float StuckSecondsOnShield = 0.18f;
//
	//private Sprite2D _sprite;
//
	//public override void _Ready()
	//{
		//_sprite = GetNodeOrNull<Sprite2D>(SpritePath);
	//}
//
	///// <summary>
	///// Lança o projétil do inimigo.
	///// blocked=true => vai para o ShieldBlockPoint e some depois de um tempo.
	///// blocked=false => acerta o mage (ApplyDamage).
	///// </summary>
	//public async void Launch(Vector2 startWorld, Node2D mage, int damage, bool blocked)
	//{
		//GlobalPosition = startWorld;
//
		//if (mage == null || !GodotObject.IsInstanceValid(mage))
		//{
			//QueueFree();
			//return;
		//}
//
		//// Se bloqueou, mira no marker do escudo. Senão, mira no corpo (GlobalPosition do mage).
		//Vector2 end = mage.GlobalPosition;
//
		//if (blocked)
		//{
			//var blockPoint = mage.GetNodeOrNull<Marker2D>("ShieldBlockPoint");
			//if (blockPoint != null)
				//end = blockPoint.GlobalPosition;
		//}
//
		//// Move até o destino
		//var tween = CreateTween();
		//tween.TweenProperty(this, "global_position", end, Mathf.Max(0.01f, TravelSeconds))
			 //.SetTrans(Tween.TransitionType.Sine)
			 //.SetEase(Tween.EaseType.Out);
//
		//await ToSignal(tween, Tween.SignalName.Finished);
//
		//if (blocked)
		//{
			//// “gruda” no escudo um pouco
			//await ToSignal(GetTree().CreateTimer(Mathf.Max(0.01f, StuckSecondsOnShield)), SceneTreeTimer.SignalName.Timeout);
			//QueueFree();
			//return;
		//}
//
		//// Hit no mage
		//if (mage is Mage m)
			//m.ApplyDamage(damage);
//
		//QueueFree();
	//}
//}
