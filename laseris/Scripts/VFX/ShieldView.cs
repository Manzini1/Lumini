//using Godot;
//using System.Collections.Generic;
//
//public partial class ShieldView : Node
//{
	//[Export] public NodePath ShieldSpritePath;
	//[Export] public ShieldVisualBank Bank;
//
	//private Sprite2D _shieldSprite;
	//private ShieldController _controller;
//
	//public override void _Ready()
	//{
		//_shieldSprite = GetNodeOrNull<Sprite2D>(ShieldSpritePath);
		//_controller = GetParent().GetNodeOrNull<ShieldController>("ShieldController");
//
		//if (_shieldSprite == null) GD.PushWarning("ShieldView: ShieldSpritePath inválido.");
		//if (Bank == null) GD.PushWarning("ShieldView: Bank não setado.");
		//if (_controller == null) GD.PushWarning("ShieldView: não achei ShieldController como irmão.");
//
		//if (_controller != null)
			//_controller.Changed += OnShieldChanged;
//
		//// estado inicial
		//if (_controller != null)
			//OnShieldChanged(_controller.Active);
	//}
//
	//private void OnShieldChanged(HashSet<ElementType> active)
	//{
		//if (_shieldSprite == null || Bank == null)
			//return;
//
		//var tex = Bank.GetTextureFor(active);
		//_shieldSprite.Texture = tex;
//
		//// animação simples de “pop” ao trocar
		//var tw = CreateTween();
		//_shieldSprite.Scale = Vector2.One;
		//tw.TweenProperty(_shieldSprite, "scale", new Vector2(1.12f, 1.12f), 0.08f);
		//tw.TweenProperty(_shieldSprite, "scale", Vector2.One, 0.10f);
	//}
//}
