shader_type canvas_item;

uniform float reveal : hint_range(0.0, 1.0) = 1.0; // 0 = só centro, 1 = inteiro
uniform float feather : hint_range(0.0, 0.25) = 0.06;

void fragment() {
	vec4 col = texture(TEXTURE, UV);

	// largura visível cresce do centro pra fora
	float halfw = max(0.001, 0.5 * reveal);
	float d = abs(UV.x - 0.5);

	// máscara com borda suave
	float mask = 1.0 - smoothstep(halfw, halfw + feather, d);

	col.a *= mask;
	COLOR = col;
}
