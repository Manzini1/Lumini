using Godot;
using System;
using System.Collections.Generic;

public partial class Mage : Sprite2D
{
	// Mapeamento das teclas para elementos
	private readonly Dictionary<Key, string> _elementMap = new()
	{
		{ Key.Q, "Fogo" },
		{ Key.W, "Gelo" },
		{ Key.E, "Raio" },
		{ Key.R, "Veneno" },

		{ Key.U, "Terra" },
		{ Key.I, "Luz" },
		{ Key.O, "Sombra" },
		{ Key.P, "Ar" }
	};

	// Buffer do combo atual
	private readonly List<string> _comboBuffer = new();

	// Teclas pressionadas no frame anterior
	private HashSet<Key> _keysDownLastFrame = new();

	// Espaço pressionado no frame anterior
	private bool _spaceDownLastFrame = false;

	// Sistema de tempo do combo
	private float _comboTimer = 0f;
	private const float ComboTimeout = 2f; // 2 segundos

	public override void _Process(double delta)
	{
		bool comboAdvancedThisFrame = false;

		var keysDownThisFrame = new HashSet<Key>();

		// Detectar teclas de elementos
		foreach (var entry in _elementMap)
		{
			var key = entry.Key;
			var element = entry.Value;

			bool isDown = Input.IsPhysicalKeyPressed(key);

			if (isDown)
			{
				keysDownThisFrame.Add(key);

				// JUST PRESSED manual
				if (!_keysDownLastFrame.Contains(key))
				{
					_comboBuffer.Add(element);
					comboAdvancedThisFrame = true; // O combo avançou
					GD.Print($"Elemento: {element}");
				}
			}
		}

		// Resetar timer quando avança o combo
		if (comboAdvancedThisFrame)
		{
			_comboTimer = 0f;
		}

		// TIMER DE EXPIRAÇÃO DO COMBO
		if (_comboBuffer.Count > 0)
		{
			_comboTimer += (float)delta;

			if (_comboTimer >= ComboTimeout)
			{
				GD.Print("Combo expirou! ❌");
				_comboBuffer.Clear();
				_comboTimer = 0f;
			}
		}

		// CAST COM ESPAÇO (just pressed manual)
		bool spaceDown = Input.IsPhysicalKeyPressed(Key.Space);

		if (spaceDown && !_spaceDownLastFrame)
		{
			if (_comboBuffer.Count > 0)
			{
				string combo = string.Join(" + ", _comboBuffer);
				GD.Print($"CAST! → {combo} ✔️");
				_comboBuffer.Clear();
				_comboTimer = 0f;
			}
		}

		// Atualizar estados
		_keysDownLastFrame = keysDownThisFrame;
		_spaceDownLastFrame = spaceDown;
	}
}
