shader_type canvas_item;

uniform float fill01 = 0.0;    // 0..1
uniform float wobble = 0.0;    // 0..~0.35
uniform float time = 0.0;
uniform float flash01 = 0.0;   // 0..1
uniform float charged01 = 0.0; // 0..1 (quando enche)

uniform vec2 center_uv = vec2(0.5, 0.53);
uniform float inner_radius = 0.285;
uniform float feather = 0.004;

uniform float wave_amp = 0.020;
uniform float wave_freq = 2.2;
uniform float wave_speed = 3.2;

// fase “normal” (antes de encher)
uniform vec3 color_orange = vec3(1.00, 0.45, 0.12);
uniform vec3 color_blood  = vec3(0.55, 0.03, 0.02);

// ✅ charged (final): #28AFF9 => (0.157, 0.686, 0.976)
uniform vec3 charged_color = vec3(0.157, 0.686, 0.976);

float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 345.45));
	p += dot(p, p + 34.345);
	return fract(p.x * p.y);
}

void fragment()
{
	float d = distance(UV, center_uv);
	float circle = smoothstep(inner_radius, inner_radius - feather, d);

	float topY = center_uv.y - inner_radius;
	float botY = center_uv.y + inner_radius;

	float f = clamp(fill01, 0.0, 1.0);
	float baseLevel = mix(botY, topY, f);

	float wave  = sin((UV.x * 6.2831 * wave_freq) + time * wave_speed) * (wobble * wave_amp);
	float jitter = (hash21(UV * 18.0 + vec2(time*0.15, time*0.07)) - 0.5) * 0.010 * wobble;

	float level = baseLevel + wave + jitter;

	// enche de baixo pra cima (UV.y cresce pra baixo)
	float filled = step(level, UV.y);

	// normal: laranja -> sangue conforme enche
	float t = pow(smoothstep(0.0, 1.0, f), 1.15);
	vec3 baseCol = mix(color_orange, color_blood, t);

	// “mistura” sutil
	float n = hash21(UV * 32.0 + vec2(time*0.35, -time*0.28));
	baseCol += (n - 0.5) * 0.10;
	baseCol = clamp(baseCol, 0.0, 1.8);

	// depth escurece
	float depth = clamp((UV.y - level) / max(1e-4, (botY - level)), 0.0, 1.0);
	baseCol *= mix(1.10, 0.75, depth);

	// highlight na superfície
	float surfBand = 1.0 - smoothstep(0.0, 0.010, abs(UV.y - level));
	vec3 surf = vec3(1.20, 0.75, 0.35) * surfBand * (0.55 + 0.45 * (1.0 - depth));

	// rim interno
	float rim = smoothstep(inner_radius - 0.020, inner_radius - 0.001, d);
	vec3 rimGlow = vec3(1.10, 0.55, 0.20) * rim * (0.35 + 0.65 * t);

	// flash
	vec3 flash = vec3(1.8, 0.8, 0.4) * flash01;
	float flashRim = rim * flash01;

	// ✅ charged: puxa a cor pro azul mágico + adiciona “energia”
	float c = clamp(charged01, 0.0, 1.0);
	float pulse = 0.65 + 0.35 * sin(time * 6.0);

	// mistura pro charged sem perder profundidade
	vec3 chargedBase = charged_color * (1.05 + 0.15 * pulse);
	baseCol = mix(baseCol, chargedBase, c * 0.80);

	// superfície e rim mais “geladinhos”
	vec3 chargedSurf = charged_color * (1.40 + 0.40 * pulse) * surfBand;
	vec3 chargedRim  = charged_color * (1.90 + 0.60 * pulse) * rim;

	surf    = mix(surf, chargedSurf, c);
	rimGlow = mix(rimGlow, chargedRim, c);

	vec3 rgb = baseCol + surf + rimGlow
	         + flash * 0.35 + flashRim * vec3(2.0, 1.0, 0.6);

	vec4 outCol = vec4(rgb, 0.92);
	outCol.a *= circle * filled;

	COLOR = outCol;
}
