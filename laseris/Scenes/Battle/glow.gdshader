shader_type canvas_item;
render_mode blend_add, unshaded;

uniform float fill01 = 0.0;
uniform float wobble = 0.0;
uniform float time = 0.0;
uniform float flash01 = 0.0;
uniform float charged01 = 0.0;

// mesma geometria do buraco
uniform vec2 center_uv = vec2(0.5, 0.53);
uniform float inner_radius = 0.285;

// controle do halo
uniform float halo_inner_thickness = 0.020; // faixa interna
uniform float halo_outer_thickness = 0.030; // “espalhamento” pra fora
uniform float halo_softness = 0.020;

// cor final do charged: #28AFF9
uniform vec3 glow_color = vec3(0.157, 0.686, 0.976);

float hash21(vec2 p) {
	p = fract(p * vec2(123.34, 345.45));
	p += dot(p, p + 34.345);
	return fract(p.x * p.y);
}

void fragment()
{
	float d = distance(UV, center_uv);

	// anel ao redor do inner_radius (um pouco pra dentro e um pouco pra fora)
	float inner = smoothstep(inner_radius, inner_radius - halo_inner_thickness, d);
	float outer = smoothstep(inner_radius + halo_outer_thickness, inner_radius, d);
	float ring = inner * outer;

	// suaviza bordas
	float soft = smoothstep(0.0, halo_softness, ring);

	// intensidade aumenta conforme enche, e fica bem forte no charged
	float f = clamp(fill01, 0.0, 1.0);
	float c = clamp(charged01, 0.0, 1.0);

	float pulse = 0.65 + 0.35 * sin(time * 7.0);
	float baseI = mix(0.20, 0.70, f);
	float chargedI = mix(0.0, 1.40, c) * pulse;

	// “sparkles” discretos no halo
	float sp = hash21(UV * 90.0 + vec2(time * 1.1, -time * 1.3));
	float spark = smoothstep(0.985, 1.0, sp) * (0.20 + 0.80 * c);

	// flash também reforça halo
	float flashI = flash01 * 1.25;

	vec3 col = glow_color * (baseI + chargedI);
	col += glow_color * spark * 2.0;
	col += vec3(1.6, 1.0, 0.6) * flashI; // tom quente no flash

	// alpha do halo
	float a = soft * (0.35 + 0.65 * (baseI + chargedI)) + spark * 0.25;

	COLOR = vec4(col, a);
}
