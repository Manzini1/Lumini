shader_type canvas_item;

uniform vec4 base_color : source_color = vec4(0.15, 0.55, 1.00, 1.0);
uniform vec4 glow_color : source_color = vec4(0.60, 0.90, 1.00, 1.0);

uniform float energy_intensity = 0.6;   // 0..1
uniform float lead_boost = 0.0;         // 0..1 (quando está ganhando)
uniform float scroll_speed = 1.8;
uniform float band_freq = 18.0;
uniform float noise_strength = 0.25;

float hash21(vec2 p){
	p = fract(p * vec2(123.34, 345.45));
	p += dot(p, p + 34.345);
	return fract(p.x * p.y);
}

void fragment(){
	vec4 c = COLOR; // ColorRect base (não importa muito)
	float y = UV.y;

	// base vertical gradient (mais claro no centro)
	float center = 1.0 - abs(y - 0.5) * 2.0;  // 1 no meio, 0 nas bordas
	center = pow(center, 1.8);

	// moving bands (energia correndo)
	float t = TIME * scroll_speed;
	float bands = sin((UV.x * band_freq) + t * 6.2831) * 0.5 + 0.5;
	bands = pow(bands, 2.2);

	// subtle noise flicker
	float n = hash21(floor(UV * vec2(220.0, 28.0)));
	float flick = sin(t * 3.3 + n * 6.2831) * 0.5 + 0.5;

	float intensity = energy_intensity + lead_boost * 0.55;
	float core = center * (0.65 + 0.35 * bands);
	float sparkle = flick * noise_strength * (0.25 + 0.75 * center);

	vec3 col = base_color.rgb;
	col += glow_color.rgb * (core * intensity);
	col += vec3(1.0, 1.0, 1.0) * sparkle * intensity * 0.25;

	// fade nas bordas (ajuda a parecer “tubo”)
	float edge = smoothstep(0.0, 0.10, y) * smoothstep(0.0, 0.10, 1.0 - y);
	col *= edge * 1.15;

	COLOR = vec4(col, 1.0);
}
