public enum CastOutcome
{
	Hit,              // acertou e deu dano
	Miss,             // errou (ex: spell só ground e inimigo é air)
	Blocked,          // escudo negou/absorveu sem cura
	Absorbed50,       // escudo absorveu 1 elemento e curou 50%
	Absorbed100,      // escudo absorveu 2 elementos e curou 100%
	CancelledNoTarget,
	CancelledNoElements,
	CancelledInputDisabled
}
