using Godot;

public partial class AudioManager : Node
{
	[ExportCategory("Defaults")]
	[Export] public AudioStream DefaultMenuMusic;
	[Export] public AudioStream DefaultBattleMusic;

	private AudioStreamPlayer _music;
	private AudioStreamPlayer _sfx;

	public override void _Ready()
	{
		// Cria players (não precisa colocar em cena)
		_music = new AudioStreamPlayer();
		_music.Name = "MusicPlayer";
		_music.Bus = "Music";
		_music.Autoplay = false;
		AddChild(_music);

		_sfx = new AudioStreamPlayer();
		_sfx.Name = "SfxPlayer";
		_sfx.Bus = "SFX";
		_sfx.Autoplay = false;
		AddChild(_sfx);

		// Aplica volume inicial dos settings (se existir)
		ApplyVolumesFromSettings();
	}

	public void ApplyVolumesFromSettings()
	{
		// Ajuste aqui para o nome real do seu autoload de settings
		// Vou assumir que você tem SettingsService com MusicVolume e SfxVolume (0..1).
		if (GetNodeOrNull<Node>("/root/SettingsService") is Node)
		{
			// se você tiver métodos públicos no SettingsService, melhor ainda.
			// por enquanto deixo seguro: só não crasha.
		}
	}

	public void PlayMusic(AudioStream stream, bool restartIfSame = false)
	{
		if (stream == null) return;

		if (_music.Stream == stream && _music.Playing && !restartIfSame)
			return;

		_music.Stream = stream;
		_music.Play();
	}

	public void StopMusic()
	{
		_music.Stop();
	}

	public void PlaySfx(AudioStream stream, float pitchMin = 1f, float pitchMax = 1f)
	{
		if (stream == null) return;

		_sfx.Stream = stream;
		_sfx.PitchScale = (pitchMin == pitchMax) ? pitchMin : (float)GD.RandRange(pitchMin, pitchMax);
		_sfx.Play();
	}

	// Conveniência
	public void PlayMenuMusic()
	{
		if (DefaultMenuMusic != null)
			PlayMusic(DefaultMenuMusic);
	}

	public void PlayBattleMusic()
	{
		if (DefaultBattleMusic != null)
			PlayMusic(DefaultBattleMusic);
	}
}
