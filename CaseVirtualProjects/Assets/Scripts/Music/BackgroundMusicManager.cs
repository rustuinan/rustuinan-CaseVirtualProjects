using System.Collections.Generic;
using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance;

    [Header("Audio")]
    public AudioSource audioSource;
    public List<AudioClip> musicClips = new List<AudioClip>();

    [Range(0f, 1f)]
    public float musicVolume = 0.5f;

    private int lastClipIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = musicVolume;
    }

    private void Start()
    {
        PlayRandomTrack();
    }

    private void Update()
    {
        if (!audioSource.isPlaying && musicClips.Count > 0)
        {
            PlayRandomTrack();
        }

        if (audioSource.volume != musicVolume)
        {
            audioSource.volume = musicVolume;
        }
    }

    private void PlayRandomTrack()
    {
        if (musicClips == null || musicClips.Count == 0)
            return;

        int index = GetRandomIndex();

        AudioClip clip = musicClips[index];
        lastClipIndex = index;

        audioSource.clip = clip;
        audioSource.Play();

        Debug.Log($"[BackgroundMusicManager] Playing track: {clip.name}");
    }

    private int GetRandomIndex()
    {
        if (musicClips.Count == 1)
            return 0;

        int index;
        do
        {
            index = Random.Range(0, musicClips.Count);
        }
        while (index == lastClipIndex);

        return index;
    }
}
