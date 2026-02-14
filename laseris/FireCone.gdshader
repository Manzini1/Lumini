// (Opcional) Shader standalone equivalente ao embutido no script acima.
// Use só se você preferir manter o shader como arquivo separado.
shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float progress  = 0.0;
uniform float fade      = 1.0;
uniform float dissolve  = 0.0;
uniform float intensity = 1.0;
uniform float width_boost = 1.0;

uniform float start_width = 0.10;
uniform float end_width   = 1.00;

uniform float noise_scale = 10.0;
uniform float noise_speed = 4.0;
uniform float edge_softness = 0.10;
uniform float heat_warp = 0.14;
uniform float arc_up = 0.0;

uniform float tip_uv = 0.07;
uniform float cap_roundness = 0.85;

uniform vec4 core_color : source_color = vec4(1.0, 0.95, 0.65, 1.0);
uniform vec4 mid_color  : source_color = vec4(1.0, 0.55, 0.20, 1.0);
uniform vec4 glow_color : source_color = vec4(1.0, 0.25, 0.08, 1.0);

float hash(vec2 p){
    return fract(sin(dot(p, vec2(127.1,311.7))) * 43758.5453123);
}
float noise2(vec2 p){
    vec2 i = floor(p);
    vec2 f = fract(p);
    float a = hash(i);
    float b = hash(i + vec2(1.0,0.0));
    float c = hash(i + vec2(0.0,1.0));
    float d = hash(i + vec2(1.0,1.0));
    vec2 u = f*f*(3.0-2.0*f);
    return mix(a,b,u.x) + (c-a)*u.y*(1.0-u.x) + (d-b)*u.x*u.y;
}
float fbm(vec2 p){
    float v = 0.0;
    float a = 0.55;
    for(int i=0;i<4;i++){
        v += a * noise2(p);
        p *= 2.02;
        a *= 0.52;
    }
    return v;
}

void fragment(){
    vec2 uv = UV;

    uv.y -= uv.x * arc_up;

    float t = TIME * noise_speed;

    float nA = fbm(uv * noise_scale + vec2(-t,  t*0.35));
    float nB = fbm(uv * (noise_scale*1.8) + vec2(t*0.6, -t));
    uv.y += (nA - 0.5) * heat_warp;
    uv.x += (nB - 0.5) * (heat_warp * 0.15);

    float w = mix(start_width, end_width, uv.x) * width_boost;

    float tongue = fbm(vec2(uv.x*noise_scale*0.9 + t*0.9, uv.y*noise_scale*1.3 - t*1.2));
    w *= (0.80 + 0.55 * tongue);

    float tipN = fbm(vec2(uv.y * noise_scale * 1.2, TIME * noise_speed * 0.6));
    float cut = progress - tip_uv * (0.10 + 0.90 * tipN);
    if (uv.x > cut) discard;

    float capT = clamp((cut - uv.x) / max(tip_uv, 1e-4), 0.0, 1.0);

    float dy = abs(uv.y - 0.5);
    float edge = smoothstep(w*0.5, w*0.5 - edge_softness, dy);

    float wHalf = max(w * 0.5, 1e-4);
    vec2 capP = vec2(1.0 - capT, (uv.y - 0.5) / wHalf);
    float capRound = smoothstep(1.0, 0.0, length(capP));
    float tipMask = mix(1.0, capRound, cap_roundness);

    w *= mix(1.0, 0.85 + 0.20 * tipN, 1.0 - capT);

    float dmask = smoothstep(dissolve, dissolve + 0.25, tongue);
    float alpha = edge * dmask * fade * tipMask;

    float coreMask = smoothstep(0.22, 0.0, dy);
    float midMask  = smoothstep(0.38, 0.12, dy);

    vec3 col = glow_color.rgb;
    col = mix(col, mid_color.rgb, midMask);
    col = mix(col, core_color.rgb, coreMask);

    float heat = smoothstep(0.0, 0.35, uv.x) * (1.0 - smoothstep(0.55, 1.0, uv.x));
    col *= (1.0 + 0.35 * heat);

    COLOR = vec4(col * intensity, alpha);
}
