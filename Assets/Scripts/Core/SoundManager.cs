using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource soundSource;  // SFX
    [SerializeField] private AudioSource musicSource;  // Müzik (child)

    private void Awake()
    {
        // Kaynaklar atanmadıysa otomatik bul
        if (soundSource == null) soundSource = GetComponent<AudioSource>();
        if (musicSource == null && transform.childCount > 0)
            musicSource = transform.GetChild(0).GetComponent<AudioSource>();

        // Singleton ayarı
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Başlangıç ses ayarları
        ChangeMusicVolume(0);
        ChangeSoundVolume(0);
    }

    // ---------------------------- SFX ----------------------------
    public void PlaySound(AudioClip _sound)
    {
        if (_sound == null || soundSource == null)
        {
            Debug.LogWarning("[SoundManager] PlaySound: clip veya source null!");
            return;
        }

        soundSource.PlayOneShot(_sound);
    }

    // ---------------------------- MUSIC ----------------------------
    public void PlayMusic(AudioClip _music, bool loop = true)
    {
        if (_music == null || musicSource == null)
        {
            Debug.LogWarning("[SoundManager] PlayMusic: clip veya source null!");
            return;
        }

        // Aynı müzik tekrar çalınıyorsa yeniden başlatma
        if (musicSource.clip == _music && musicSource.isPlaying)
            return;

        musicSource.clip = _music;
        musicSource.loop = loop;
        musicSource.Play();

        Debug.Log($"[SoundManager] Music started: {_music.name}");
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
            musicSource.Stop();
    }

    // ---------------------------- VOLUME ----------------------------
    public void ChangeSoundVolume(float _change)
    {
        ChangeSourceVolume(1f, "soundVolume", _change, soundSource);
    }

    public void ChangeMusicVolume(float _change)
    {
        ChangeSourceVolume(0.3f, "musicVolume", _change, musicSource);
    }

    private void ChangeSourceVolume(float baseVolume, string volumeName, float change, AudioSource source)
    {
        if (source == null) return;

        float currentVolume = PlayerPrefs.GetFloat(volumeName, 1);
        currentVolume += change;

        if (currentVolume > 1)
            currentVolume = 0;
        else if (currentVolume < 0)
            currentVolume = 1;

        float finalVolume = currentVolume * baseVolume;
        source.volume = finalVolume;
        PlayerPrefs.SetFloat(volumeName, currentVolume);
    }
}
