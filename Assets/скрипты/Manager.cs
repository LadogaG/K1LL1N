using UnityStandardAssets.ImageEffects;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;

public class Manager : MonoBehaviour
{
    public static Manager Instance { get; private set; }
    public float gameTime = 0f;
    public float levelDamage = 0f;
    public float totalDamage = 0f;
    public float kills = 0f;
    public string playerName = "Player";
    public float dps;
    public float totalDps;
    public bool pause = false;
    public bool game = false;
    public bool music = false;
    [HideInInspector] public Rigidbody rb;

    [Header("UI Settings")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    [SerializeField] GameObject damageAim;
    [SerializeField] GameObject killAim;
    [SerializeField] GameObject health;

    [Header("Particles Settings")]
    public ParticleSystem sparks;
    public ParticleSystem frictionSparks;
    public ParticleSystem landSparks;
    public ParticleSystem stepParticle;
    public ParticleSystem stepSparks;
    public ParticleSystem grassSparks;
    public ParticleSystem metalSparks;

    [Header("Sound Settings")]
    [SerializeField] AudioClip clickSound;
    [SerializeField] AudioClip startupSound;
    [SerializeField] AudioClip panelSound;
    public AudioClip damageSound;
    [SerializeField] AudioClip killSound;
    public AudioClip fallSound;
    public AudioClip landSound;
    public AudioClip hitSound;
    public AudioClip grassSound;
    public AudioClip metalSound;
    public AudioClip sparksSound;
    AudioSource source;
    BloomAndFlares bloomAndFlares;
    float dpscd;

    void Awake()
    {
        Instance = this;
        Load();
        gameObject.name = playerName;

        source = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        Rigidbody[] rbs = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            if (rb.gameObject.GetComponent<Physic>() == null && rb.gameObject.name != "физика") rb.gameObject.AddComponent<Physic>();
        }
        Panel(health, 2);
        bloomAndFlares = Camera.main.GetComponent<BloomAndFlares>();
    }

    void Start() => Sound(startupSound);

    void Update()
    {
        gameTime += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GamePause();
        }

        Rigidbody[] rbs = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            if (rb.gameObject.GetComponent<Physic>() == null && rb.gameObject.name != "физика") rb.gameObject.AddComponent<Physic>();
        }

        totalDps = levelDamage/Time.time;
    }

    void FixedUpdate()
    {
        if (dpscd > 0) dpscd -= 0.02f;
        if (dpscd <= 0) dps = 0;
    }

    public void GamePause()
    {
        if (!settingsPanel.activeSelf)
        {
            pause = !pause;
            Panel(pausePanel, 0.1f);
            Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = pause;
            Time.timeScale = pause ? 0 : 1;
        }
        else
        {
            Panel(settingsPanel, 0.1f);
        }
    }

    public void Damage(float damage, AudioSource newAudioSource = null)
    {
        dps += damage;
        levelDamage += damage;
        dpscd = 1;
        Sound(damageSound, Random.Range(0.8f, 1) * Mathf.Min(damage / 20, 1));
        Sound(damageSound, Random.Range(0.8f, 1) * Mathf.Min(damage / 10, 1), newAudioSource);
        Popup(damageAim, 0.25f);
        totalDamage += damage;
    }

    public void Kill(float damage, AudioSource newAudioSource = null)
    {
        dps += damage;
        levelDamage += damage;
        dpscd = 1;
        Sound(killSound, 0.5f);
        Sound(killSound, 1, newAudioSource);
        Popup(killAim, 0.25f);
        Flash();
        Pause(0.2f);
        kills++;
    }

    public void Shrink(Transform target, float timeBeforeShrink = 0, float shrinkDuration = 0) => StartCoroutine(ShrinkIE(target, timeBeforeShrink, shrinkDuration));
    IEnumerator ShrinkIE(Transform target, float timeBeforeShrink = 0, float shrinkDuration = 0)
    {
        if (timeBeforeShrink != 0) yield return new WaitForSeconds(timeBeforeShrink);

        if (shrinkDuration != 0)
        {            
            Vector3 originalScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < shrinkDuration)
            {
                float t = elapsed / shrinkDuration;
                target.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        Destroy(target.gameObject);
    }

    public void Pause(float duration = 0.5f) => StartCoroutine(PauseIE(duration));
    IEnumerator PauseIE(float duration = 1f)
    {
        if (!pause) Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);
        if (!pause) Time.timeScale = 1;
    }

    public void Flash(float flash = 0) => bloomAndFlares.bloomThreshold = flash;

    void OnApplicationQuit()
    {
        PlayerPrefs.SetFloat("GameTime", gameTime);
        PlayerPrefs.SetFloat("TotalDamage", totalDamage);
        PlayerPrefs.SetFloat("Kills", kills);
        PlayerPrefs.Save();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) GamePause();
    }

    void Load()
    {
        gameTime = PlayerPrefs.GetFloat("GameTime");
        totalDamage = PlayerPrefs.GetFloat("TotalDamage");
        kills = PlayerPrefs.GetFloat("Kills");
    }

    public void Panel(GameObject panel, float duration = 0.5f) => StartCoroutine(PanelIE(panel, duration));
    IEnumerator PanelIE(GameObject panel, float duration = 0.5f)
    {
        Sound(panelSound);
        CanvasGroup cg = panel.GetComponent<CanvasGroup>();
        bool show = !panel.activeSelf;
        cg.alpha = show ? 0f : 1f;
        panel.SetActive(show);

        RectTransform rt = panel.GetComponent<RectTransform>();
        Vector2 rtsd = rt.sizeDelta;

        GameObject copy = Instantiate(panel, panel.transform.parent);
        copy.SetActive(true);
        RectTransform crt = copy.GetComponent<RectTransform>();
        CanvasGroup ccg = copy.GetComponent<CanvasGroup>();
        ccg.alpha = show ? 0.5f : 1f;
        rt.sizeDelta = rtsd;
        if (show)
        {
            crt.sizeDelta = new Vector2(rtsd.x / 5f, rtsd.y / 5f);
            yield return new WaitForSecondsRealtime(duration/10f);
            crt.sizeDelta = new Vector2(rtsd.x / 5f, rtsd.y);
            yield return new WaitForSecondsRealtime(duration/10f);
            crt.sizeDelta = new Vector2(rtsd.x, rtsd.y / 5f);
            yield return new WaitForSecondsRealtime(duration/10f);
            crt.sizeDelta = rtsd;
        }
        else
        {
            crt.sizeDelta = new Vector2(rtsd.x, rtsd.y / 5f);
            yield return new WaitForSecondsRealtime(duration/10f);
            crt.sizeDelta = new Vector2(rtsd.x / 5f, rtsd.y);
            yield return new WaitForSecondsRealtime(duration/10f);
            crt.sizeDelta = new Vector2(rtsd.x / 5f, rtsd.y / 5f);
            yield return new WaitForSecondsRealtime(duration/10f);
        }
        if (show) Destroy(copy);
        cg.alpha = show ? 0.5f : 1f;

        CanvasGroup targetCg = show ? cg : ccg;
        float elapsed = 0f;
        float startAlpha = targetCg.alpha;
        float targetAlpha = show ? 1f : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            targetCg.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return new WaitForSecondsRealtime(0.001f);
        }
        targetCg.alpha = targetAlpha;
        if (!show) { Destroy(copy); }
    }

    public void Popup(GameObject panel, float duration = 1f) => StartCoroutine(PopupIE(panel, duration));
    IEnumerator PopupIE(GameObject panel, float duration = 1f)
    {
        CanvasGroup cg = panel.GetComponent<CanvasGroup>() ?? panel.AddComponent<CanvasGroup>();
        panel.transform.localScale = Vector3.one;

        panel.SetActive(true);
        cg.alpha = 1f;

        float elapsed = 0f;
        float startAlpha = cg.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.fixedDeltaTime;
            float t = elapsed / duration;
            cg.alpha = Mathf.Lerp(startAlpha, 0, t);
            panel.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 1.5f, t);
            yield return new WaitForSecondsRealtime(0.001f);
        }

        cg.alpha = 0;
        panel.transform.localScale = Vector3.one;
        panel.SetActive(false);
    }

    public void Settings()
    {
        Panel(settingsPanel, 0.2f);
    }

    public void Click() => Sound(clickSound);

    public void Sound(AudioClip audio, float volume = 1, AudioSource newAudioSource = null)
    {
        if (newAudioSource == null) newAudioSource = source;
        newAudioSource.volume = Random.Range(volume - volume / 10f, volume + volume / 10f);
        newAudioSource.pitch = Random.Range(0.8f, 1.2f);
        newAudioSource.PlayOneShot(audio);
    }
}