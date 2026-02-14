shader_type canvas_item;

uniform sampler2D noise_tex;
uniform vec4 aura_color : source_color = vec4(0.3, 0.7, 1.0, 1.0);

uniform float noise_scale = 3.0;
uniform float speed = 1.0;

// formato base do blob
uniform float radius = 0.42;        // 0.35..0.48
uniform vec2  ellipse = vec2(1.0, 1.0); // (1.2, 0.9) deixa oval

// quanto a borda muda de forma
uniform float wobble = 0.10;        // 0.05..0.18
uniform float edge_soft = 0.10;     // 0.06..0.20

// opcional: usar o alpha do PNG como “máscara extra”
uniform float texture_influence = 0.5; // 0..1
uniform float uv_warp = 0.03;          // 0.00..0.06

void fragment() {
    vec2 uv = UV;

    // coordenada “centralizada” pra fazer círculo/oval
    vec2 p = (uv - vec2(0.5)) * 2.0;
    p /= ellipse;
    float r = length(p); // 0 no centro, ~1 na borda

    // noise animado com wrap (fract)
    vec2 n_uv1 = fract(uv * noise_scale + vec2(TIME * speed, TIME * speed * 0.77));
    vec2 n_uv2 = fract(uv * (noise_scale * 1.7) + vec2(-TIME * speed * 0.63, TIME * speed * 0.41));

    float n1 = texture(noise_tex, n_uv1).r;
    float n2 = texture(noise_tex, n_uv2).r;
    float n  = n1 * 0.6 + n2 * 0.4;

    // ✅ isso aqui é o que muda o “shape”: raio base + wobble por noise
    float rr = radius + (n - 0.5) * wobble;

    // máscara do blob com borda suave
    float mask = 1.0 - smoothstep(rr, rr + edge_soft, r);

    // opcional: usa o PNG como detalhe (se quiser irregularidade)
    vec2 warp = (vec2(n1, n2) - 0.5) * uv_warp;
    float texA = texture(TEXTURE, uv + warp).a;

    float a = mix(mask, mask * texA, texture_influence);

    COLOR = vec4(aura_color.rgb, aura_color.a * a);
}
