using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Music : MonoBehaviour
{
    [SerializeField] private AudioClip[] menuClips;
    [SerializeField] private AudioClip[] musicClips1;
    [SerializeField] private int level;

    private AudioSource source;
    private AudioSource secondarySource;
    int musicType;

    void Start()
    {
        source = GetComponent<AudioSource>();
        secondarySource = gameObject.AddComponent<AudioSource>();
        PlayRandomMusic();
    }

    void Update()
    {
        int lastMusicType = musicType;
        if (!Manager.Instance.game) musicType = 0;
        else
        {
            if (Manager.Instance.music)
            {
                musicType = 2;
            }
            else
            {
                musicType = 1;
            }
        }
        
        if (!source.isPlaying && musicType == 0)
        {
            PlayRandomMusic();
        }
        if (musicType != 0 && (lastMusicType != musicType || !source.isPlaying))
        {
            SwitchMusic(musicClips1[musicType - 1], lastMusicType != 0);
        }
    }

    public void SwitchMusic(AudioClip newClip, bool safe = true)
    {
        float currentTime = source.time;
        if (!safe) currentTime = 0;
        currentTime = Mathf.Clamp(currentTime, 0, newClip.length);
        
        secondarySource.clip = source.clip;
        secondarySource.time = currentTime;
        source.clip = newClip;
        source.time = currentTime;
        
        secondarySource.Play();
        source.Play();
        StartCoroutine(FadeTransition());
    }

    private IEnumerator FadeTransition()
    {
        float time = 0f;
        while (time < 0.5f)
        {
            time += Time.deltaTime;
            float t = time / 0.5f;
            secondarySource.volume = Mathf.Lerp(1, 0, t);
            source.volume = Mathf.Lerp(0, 1, t);
            yield return null;
        }
        secondarySource.Stop();
    }

    void PlayRandomMusic()
    {
        if (menuClips.Length == 0)
            return;

        source.volume = Random.Range(0.9f, 1);

        AudioClip clip = menuClips[Random.Range(0, menuClips.Length)];
        source.clip = clip;
        source.Play();
    }
}
