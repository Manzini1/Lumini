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

// ponta
uniform float tip_uv = 0.07;
uniform float cap_roundness = 0.85;

// updraft
uniform float rise_uv = 0.12;
uniform float rise_start = 0.60;
uniform float rise_wobble = 0.55;
uniform float rise_asym = 0.22;

// mata resíduo nas bordas do quad (evita “linha/retângulo”)
uniform float uv_edge_fade = 0.02;

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

	// arco do cone
	uv.y -= uv.x * arc_up;

	float t = TIME * noise_speed;

	// heat shimmer
	float nA = fbm(uv * noise_scale + vec2(-t,  t*0.35));
	float nB = fbm(uv * (noise_scale*1.8) + vec2(t*0.6, -t));
	uv.y += (nA - 0.5) * heat_warp;
	uv.x += (nB - 0.5) * (heat_warp * 0.15);

	// largura base
	float w = mix(start_width, end_width, uv.x) * width_boost;

	// línguas (mais variação = mais “fogo”, menos “fumaça”)
	float tongue = fbm(vec2(uv.x*noise_scale*0.9 + t*0.9, uv.y*noise_scale*1.3 - t*1.2));
	w *= (0.78 + 0.70 * tongue);

	// updraft no final
	float xr = clamp((uv.x - rise_start) / max(1.0 - rise_start, 1e-4), 0.0, 1.0);
	float lift = rise_uv * xr * xr * (0.35 + 0.65 * tongue);

	float wobN = fbm(vec2(uv.x*noise_scale*0.55 + t*0.75, t*0.25));
	float wob = (wobN - 0.5) * rise_wobble * rise_uv * xr;

	float center = 0.5 - lift + wob;

	// corte da ponta (com feather)
	float tipN = fbm(vec2(uv.y * noise_scale * 1.2, TIME * noise_speed * 0.6));
	float cut = progress - tip_uv * (0.10 + 0.90 * tipN);
	if (uv.x > cut) discard;

	float capT = clamp((cut - uv.x) / max(tip_uv, 1e-4), 0.0, 1.0);

	// taper na ponta
	w *= mix(1.0, 0.85 + 0.20 * tipN, 1.0 - capT);

	// dy com assimetria suave (subindo mais em cima)
	float dy = abs(uv.y - center);
	float topMask = smoothstep(0.0, 0.03, center - uv.y);
	dy *= (1.0 - rise_asym * xr * topMask);

	// borda (se parecer “neblina”, diminua edge_softness no Inspector)
	float edge = smoothstep(w*0.5, w*0.5 - edge_softness, dy);

	// cap arredondado
	float wHalf = max(w * 0.5, 1e-4);
	vec2 capP = vec2(1.0 - capT, (uv.y - center) / wHalf);
	float capRound = smoothstep(1.0, 0.0, length(capP));
	float tipMask = mix(1.0, capRound, cap_roundness);

	// dissolve
	float dmask = smoothstep(dissolve, dissolve + 0.25, tongue);
	float alpha = edge * dmask * fade * tipMask;

	// mata qualquer resíduo colado no topo/baixo do QUAD
	float m = max(uv_edge_fade, 1e-4);
	float vfade = smoothstep(0.0, m, uv.y) * smoothstep(0.0, m, 1.0 - uv.y);
	alpha *= vfade;

	// “temperatura” ao longo do cone
	float x = clamp(uv.x, 0.0, 1.0);
	float xHeat = smoothstep(0.05, 0.85, x);

	float coreMask = smoothstep(0.18, 0.0, dy);
	float midMask  = smoothstep(0.42, 0.14, dy);

	vec3 col = glow_color.rgb;
	col = mix(col, mid_color.rgb, midMask);
	col = mix(col, core_color.rgb, coreMask);

	// hot core boost
	float hot = pow(coreMask, 2.6) * (1.0 - 0.55 * x);
	col += core_color.rgb * hot * 1.35;

	// leve cool na origem (opcional)
	vec3 coolTint = vec3(0.55, 0.85, 1.0);
	float cool = (1.0 - smoothstep(0.0, 0.18, x)) * 0.18;
	col = mix(col, coolTint, cool);

	// redshift no fim
	vec3 tipTint = vec3(1.0, 0.18, 0.05);
	float redshift = smoothstep(0.35, 1.0, x) * 0.42;
	col = mix(col, tipTint, redshift);

	// flicker
	float flick = 0.88 + 0.12 * sin(TIME*14.0 + x*10.0 + tongue*6.0);
	col *= flick;

	// embers/fiapos
	float emberN = fbm(vec2(uv.x*noise_scale*3.2 + t*2.2, uv.y*noise_scale*6.0 - t*3.1));
	float embers = smoothstep(0.86, 1.0, emberN) * smoothstep(0.10, 0.90, x) * (1.0 - coreMask);
	col += glow_color.rgb * embers * 1.4;
	alpha += embers * 0.22;

	// mais presença no meio do cone
	alpha *= (0.85 + 0.35 * xHeat);
	alpha = clamp(alpha, 0.0, 1.0);

	if (alpha < 0.002) discard;

	COLOR = vec4(col * intensity, alpha);
}
