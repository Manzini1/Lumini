shader_type canvas_item;

// 0..1 (seu código controla)
// loser = 0, leader = 1
uniform float lead_boost = 0.0;

// força do efeito
uniform float energy_intensity = 1.25;

// ---- Ruby (loser) ----
uniform vec4 ruby_base : source_color = vec4(0.45, 0.02, 0.08, 1.0);
uniform vec4 ruby_glow : source_color = vec4(1.60, 0.12, 0.18, 1.0);

// ---- Metal iridescent (leader) ----
uniform vec4 metal_base : source_color = vec4(0.14, 0.16, 0.20, 1.0);
uniform float iridescence_amount = 1.6;   // 1.2..2.2
uniform float iridescence_speed  = 1.2;
uniform float iridescence_freq   = 2.4;

// sheen (faixa brilhante correndo)
uniform float sheen_speed = 1.8;
uniform float sheen_freq  = 2.6;
uniform float sheen_width = 0.08;         // 0.05..0.12

// energia correndo (bands)
uniform float band_freq  = 14.0;
uniform float band_speed = 2.0;
uniform float band_pow   = 2.2;

// micro flicker
uniform float noise_strength = 0.20;

float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 345.45));
	p += dot(p, p + 34.345);
	return fract(p.x * p.y);
}

// “fake spectrum” (RGB defasado) pra iridescência
vec3 spectral(float x) {
	return 0.55 + 0.45 * sin(vec3(0.0, 2.1, 4.2) + x * 6.2831853);
}

void fragment() {
	float x = UV.x;
	float t = TIME;

	// alpha vem do ColorRect/Modulate
	float a = COLOR.a;

	if (a <= 0.001) {
		COLOR = vec4(0.0);
	} else {
		// bands (energia correndo)
		float bands = sin((x * band_freq) + t * band_speed * 6.2831853) * 0.5 + 0.5;
		bands = pow(bands, band_pow);

		// flicker/noise
		float n = hash21(floor(UV * vec2(420.0, 12.0)));
		float flick = 0.75 + 0.25 * sin(t * 6.0 + n * 6.2831853);

		// ---------------- LOSER (RUBY) ----------------
		float ruby_core = (0.55 + 0.45 * bands) * energy_intensity;
		vec3 ruby = ruby_base.rgb + ruby_glow.rgb * ruby_core;
		ruby *= (0.92 + 0.08 * flick);

		// ---------------- LEADER (METAL IRIDESCENT) ----------------
		float iri_axis = (x * iridescence_freq) + t * iridescence_speed;
		vec3 iri = spectral(iri_axis) * iridescence_amount;

		vec3 metal = metal_base.rgb * (0.85 + 0.55 * iri);

		// sheen correndo
		float s = fract(x * sheen_freq + t * sheen_speed);
		float dist = abs(s - 0.5);
		float sheen = 1.0 - smoothstep(sheen_width, sheen_width + 0.05, dist);
		metal += vec3(1.0) * sheen * 1.7 * energy_intensity;

		metal += vec3(1.0, 0.98, 0.90) * (bands * 0.35) * energy_intensity;

		float sp = step(0.85, n) * (0.6 + 0.4 * flick) * noise_strength;
		metal += vec3(1.0, 0.98, 0.92) * sp * 0.55 * energy_intensity;

		// ---------------- MIX ----------------
		float k = clamp(lead_boost, 0.0, 1.0);
		vec3 col = mix(ruby * 0.95, metal * 1.15, k);

		COLOR = vec4(col, a);
	}
}
