using Godot;
using System.Collections.Generic;

public partial class HUDController : Control
{
	private Dictionary<Key, TextureRect> _icons = new();

	public override void _Ready()
	{
		// Mapeamento das teclas para seus ícones
		_icons[Key.Q] = GetNode<TextureRect>("Icon_Fogo");
		_icons[Key.W] = GetNode<TextureRect>("Icon_Gelo");
		_icons[Key.E] = GetNode<TextureRect>("Icon_Raio");
		_icons[Key.R] = GetNode<TextureRect>("Icon_Veneno");

		_icons[Key.U] = GetNode<TextureRect>("Icon_Terra");
		_icons[Key.I] = GetNode<TextureRect>("Icon_Luz");
		_icons[Key.O] = GetNode<TextureRect>("Icon_Sombra");
		_icons[Key.P] = GetNode<TextureRect>("Icon_Ar");
	}

	public override void _Process(double delta)
	{
		foreach (var entry in _icons)
		{
			bool isDown = Input.IsPhysicalKeyPressed(entry.Key);

			if (isDown)
			{
				// brilho dourado quando pressionado
				entry.Value.Modulate = new Color(1f, 0.85f, 0.5f);
			}
			else
			{
				// volta ao normal
				entry.Value.Modulate = Colors.White;
			}
		}
	}
}
