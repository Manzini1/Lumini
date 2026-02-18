shader_type canvas_item;

/*
Orb glow shader (AAA-ish)
- Desenha um círculo (SDF) dentro do ColorRect
- Halo externo + núcleo brilhante
- Shimmer/energia sutil animada
*/

uniform vec4 core_color : source_color = vec4(0.3, 0.2, 0.50, 0.4);
uniform vec4 rim_color  : source_color = vec4(0.4, 1, 1, 1);
uniform float intensity = 1;     // 0..2
uniform float softness  = 0.06;    // borda do círculo
uniform float halo_size = 1;    // tamanho do halo

uniform float shimmer_strength = 0.35;
uniform float shimmer_speed = 2.2;
uniform float shimmer_freq = 10.0;

float hash21(vec2 p)
{
	p = fract(p * vec2(123.34, 345.45));
	p += dot(p, p + 34.345);
	return fract(p.x * p.y);
}

void fragment()
{
	// UV 0..1 => centro (0.5,0.5)
	vec2 p = UV - vec2(0.5);
	// Corrige aspecto (pra não virar oval quando rect não é quadrado)
	vec2 asp = vec2(1.0, 1.0);
	// TEXTURE_PIXEL_SIZE existe, mas aqui é mais simples:
	// Se seu orb for sempre quadrado, pode ignorar.
	// Se não for, prefira deixar o Control/ColorRect quadrado (recomendado).
	p *= asp;

	float r = length(p);

	// círculo base (SDF)
	float radius = 0.34;
	float edge = smoothstep(radius, radius - softness, r); // 1 dentro, 0 fora

	// halo externo
	float halo = smoothstep(radius + halo_size, radius, r);
	halo = pow(halo, 2.2);

	// brilho no rim
	float rim = smoothstep(radius, radius - softness * 1.4, r) - smoothstep(radius - softness * 2.8, radius - softness * 4.0, r);
	rim = clamp(rim, 0.0, 1.0);

	// shimmer (faixas internas)
	float t = TIME * shimmer_speed;
	float bands = sin((p.x * shimmer_freq) + t * 6.2831) * 0.5 + 0.5;
	bands = pow(bands, 2.2);

	// ruído sutil
	float n = hash21(floor(UV * 80.0));
	float flick = 0.6 + 0.4 * sin(t * 3.2 + n * 6.2831);

	float inner = edge * (0.55 + 0.45 * bands) * flick;

	vec3 col = vec3(0.0);

	// núcleo
	col += core_color.rgb * inner * intensity;

	// rim e halo
	col += rim_color.rgb * (rim * 0.1 + halo * 0.52) * intensity;

	// sparkle sutil
	col += vec3(1.0, 0.98, 0.90) * shimmer_strength * bands * edge * 0.25 * intensity;

	// alpha final: usa edge + halo
	float a = clamp(edge + halo * 0.75, 0.0, 1.0);

	COLOR = vec4(col, a);
}
