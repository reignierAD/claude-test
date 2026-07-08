using UnityEngine;

/// <summary>
/// Global audio: one looping music source + one SFX source for PlayOneShot.
/// Volumes/mutes are read from PlayerPrefs (set in the Settings popup) and the
/// clips come from GameSettings (GameConfig.S). Self-creates on first use and
/// survives scene loads, so a single instance drives audio for the whole game.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    AudioSource _music;
    AudioSource _sfx;
    float _musicVol = 0.7f;
    float _sfxVol = 0.8f;
    bool _musicMute;
    bool _sfxMute;

    public static AudioManager Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("AudioManager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<AudioManager>();
            Instance.Init();
        }
        return Instance;
    }

    void Init()
    {
        _music = gameObject.AddComponent<AudioSource>();
        _music.loop = true;
        _music.playOnAwake = false;

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;

        LoadPrefs();
        PlayMusic(GameConfig.S.backgroundMusic);
    }

    /// <summary>Re-reads the saved volume/mute settings and applies them.</summary>
    public void LoadPrefs()
    {
        _sfxVol = PlayerPrefs.GetFloat("sfx_vol", 0.8f);
        _musicVol = PlayerPrefs.GetFloat("music_vol", 0.7f);
        _sfxMute = PlayerPrefs.GetInt("sfx_mute", 0) == 1;
        _musicMute = PlayerPrefs.GetInt("music_mute", 0) == 1;
        ApplyMusicVolume();
    }

    /// <summary>Live preview while the SFX slider/mute is dragged in Settings.</summary>
    public void SetSfx(float vol, bool mute)
    {
        _sfxVol = vol;
        _sfxMute = mute;
    }

    /// <summary>Live preview while the Music slider/mute is dragged in Settings.</summary>
    public void SetMusic(float vol, bool mute)
    {
        _musicVol = vol;
        _musicMute = mute;
        ApplyMusicVolume();
    }

    void ApplyMusicVolume()
    {
        if (_music != null) _music.volume = _musicMute ? 0f : _musicVol;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (_music == null) return;
        if (clip == null)
        {
            _music.Stop();
            return;
        }
        if (_music.clip == clip && _music.isPlaying) return;
        _music.clip = clip;
        ApplyMusicVolume();
        _music.Play();
    }

    public void PlaySfx(AudioClip clip)
    {
        if (_sfx == null || clip == null || _sfxMute || _sfxVol <= 0f) return;
        _sfx.PlayOneShot(clip, _sfxVol);
    }
}
