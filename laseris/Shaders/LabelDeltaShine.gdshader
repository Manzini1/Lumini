shader_type canvas_item;

uniform float is_lead = 0.0;          // 0=rubi, 1=metal
uniform float lead_boost = 0.0;       // 0..1 (brilho extra no metal)

uniform float spark_pos = -1.0;       // -0.2..1.2
uniform float spark_intensity = 0.0;  // 0..1
uniform float spark_width = 0.035;    // fino
uniform vec2  spark_dir = vec2(0.80, -0.60);

uniform vec4 ruby_color  : source_color = vec4(0.95, 0.15, 0.22, 1.0);
uniform vec3 ir_a = vec3(0.25, 0.80, 1.00);
uniform vec3 ir_b = vec3(0.95, 0.35, 1.00);
uniform vec3 ir_c = vec3(1.00, 0.90, 0.25);

float tri(float x){ return abs(fract(x) - 0.5) * 2.0; }

vec3 iridescent(float x){
	float t1 = 1.0 - tri(x);
	float t2 = 1.0 - tri(x + 0.333);
	float t3 = 1.0 - tri(x + 0.666);
	vec3 col = ir_a * t1 + ir_b * t2 + ir_c * t3;
	return col / (t1 + t2 + t3 + 1e-5);
}

void fragment(){
	vec4 m = texture(TEXTURE, UV);
	float a = m.a;

	// mask = 0 quando não há texto, 1 quando há
	float mask = step(0.001, a);

	float t = TIME;

	float grad = UV.x * 1.15 + UV.y * 0.10 + t * 0.10;
	vec3 metal = iridescent(grad);

	float k = clamp(is_lead, 0.0, 1.0);
	vec3 base = mix(ruby_color.rgb, metal, k);

	float polish = (0.25 + 0.65 * lead_boost) * k;

	// spark
	vec2 d = normalize(spark_dir);
	float axis = (dot(UV, d) + 0.70710678) / 1.41421356;
	float dist = abs(axis - spark_pos);
	float spark = 1.0 - smoothstep(spark_width, spark_width * 1.8, dist);
	spark *= spark_intensity;

	vec3 spark_col = vec3(1.0, 0.98, 0.90) * (spark * (0.5 + 1.2 * k));

	vec3 col = base * (1.05 + polish) + spark_col;

	// aplica máscara: sem texto -> transparente
	COLOR = vec4(col * mask, a * mask);
}
