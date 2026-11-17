using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioClip music;
    [SerializeField] private AudioClip chompA, chompB, power, hit, ghostEaten, win, click;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.3f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    private AudioSource musicSource;
    private AudioSource sfxSource;
    private bool alternate;
    private float lastChomp;

    private void Awake()
    {
        Instance = this;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.clip = music;
        musicSource.loop = true;
        musicSource.volume = musicVolume;
        musicSource.playOnAwake = false;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    private void Start()
    {
        if (music != null) musicSource.Play();
    }

    private void Sfx(AudioClip clip, float volume = 1f)
    {
        if (clip != null) sfxSource.PlayOneShot(clip, sfxVolume * volume);
    }

    public static void Chomp()
    {
        if (Instance == null || Time.time - Instance.lastChomp < 0.05f) return;
        Instance.lastChomp = Time.time;
        Instance.alternate = !Instance.alternate;
        Instance.Sfx(Instance.alternate ? Instance.chompA : Instance.chompB, 0.6f);
    }

    public static void Power() { if (Instance != null) Instance.Sfx(Instance.power); }
    public static void Hit() { if (Instance != null) Instance.Sfx(Instance.hit); }
    public static void GhostEaten() { if (Instance != null) Instance.Sfx(Instance.ghostEaten); }
    public static void Win() { if (Instance != null) Instance.Sfx(Instance.win); }
    public static void Click() { if (Instance != null) Instance.Sfx(Instance.click); }
}