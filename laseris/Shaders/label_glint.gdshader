shader_type canvas_item;

uniform float glint_pos = -1.0;
uniform float glint_intensity = 0.0;

uniform vec2  glint_dir = vec2(0.70710678, -0.70710678);

uniform float glint_width = 0.13;
uniform float glint_core_width = 0.045;
uniform float glint_soft = 0.035;

uniform vec4 glint_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform vec4 core_color  : source_color = vec4(1.0, 0.95, 0.75, 1.0);

uniform float sparkle_strength = 0.25;
uniform float sparkle_scale = 220.0;
uniform float sparkle_speed = 2.2;
uniform float sparkle_gate = 0.65;

float hash21(vec2 p)
{
	p = fract(p * vec2(123.34, 345.45));
	p += dot(p, p + 34.345);
	return fract(p.x * p.y);
}

void fragment()
{
	vec4 base = COLOR;
	float mask = base.a;

	vec2 d = normalize(glint_dir);
	float axis = (dot(UV, d) + 0.70710678) / 1.41421356;
	float dist = abs(axis - glint_pos);

	float band = 1.0 - smoothstep(glint_width, glint_width + glint_soft, dist);
	float core = 1.0 - smoothstep(glint_core_width, glint_core_width + glint_soft, dist);

	float g_band = band * 0.55;
	float g_core = core * 1.10;

	float g = (g_band + g_core) * glint_intensity;

	// sparkle
	float sp = 0.0;
	if (g > 0.001 && mask > 0.001)
	{
		vec2 uvS = UV * sparkle_scale;
		float rnd = hash21(floor(uvS));
		float t = TIME * sparkle_speed;
		float flicker = 0.5 + 0.5 * sin(t + rnd * 6.2831);
		float gate = step(sparkle_gate, rnd);

		sp = gate * flicker * sparkle_strength;
		sp *= smoothstep(0.0, 1.0, core * 1.6 + band * 0.4);
		sp *= glint_intensity;
	}

	// cor do brilho (core mais quente)
	vec3 tint = glint_color.rgb * g_band + core_color.rgb * g_core;

	// ✅ em vez de "só somar", a gente TINGE o texto (aparece mesmo no branco)
	float k = clamp(g * mask, 0.0, 1.0);

	// escurece um tiquinho e tinge
	base.rgb = mix(base.rgb, base.rgb * 0.78 + tint * 0.22, k);

	// ainda dá um highlight leve (não depende de HDR)
	base.rgb += tint * (k * 0.18);

	// sparkle levinho
	base.rgb += vec3(1.0, 0.98, 0.85) * sp * 0.25 * mask;

	COLOR = base;
}
