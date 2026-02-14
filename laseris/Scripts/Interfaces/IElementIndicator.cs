using Godot;

namespace Game.Combat
{
	/// <summary>
	/// Qualquer indicador visual do elemento do inimigo (aura, círculo, etc.).
	/// O BattleController só conversa por essa interface.
	/// </summary>
	public interface IElementIndicator
	{
		int CurrentElementId { get; }
		void SetElement(int elementId);
		void SetEnabled(bool enabled);

		/// <summary>
		/// Tamanho “lógico” em pixels do indicador (ex.: para inimigos maiores).
		/// Implementação decide como aplicar (scale, sprite size, etc.)
		/// </summary>
		void SetSize(Vector2 size);
	}
}
