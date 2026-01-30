shader_type canvas_item;

uniform float bend_strength = 0.18; // 0.05 a 0.30
uniform float frequency = 3.0;      // 1 a 8
uniform float speed = 2.0;          // 0.2 a 6

void fragment() {
    vec2 uv = UV;

    // ondas ao longo do comprimento (uv.x)
    float x = uv.x;
    float t = TIME * speed;

    float w1 = sin((x * frequency + t) * 6.2831853);
    float w2 = 0.6 * sin((x * frequency * 2.0 - t * 1.3) * 6.2831853);

    // concentra a curvatura mais no “miolo” da linha pra não virar borracha nas bordas
    float center_mask = 1.0 - abs(uv.y - 0.5) * 2.0; // 1 no centro, 0 nas bordas
    center_mask = clamp(center_mask, 0.0, 1.0);

    uv.y += (w1 + w2) * bend_strength * center_mask;

    vec4 tex = texture(TEXTURE, uv);
    COLOR = tex * COLOR;
}
