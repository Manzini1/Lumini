shader_type canvas_item;

uniform float lead_boost = 0.0;
uniform float energy_intensity = 1.25;

// Ruby
uniform vec4 ruby_base : source_color = vec4(0.45, 0.02, 0.08, 1.0);
uniform vec4 ruby_glow : source_color = vec4(1.60, 0.12, 0.18, 1.0);

// Metal iridescent
uniform vec4 metal_base : source_color = vec4(0.14, 0.16, 0.20, 1.0);
uniform float iridescence_amount = 1.6;
uniform float iridescence_speed  = 1.2;
uniform float iridescence_freq   = 2.4;

// bands
uniform float band_freq  = 14.0;
uniform float band_speed = 2.0;
uniform float band_pow   = 2.2;

// micro flicker
uniform float noise_strength = 0.16;

// spark pontinho
uniform float spark_speed = 1.2;
uniform float spark_width = 0.012;
uniform float spark_soft  = 0.010;
uniform float spark_duty  = 0.18;   // menor = mais raro (0.08..0.25)
uniform float spark_gain  = 2.2;

float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 345.45));
	p += dot(p, p + 34.345);
	return fract(p.x * p.y);
}

vec3 spectral(float x) {
	return 0.55 + 0.45 * sin(vec3(0.0, 2.1, 4.2) + x * 6.2831853);
}

void fragment() {
	float x = UV.x;
	float t = TIME;
	float a = COLOR.a;

	// mask: 0 se alpha baixo
	float mask = step(0.001, a);

	// bands
	float bands = sin((x * band_freq) + t * band_speed * 6.2831853) * 0.5 + 0.5;
	bands = pow(bands, band_pow);

	// noise
	float n = hash21(floor(UV * vec2(420.0, 12.0)));
	float flick = 0.75 + 0.25 * sin(t * 6.0 + n * 6.2831853);

	// Ruby
	float ruby_core = (0.55 + 0.45 * bands) * energy_intensity;
	vec3 ruby = ruby_base.rgb + ruby_glow.rgb * ruby_core;
	ruby *= (0.92 + 0.08 * flick);

	// Metal
	float iri_axis = (x * iridescence_freq) + t * iridescence_speed;
	vec3 iri = spectral(iri_axis) * iridescence_amount;
	vec3 metal = metal_base.rgb * (0.85 + 0.55 * iri);

	metal += vec3(1.0, 0.98, 0.90) * (bands * 0.28) * energy_intensity;

	float sp = step(0.88, n) * (0.6 + 0.4 * flick) * noise_strength;
	metal += vec3(1.0, 0.98, 0.92) * sp * 0.45 * energy_intensity;

	// spark raro (janela no tempo)
	float phase = fract(t * spark_speed);
	float gate  = step(1.0 - spark_duty, phase);

	float local = (phase - (1.0 - spark_duty)) / max(1e-5, spark_duty);
	local = clamp(local, 0.0, 1.0);

	float dist = abs(x - local);
	float spark = 1.0 - smoothstep(spark_width, spark_width + spark_soft, dist);

	metal += vec3(1.0, 0.98, 0.90) * spark * gate * spark_gain * energy_intensity;

	// mix
	float k = clamp(lead_boost, 0.0, 1.0);
	vec3 col = mix(ruby * 0.95, metal * 1.15, k);

	COLOR = vec4(col * mask, a * mask);
}
