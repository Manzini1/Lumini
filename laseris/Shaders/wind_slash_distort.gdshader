shader_type canvas_item;
render_mode unshaded, blend_mix;

uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

uniform float progress = 1.0;        // 0..1 revela ao longo do eixo X
uniform float fade = 1.0;            // 0..1 alpha global
uniform float distort_strength = 0.010; // força da refração (screen uv offset)

uniform float noise_scale = 10.0;
uniform float noise_speed = 3.5;

uniform float arc_center_x = -0.20;  // desloca o “centro” do arco (ajusta a curvatura)
uniform float outer_r = 1.05;        // raio externo (em coords -1..1)
uniform float thickness = 0.28;      // espessura do arco
uniform float edge_soft = 0.10;      // suavidade da borda
uniform float curve = 0.18;          // curva adicional (seno)

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

    // reveal (corte suave na ponta)
    float pr = smoothstep(progress, progress - 0.08, uv.x);
    if (pr <= 0.001) discard;

    // coords -1..1
    vec2 p = uv * 2.0 - 1.0;

    // curva extra (dá “swing” de vento)
    p.y += sin((uv.x) * 3.14159) * curve;

    // arco tipo “meia lua” (anel)
    vec2 c = vec2(arc_center_x, 0.0);
    float r = length(p - c);

    float inner_r = outer_r - thickness;

    float outer = smoothstep(outer_r, outer_r - edge_soft, r);
    float inner = smoothstep(inner_r - edge_soft, inner_r, r);

    float ring = outer * inner;

    // taper (mais fino nas pontas)
    float taper = smoothstep(0.02, 0.18, uv.x) * (1.0 - smoothstep(0.70, 0.98, uv.x));
    float mask = ring * pr * taper;

    if (mask <= 0.002) discard;

    float t = TIME * noise_speed;

    // noise para distorcer
    float nA = fbm(p * noise_scale + vec2(-t, t*0.35));
    float nB = fbm(p * (noise_scale*1.7) + vec2(t*0.6, -t));

    // direção de distorção “rodando” ao redor do arco
    vec2 nrm = normalize(vec2(-(p.y - c.y), (p.x - c.x)));
    vec2 ofs = nrm * (nA - 0.5) * distort_strength * mask;
    ofs += vec2((nB - 0.5), (nA - 0.5)) * (distort_strength * 0.35) * mask;

    vec2 suv = SCREEN_UV + ofs;
    vec3 col = texture(SCREEN_TEXTURE, suv).rgb;

    COLOR = vec4(col, mask * fade);
}