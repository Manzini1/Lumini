shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float progress = 1.0;
uniform float fade = 1.0;
uniform float intensity = 1.0;

uniform vec4 glow_color : source_color = vec4(0.70, 0.95, 1.00, 1.0);

uniform float arc_center_x = -0.20;
uniform float outer_r = 1.05;
uniform float thickness = 0.22;
uniform float edge_soft = 0.10;
uniform float curve = 0.18;

void fragment(){
    vec2 uv = UV;

    float pr = smoothstep(progress, progress - 0.08, uv.x);
    if (pr <= 0.001) discard;

    vec2 p = uv * 2.0 - 1.0;
    p.y += sin((uv.x) * 3.14159) * curve;

    vec2 c = vec2(arc_center_x, 0.0);
    float r = length(p - c);

    float inner_r = outer_r - thickness;

    float outer = smoothstep(outer_r, outer_r - edge_soft, r);
    float inner = smoothstep(inner_r - edge_soft, inner_r, r);
    float ring = outer * inner;

    float taper = smoothstep(0.02, 0.18, uv.x) * (1.0 - smoothstep(0.70, 0.98, uv.x));
    float mask = ring * pr * taper;

    if (mask <= 0.002) discard;

    // core extra (mais intenso no “miolo” do arco)
    float core = smoothstep(0.0, 0.35, (ring)) * (1.0 - smoothstep(0.55, 1.0, uv.x));
    float a = mask * fade * (0.75 + 0.45 * core);

    COLOR = vec4(glow_color.rgb * intensity, a);
}