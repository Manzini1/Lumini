using Godot;
using System;

namespace Game.Combat;

public partial class FireConeVfx : Node2D
{
	[ExportGroup("Refs")]
	[Export] public NodePath ConeMeshPath = "ConeMesh";
	[Export] public NodePath EndFlamesPath = "EndFlames"; // opcional
	[Export] public NodePath EndEmbersPath = "EndEmbers"; // opcional

	[ExportGroup("Timing")]
	[Export] public float LingerSeconds = 0.06f;
	[Export] public float FadeOutSeconds = 0.08f;
	[Export] public bool QueueFreeOnEnd = true;

	[ExportGroup("Cone Width (px)")]
	[Export] public float StartWidthPx = 8f;
	[Export] public float EndWidthPxBase = 120f;
	[Export] public float EndWidthPxPer100px = 35f;
	[Export] public float ExtraLengthPx = 10f;

	[ExportGroup("Flame Feel")]
	[Export] public float ArcUpPx = 18f;
	[Export] public float Intensity = 1.25f;
	[Export] public float WidthBoostMax = 1.25f;

	[ExportGroup("Noise / Distortion")]
	[Export] public float NoiseScale = 10.0f;
	[Export] public float NoiseSpeed = 4.0f;
	[Export] public float EdgeSoftness = 0.10f;
	[Export] public float HeatWarp = 0.14f;

	[ExportGroup("Tip Refinement")]
	[Export] public float TipFeatherPx = 20f;
	[Export] public float TipRoundness = 0.85f;

	[ExportGroup("Updraft (Flames rise at the end)")]
	[Export] public float TipRisePx = 28f;
	[Export] public float TipRiseStart01 = 0.60f;
	[Export] public float TipRiseWobble = 0.55f;
	[Export] public float TipRiseAsymmetry = 0.22f;

	[ExportGroup("Fix: prevent top edge line (headroom + uv fade)")]
	[Export(PropertyHint.Range, "-1,240,1")]
	public float HeadroomPx = -1f; // -1 = auto
	[Export(PropertyHint.Range, "0.50,0.95,0.01")]
	public float HeadroomAboveBias01 = 0.78f; // >0.5 = mais espaço acima (bom pro “sobe”)
	[Export(PropertyHint.Range, "0,30,1")]
	public float UvEdgeFadePx = 10f; // suaviza a borda do quad (remove “retângulo”)

	[ExportGroup("Colors")]
	[Export] public Color CoreColor = new(1.00f, 0.95f, 0.65f, 1f);
	[Export] public Color MidColor  = new(1.00f, 0.55f, 0.20f, 1f);
	[Export] public Color GlowColor = new(1.00f, 0.25f, 0.08f, 1f);

	private MeshInstance2D _mesh;
	private QuadMesh _quad;
	private ShaderMaterial _mat;
	private Tween _tw;

	private GpuParticles2D _endFlames;
	private GpuParticles2D _endEmbers;

	private bool _inited;

	private const string ShaderCode = @"
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

// ponta refinada
uniform float tip_uv = 0.07;
uniform float cap_roundness = 0.85;

// updraft
uniform float rise_uv = 0.12;
uniform float rise_start = 0.60;
uniform float rise_wobble = 0.55;
uniform float rise_asym = 0.22;

// suaviza bordas do próprio QUAD pra não aparecer “retângulo”
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

    // arco geral
    uv.y -= uv.x * arc_up;

    float t = TIME * noise_speed;

    // heat shimmer
    float nA = fbm(uv * noise_scale + vec2(-t,  t*0.35));
    float nB = fbm(uv * (noise_scale*1.8) + vec2(t*0.6, -t));
    uv.y += (nA - 0.5) * heat_warp;
    uv.x += (nB - 0.5) * (heat_warp * 0.15);

    // largura base
    float w = mix(start_width, end_width, uv.x) * width_boost;

    // línguas
    float tongue = fbm(vec2(uv.x*noise_scale*0.9 + t*0.9, uv.y*noise_scale*1.3 - t*1.2));
    w *= (0.80 + 0.55 * tongue);

    // updraft (sobe no final)
    float xr = clamp((uv.x - rise_start) / max(1.0 - rise_start, 1e-4), 0.0, 1.0);
    float lift = rise_uv * xr * xr * (0.35 + 0.65 * tongue);

    float wobN = fbm(vec2(uv.x*noise_scale*0.55 + t*0.75, t*0.25));
    float wob = (wobN - 0.5) * rise_wobble * rise_uv * xr;

    float center = 0.5 - lift + wob;

    // tip refine (remove corte reto)
    float tipN = fbm(vec2(uv.y * noise_scale * 1.2, TIME * noise_speed * 0.6));
    float cut = progress - tip_uv * (0.10 + 0.90 * tipN);
    if (uv.x > cut) discard;

    float capT = clamp((cut - uv.x) / max(tip_uv, 1e-4), 0.0, 1.0);

    // taper
    w *= mix(1.0, 0.85 + 0.20 * tipN, 1.0 - capT);

    // dy com assimetria suave
    float dy = abs(uv.y - center);
    float topMask = smoothstep(0.0, 0.03, center - uv.y);
    dy *= (1.0 - rise_asym * xr * topMask);

    float edge = smoothstep(w*0.5, w*0.5 - edge_softness, dy);

    // cap arredondado
    float wHalf = max(w * 0.5, 1e-4);
    vec2 capP = vec2(1.0 - capT, (uv.y - center) / wHalf);
    float capRound = smoothstep(1.0, 0.0, length(capP));
    float tipMask = mix(1.0, capRound, cap_roundness);

    // dissolve
    float dmask = smoothstep(dissolve, dissolve + 0.25, tongue);
    float alpha = edge * dmask * fade * tipMask;

    // UV edge fade: mata QUALQUER resíduo perto das bordas do QUAD (evita “linha/retângulo”)
    float m = max(uv_edge_fade, 1e-4);
    float vfade = smoothstep(0.0, m, uv.y) * smoothstep(0.0, m, 1.0 - uv.y);
    alpha *= vfade;

    // descarta alpha muito baixo (evita linha fantasma)
    if (alpha < 0.002) discard;

    // cor
    float coreMask = smoothstep(0.22, 0.0, dy);
    float midMask  = smoothstep(0.38, 0.12, dy);

    vec3 col = glow_color.rgb;
    col = mix(col, mid_color.rgb, midMask);
    col = mix(col, core_color.rgb, coreMask);

    float heat = smoothstep(0.0, 0.35, uv.x) * (1.0 - smoothstep(0.55, 1.0, uv.x));
    col *= (1.0 + 0.35 * heat);

    COLOR = vec4(col * intensity, alpha);
}
";

	public override void _Ready() { }

	// Compatível com ElementVfxLibrary.SpawnCastProjectile(...)
	public void Play(Vector2 from, Vector2 to, float travelSec = 0.06f)
	{
		EnsureInit();

		float reach = Mathf.Max(0.01f, travelSec);
		AlignBetween(from, to);

		ActivateEndParticles(to);
		StartTween(reach, LingerSeconds, FadeOutSeconds);
	}

	private void EnsureInit()
	{
		if (_inited) return;

		_mesh = GetNodeOrNull<MeshInstance2D>(ConeMeshPath);
		if (_mesh == null)
		{
			_mesh = new MeshInstance2D { Name = "ConeMesh" };
			AddChild(_mesh);
		}

		_endFlames = GetNodeOrNull<GpuParticles2D>(EndFlamesPath);
		_endEmbers = GetNodeOrNull<GpuParticles2D>(EndEmbersPath);

		if (_mesh.Mesh is QuadMesh qm) _quad = (QuadMesh)qm.Duplicate(true);
		else _quad = new QuadMesh();
		_mesh.Mesh = _quad;

		ShaderMaterial baseMat = _mesh.Material as ShaderMaterial;
		_mat = baseMat != null ? (ShaderMaterial)baseMat.Duplicate(true) : new ShaderMaterial();

		_mat.Shader = new Shader { Code = ShaderCode };
		_mesh.Material = _mat;

		_mat.SetShaderParameter("core_color", CoreColor);
		_mat.SetShaderParameter("mid_color", MidColor);
		_mat.SetShaderParameter("glow_color", GlowColor);

		_mat.SetShaderParameter("noise_scale", NoiseScale);
		_mat.SetShaderParameter("noise_speed", NoiseSpeed);
		_mat.SetShaderParameter("edge_softness", EdgeSoftness);
		_mat.SetShaderParameter("heat_warp", HeatWarp);

		_mat.SetShaderParameter("intensity", Intensity);

		_inited = true;
	}

	private void AlignBetween(Vector2 from, Vector2 to)
	{
		Vector2 dir = to - from;
		float len = Mathf.Max(2f, dir.Length());
		Vector2 n = dir / len;

		GlobalPosition = (from + to) * 0.5f;
		GlobalRotation = n.Angle();

		// largura visual (em px) do fogo
		float flameWidthPx = EndWidthPxBase + (len / 100f) * EndWidthPxPer100px;
		flameWidthPx = Mathf.Max(flameWidthPx, StartWidthPx * 2.0f);

		float lengthPx = len + ExtraLengthPx;

		// headroom pra não “bater no teto” quando sobe
		float autoHeadroom = Mathf.Max(0f, TipRisePx * 1.60f + 14f);
		float headroom = (HeadroomPx >= 0f) ? HeadroomPx : autoHeadroom;

		// altura do QUAD (tem espaço sobrando)
		float meshHeightPx = flameWidthPx + headroom;
		_quad.Size = new Vector2(lengthPx, meshHeightPx);

		// bias: joga o quad um pouco pra baixo, criando MAIS espaço acima (ótimo pro updraft)
		float bias = Mathf.Clamp(HeadroomAboveBias01, 0.5f, 0.95f);
		float offsetLocalY = (bias - 0.5f) * headroom;
		_mesh.Position = new Vector2(0f, offsetLocalY);
		_mesh.Rotation = 0f;

		// normaliza larguras pelo meshHeight (assim end_width NÃO vira 1.0 e não aparece retângulo)
		float startRel = Mathf.Clamp(StartWidthPx / meshHeightPx, 0.005f, 1f);
		float endRel   = Mathf.Clamp(flameWidthPx / meshHeightPx, 0.05f, 0.96f); // <- evita “encher tudo”
		_mat.SetShaderParameter("start_width", startRel);
		_mat.SetShaderParameter("end_width", endRel);

		float arcRel = (ArcUpPx <= 0f) ? 0f : (ArcUpPx / meshHeightPx);
		_mat.SetShaderParameter("arc_up", arcRel);

		float tipUv = Mathf.Clamp(TipFeatherPx / Mathf.Max(2f, lengthPx), 0.02f, 0.15f);
		_mat.SetShaderParameter("tip_uv", tipUv);
		_mat.SetShaderParameter("cap_roundness", Mathf.Clamp(TipRoundness, 0f, 1f));

		// updraft normalizado pelo meshHeight
		float riseUv = Mathf.Clamp(TipRisePx / Mathf.Max(2f, meshHeightPx), 0.0f, 0.45f);
		_mat.SetShaderParameter("rise_uv", riseUv);
		_mat.SetShaderParameter("rise_start", Mathf.Clamp(TipRiseStart01, 0f, 0.98f));
		_mat.SetShaderParameter("rise_wobble", Mathf.Max(0f, TipRiseWobble));
		_mat.SetShaderParameter("rise_asym", Mathf.Clamp(TipRiseAsymmetry, 0f, 0.5f));

		// fade nas bordas do quad (em px -> uv)
		float uvFade = Mathf.Clamp(UvEdgeFadePx / Mathf.Max(2f, meshHeightPx), 0.0f, 0.12f);
		_mat.SetShaderParameter("uv_edge_fade", uvFade);
	}

	private void ActivateEndParticles(Vector2 hitPos)
	{
		if (_endFlames != null)
		{
			_endFlames.GlobalPosition = hitPos;
			_endFlames.GlobalRotation = GlobalRotation;
			_endFlames.Emitting = true;

			if (_endFlames.ProcessMaterial is ParticleProcessMaterial pm)
			{
				pm.Direction = new Vector3(1f, -0.35f, 0f);
				pm.Spread = 65f;
				pm.InitialVelocityMin = 90f;
				pm.InitialVelocityMax = 220f;
				pm.Gravity = new Vector3(0f, -260f, 0f);
			}
		}

		if (_endEmbers != null)
		{
			_endEmbers.GlobalPosition = hitPos;
			_endEmbers.GlobalRotation = GlobalRotation;
			_endEmbers.Emitting = true;

			if (_endEmbers.ProcessMaterial is ParticleProcessMaterial pm)
			{
				pm.Direction = new Vector3(0.7f, -0.55f, 0f);
				pm.Spread = 140f;
				pm.InitialVelocityMin = 80f;
				pm.InitialVelocityMax = 280f;
				pm.Gravity = new Vector3(0f, 520f, 0f);
			}
		}
	}

	private void StartTween(float reach, float linger, float fadeOut)
	{
		if (_tw != null && GodotObject.IsInstanceValid(_tw)) _tw.Kill();

		_mat.SetShaderParameter("progress", 0f);
		_mat.SetShaderParameter("fade", 1f);
		_mat.SetShaderParameter("dissolve", 0f);
		_mat.SetShaderParameter("width_boost", 0.75f);

		_tw = CreateTween();

		_tw.TweenMethod(Callable.From<float>(v => _mat.SetShaderParameter("progress", v)), 0f, 1f, reach)
		   .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

		_tw.Parallel().TweenMethod(Callable.From<float>(v => _mat.SetShaderParameter("width_boost", v)),
			0.75f, WidthBoostMax, Mathf.Min(0.05f, reach))
		   .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

		if (linger > 0.001f)
			_tw.TweenInterval(linger);

		float fo = Mathf.Max(0.01f, fadeOut);

		_tw.TweenMethod(Callable.From<float>(v => _mat.SetShaderParameter("dissolve", v)), 0f, 1f, fo)
		   .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

		_tw.Parallel().TweenMethod(Callable.From<float>(v => _mat.SetShaderParameter("fade", v)), 1f, 0f, fo)
		   .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);

		if (QueueFreeOnEnd)
			_tw.Finished += () => { if (GodotObject.IsInstanceValid(this)) QueueFree(); };
	}
}
