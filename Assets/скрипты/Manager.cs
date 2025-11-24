using UnityStandardAssets.ImageEffects;
using UnityEngine.SceneManagement;
using DamageNumbersPro.Demo;
using System.Collections;
using DamageNumbersPro;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class Manager : MonoBehaviour
{
    public static Manager Instance { get; private set; }
    [SerializeField] bool kill2win;
    public float totalTime = 0f;
    public float levelTime = 0f;
    public float levelDamage = 0f;
    public float totalDamage = 0f;
    public float totalKills = 0f;
    public float levelKills = 0f;
    int enemyAmount = 0;
    public string playerName = "Player";
    public float dps;
    public float totalDps;
    public bool pause = false;
    public bool game = false;
    public bool music = false;
    public bool win;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public GameObject player;

    [Header("Weapon")]
    [HideInInspector] float lineFadeTime = 0.5f;
    public GameObject bullet;
    public GameObject crit;
    public GameObject rocket;

    [Header("Enemy")]
    [SerializeField] DamageNumber textPrefab;
    [SerializeField] DamageNumber critTextPrefab;
    DNP_PrefabSettings settings;
    public Transform healthBarPrefab;
    [SerializeField] public GameObject BloodAttach;
    [SerializeField] public GameObject[] BloodFX;

    [Header("UI Settings")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    [SerializeField] GameObject stageClear;
    public GameObject toWinPanel;
    public GameObject kill2winPanel;
    public TextMeshProUGUI kill2winText;
    [SerializeField] GameObject winPanel;
    [SerializeField] GameObject timePanel;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] GameObject damagePanel;
    [SerializeField] TextMeshProUGUI damageText;
    [SerializeField] GameObject killsPanel;
    [SerializeField] TextMeshProUGUI killsText;
    [SerializeField] GameObject continuePanel;
    [SerializeField] GameObject damageAim;
    public GameObject killAim;
    [SerializeField] GameObject health;
    [SerializeField] TextMeshProUGUI cheatText;
    private string input = "";
    private bool isRecording = false;

    [Header("Particles Settings")]
    public ParticleSystem sparks;
    public ParticleSystem small;
    public ParticleSystem explosion;
    public ParticleSystem friction;
    public ParticleSystem land;
    public ParticleSystem step;
    public ParticleSystem walk;
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
        enemyAmount = GameObject.FindGameObjectsWithTag("Enemy").Length;
        Load();
        gameObject.name = playerName;

        source = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        player = gameObject;
        Rigidbody[] rbs = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            if (rb.gameObject.GetComponent<Physic>() == null && rb.tag != "PhysicIgnore") rb.gameObject.AddComponent<Physic>();
        }
        Panel(health, 2);
        bloomAndFlares = Camera.main.GetComponent<BloomAndFlares>();

        if (kill2win) Panel(kill2winPanel);
    }

    void Start() => Sound(startupSound);

    void Update()
    {
        totalTime += Time.deltaTime;
        levelTime += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GamePause();
        }
        if (Input.GetMouseButtonUp(0) && win)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);                                                        
        }

        Rigidbody[] rbs = FindObjectsOfType<Rigidbody>();
        foreach (Rigidbody rb in rbs)
        {
            if (rb.gameObject.GetComponent<Physic>() == null && rb.tag != "PhysicIgnore") rb.gameObject.AddComponent<Physic>();
        }

        totalDps = levelDamage / Time.time;

        Cheat();
    }
    
    void Cheat()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (!isRecording) isRecording = true;
            else
            {
                isRecording = false;
                SceneManager.LoadScene(int.Parse(input));
            }

            Panel(cheatText.transform.parent.gameObject);
        }

        if (isRecording)
        {
            string newInput = Input.inputString;
            foreach (char c in newInput)
            {
                if (c == '\b')
                {
                    if (input.Length > 0)
                        input = input.Substring(0, input.Length - 1);
                }
                else
                {
                    input += c;
                }
            }
            cheatText.text = input;
        }
        else
        {
            input = "";
            cheatText.text = "";
        }
    }

    void FixedUpdate()
    {
        if (dpscd > 0) dpscd -= 0.02f;
        if (dpscd <= 0) dps = 0;
    }

    public GameObject Fire(bool? isPlayer, GameObject projectile, Vector3 position, Quaternion rotation, Vector3 direction, float speed = 15, float spread = 0, bool isCrit = false) //List<EffectType> effectTypes = null)
    {
        //activeEffects = effectTypes;
        GameObject p = Instantiate(projectile, position, rotation);
        if (p.GetComponent<Rigidbody>() == null) p.AddComponent<Rigidbody>();
        Projectile pc = p.GetComponent<Projectile>();
        if (pc == null) pc = p.AddComponent<Bullet>();
        if (!pc.enabled) pc.enabled = true;
        p.GetComponent<Collider>().isTrigger = false;
        pc.Fire(isPlayer, direction, speed, spread, null, isCrit);
        return p;
    }

    public LineRenderer GetLineRenderer(Color color)
    {
        GameObject lrObj = new GameObject("LineRenderer");
        lrObj.transform.SetParent(transform);
        LineRenderer newLr = lrObj.AddComponent<LineRenderer>();
        newLr.startWidth = 0.25f;
        newLr.endWidth = 0.25f;
        newLr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
        newLr.startColor = color;
        newLr.endColor = color;
        newLr.positionCount = 2;
        newLr.enabled = true;
        return newLr;
    }

    public void ShowLineRenderer(Vector3 start, Vector3 end, LineRenderer lr, ParticleSystem particle = null) => StartCoroutine(ShowLineRendererIE(start, end, lr, particle));
    public IEnumerator ShowLineRendererIE(Vector3 start, Vector3 end, LineRenderer lr, ParticleSystem particle = null)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        float elapsed = 0f;
        if (particle != null) Particle(particle.gameObject, end, Quaternion.LookRotation(start - end), 5);
        while (elapsed < lineFadeTime)
        {
            elapsed += Time.deltaTime;
            lr.startWidth /= ((elapsed + 1) / lineFadeTime) + 1;
            lr.endWidth /= ((elapsed + 1) / lineFadeTime) + 1;
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    public void Particle(GameObject particle, Vector3 position, Quaternion rotation, float cooldown = 1)
    {
        ParticleSystem sparksParticles = Instantiate(particle, position, rotation).GetComponent<ParticleSystem>();
        sparksParticles.Play();
        Destroy(sparksParticles.gameObject, cooldown);
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

    public void Damage(float damage, Vector3 target, Transform enemy, bool crit = false, AudioSource newAudioSource = null)
    {
        dps += damage;
        levelDamage += damage;
        dpscd = 1;
        Sound(damageSound, Random.Range(0.8f, 1) * Mathf.Min(damage / 20, 1));
        Sound(damageSound, Random.Range(0.8f, 1) * Mathf.Min(damage / 10, 1), newAudioSource);
        Popup(damageAim, 0.25f);
        DNPSet(target, damage, enemy, crit);
        totalDamage += damage;
    }

    public void Kill(float damage, Vector3 target, Transform enemy, bool crit = false, AudioSource newAudioSource = null)
    {
        dps += damage;
        levelDamage += damage;
        dpscd = 1;
        Sound(killSound, 0.5f);
        Sound(killSound, 1, newAudioSource);
        Popup(killAim, 0.25f);
        Flash(0.3f);
        DNPSet(target, damage, enemy, crit);

        if (levelKills == enemyAmount)
        {
            Popup(killAim, 1);
            Flash();
            Pause(0.2f);
            Popup(stageClear, 1);

            if (kill2win) Win();
        }
        if (kill2win) kill2winText.text = $"GOAL: KILL EVERYONE ({levelKills}/{enemyAmount})(%{levelKills / enemyAmount * 100})";
    }

    protected void DNPSet(Vector3 target, float number, Transform enemy, bool crit = false)
    {
        DamageNumber pref = crit ? critTextPrefab : textPrefab;
        settings = pref.gameObject.GetComponent<DNP_PrefabSettings>();
        if (pref.digitSettings.decimals == 0)
        {
            number = Mathf.Floor(number);
        }
        DamageNumber newDamageNumber = pref.Spawn(target, number);
        settings.Apply(newDamageNumber);
        newDamageNumber.enableFollowing = true;
        newDamageNumber.followedTarget = enemy;
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
        PlayerPrefs.SetFloat("GameTime", totalTime);
        PlayerPrefs.SetFloat("TotalDamage", totalDamage);
        PlayerPrefs.SetFloat("Kills", totalKills);
        PlayerPrefs.Save();
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus) GamePause();
    }

    void Load()
    {
        totalTime = PlayerPrefs.GetFloat("GameTime");
        totalDamage = PlayerPrefs.GetFloat("TotalDamage");
        totalKills = PlayerPrefs.GetFloat("Kills");
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


    public void Win() => StartCoroutine(WinIE());
    IEnumerator WinIE()
    {
        win = true;
        Flash();
        Time.timeScale = 0;

        Panel(winPanel);

        yield return new WaitForSecondsRealtime(1);
        Panel(timePanel, 0.1f);
        timeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)(levelTime / 3600), (int)(levelTime % 3600 / 60), (int)(levelTime % 60));

        yield return new WaitForSecondsRealtime(0.5f);
        Panel(damagePanel, 0.1f);
        damageText.text = levelDamage.ToString();

        yield return new WaitForSecondsRealtime(0.5f);
        Panel(killsPanel, 0.1f);
        killsText.text = $"{levelKills}/{enemyAmount} (%{levelKills / enemyAmount * 100})";

        yield return new WaitForSecondsRealtime(0.5f);
        Panel(continuePanel);

        while (Time.timeScale < 1) Time.timeScale += 0.1f;
    }
}