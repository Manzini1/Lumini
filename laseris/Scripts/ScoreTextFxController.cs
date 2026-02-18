using Godot;

public partial class ScoreTextFxController : Control
{
	[Export] public NodePath ViewportPath = "TextViewport";
	[Export] public NodePath MaskLabelPath = "TextViewport/MaskLabel";
	[Export] public NodePath ShinePath = "Shine";
	[Export] public NodePath BaseLabelPath = "BaseLabel"; // opcional

	private SubViewport _vp;
	private Label _mask;
	private TextureRect _shine;
	private Label _base;

	public override void _Ready()
	{
		_vp = GetNodeOrNull<SubViewport>(ViewportPath);
		_mask = GetNodeOrNull<Label>(MaskLabelPath);
		_shine = GetNodeOrNull<TextureRect>(ShinePath);
		_base = GetNodeOrNull<Label>(BaseLabelPath);

		// liga a textura do viewport no Shine
		if (_vp != null && _shine != null)
			_shine.Texture = _vp.GetTexture();

		// importante: viewport transparente
		if (_vp != null)
			_vp.TransparentBg = true;
	}

	public void SetText(string text)
	{
		if (_mask != null) _mask.Text = text;
		if (_base != null) _base.Text = text;
	}

	public void SetLeading(bool leading)
	{
		// o shader está no Shine (TextureRect)
		if (_shine?.Material is ShaderMaterial mat)
			mat.SetShaderParameter("lead_boost", leading ? 1.0f : 0.0f);
	}
}
