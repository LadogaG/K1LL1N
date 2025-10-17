using UnityStandardAssets.ImageEffects;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class Health : MonoBehaviour
{
    [SerializeField] Gradient healthGradient;
    [SerializeField] Gradient staminaGradient;

    public GameObject head;
    public GameObject body;
    public GameObject rArm;
    public GameObject lArm;
    public GameObject rLeg;
    public GameObject lLeg;

    // Переменные для головы (основное здоровье)
    public float healthSum = 100f;
    public float health = 100f;
    float maxHealth = 100f;
    [SerializeField] Image healthBar;
    [SerializeField] TextMeshProUGUI healthText;

    // Переменные для тела
    public float bodyHealth = 100f;
    float maxbodyHealth = 100f;
    [SerializeField] Image bodyHealthBar;
    [SerializeField] TextMeshProUGUI bodyHealthText;

    // Переменные для правой руки
    public float rArmHealth = 100f;
    float maxrArmHealth = 100f;
    [SerializeField] Image rArmHealthBar;
    [SerializeField] TextMeshProUGUI rArmHealthText;

    // Переменные для левой руки
    public float lArmHealth = 100f;
    float maxlArmHealth = 100f;
    [SerializeField] Image lArmHealthBar;
    [SerializeField] TextMeshProUGUI lArmHealthText;

    // Переменные для правой ноги
    public float rLegHealth = 100f;
    float maxrLegHealth = 100f;
    [SerializeField] Image rLegHealthBar;
    [SerializeField] TextMeshProUGUI rLegHealthText;

    // Переменные для левой ноги
    public float lLegHealth = 100f;
    float maxlLegHealth = 100f;
    [SerializeField] Image lLegHealthBar;
    [SerializeField] TextMeshProUGUI lLegHealthText;

    // Переменные для энергии
    public float stamina = 100f;
    float maxStamina = 100f;
    [SerializeField] Image staminaBar;
    [SerializeField] TextMeshProUGUI staminaText;

    public float regen = 0.1f;
    public Image damagePanel;
    [SerializeField] GameObject deathPanel;
    [SerializeField] GameObject warningPanel;
    Dictionary<GameObject, Image> barMap = new Dictionary<GameObject, Image>();
    Dictionary<GameObject, Image> barCopies = new Dictionary<GameObject, Image>();
    Dictionary<GameObject, Coroutine> copyCoroutines = new Dictionary<GameObject, Coroutine>();
    Rigidbody rb;
	Movement movement;
	NoiseAndGrain noiseAndGrain;
	MotionBlur motionBlur;

    [Header("Sound Settings")]
    [SerializeField] AudioClip damageSound;
    [SerializeField] AudioClip healSound;
    [SerializeField] AudioClip pickupSound;
    [SerializeField] AudioClip breakSound;
    [SerializeField] AudioClip warningSound;
    [SerializeField] AudioClip[] deathSound;
    bool canDamage;
    float multiplier = 1;
    bool dead = false;

    void Awake()
    {
        movement = GetComponent<Movement>();
        rb = GetComponent<Rigidbody>();
        noiseAndGrain = Camera.main.GetComponent<NoiseAndGrain>();
        motionBlur = Camera.main.GetComponent<MotionBlur>();
        InvokeRepeating("CanDamage", 0, 0.5f);
        barMap[head] = healthBar;
        barMap[body] = bodyHealthBar;
        barMap[rArm] = rArmHealthBar;
        barMap[lArm] = lArmHealthBar;
        barMap[rLeg] = rLegHealthBar;
        barMap[lLeg] = lLegHealthBar;

        UpdateHealthBar();
        UpdateStaminaBar();

        maxHealth = health;
        maxbodyHealth = bodyHealth;
        maxrArmHealth = rArmHealth;
        maxlArmHealth = lArmHealth;
        maxrLegHealth = rLegHealth;
        maxlLegHealth = lLegHealth;
        maxStamina = stamina;
        InvokeRepeating("Regen", 0, 1);
    }

    void Update()
    {
        healthSum = (health + bodyHealth + rArmHealth + lArmHealth + rLegHealth + lLegHealth) / 6f;

        noiseAndGrain.intensityMultiplier = -healthSum / 25 + 4 * multiplier;
        motionBlur.blurAmount = -healthSum / 200 + 0.5f;

        GetComponent<CapsuleCollider>().height = (rLegHealth + lLegHealth) / 100;
        PhysicMaterial rbMat = GetComponent<Collider>().material;
        rbMat.dynamicFriction = (-rLegHealth + -lLegHealth + 200) / 100 + 1;
        rbMat.staticFriction = (-rLegHealth + -lLegHealth + 200) / 100 + 1;

        if (rLegHealth == 0 || lLegHealth == 0)
        {
            if (rLegHealth == 0 && lLegHealth == 0)
            {
                rbMat.dynamicFriction = 1000;
                rbMat.staticFriction = 1000;
            }
            else
            {
                rbMat.dynamicFriction = (-rLegHealth + -lLegHealth + 200) / 25 + 20;
                rbMat.staticFriction = (-rLegHealth + -lLegHealth + 200) / 25 + 20;
            }
        }

        if (stamina != movement.stamina)
        {
            bool isDecrease = movement.stamina < stamina;
            float difference = Mathf.Abs(movement.stamina - stamina);
            Vector3 oldPos = staminaBar.rectTransform.localPosition;
            stamina = movement.stamina;
            UpdateStaminaBar();
            Vector3 newPos = staminaBar.rectTransform.localPosition;
            if (difference > 5) HandleStaminaBarCopy(oldPos, newPos, isDecrease);
        }

        if (Input.GetKeyDown(KeyCode.H) && dead)
        {
            Manager.Instance.Panel(deathPanel);
        }
        if (Input.GetKeyDown(KeyCode.R) && dead)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            Time.timeScale = 1;
        }

        if (multiplier > 1) multiplier /= 2f;
    }

    void Regen()
    {
        HealAll(regen);
    }

    public void Damage(Vector3 part, float damage)
    {
        Manager.Instance.Sound(damageSound, (damage / 100) + 0.2f);
        Dictionary<GameObject, Vector3> bodyParts = new Dictionary<GameObject, Vector3>();

        if (health > 0)
        {
            bodyParts.Add(head, head.transform.position);
        }
        if (bodyHealth > 0)
        {
            bodyParts.Add(body, body.transform.position);
        }
        if (rArmHealth > 0)
        {
            bodyParts.Add(rArm, rArm.transform.position);
        }
        if (lArmHealth > 0)
        {
            bodyParts.Add(lArm, lArm.transform.position);
        }
        if (rLegHealth > 0)
        {
            bodyParts.Add(rLeg, rLeg.transform.position);
        }
        if (lLegHealth > 0)
        {
            bodyParts.Add(lLeg, lLeg.transform.position);
        }

        GameObject closestPart = null;

        if (health == 0) closestPart = head;
        else if (bodyHealth == 0) closestPart = body;
        else
        {
            float minDistance = float.MaxValue;

            foreach (var kvp in bodyParts)
            {
                float dist = Vector3.Distance(part, kvp.Value);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestPart = kvp.Key;
                }
            }
        }

        if (closestPart == null) return;

        if (closestPart == head)
        {
            Vector3 oldPos = healthBar.rectTransform.localPosition;
            health -= damage;
            if (health < 0) { Damage(part, -health); health = 0; Manager.Instance.Sound(breakSound); }
            UpdateHealthBar();
            Vector3 newPos = healthBar.rectTransform.localPosition;
            HandleBarCopy(head, oldPos, newPos, true);
        }
        else if (closestPart == body)
        {
            Vector3 oldPos = bodyHealthBar.rectTransform.localPosition;
            bodyHealth -= damage;
            if (bodyHealth < 0) { Damage(part, -bodyHealth); bodyHealth = 0; Manager.Instance.Sound(breakSound); }
            UpdateHealthBar();
            Vector3 newPos = bodyHealthBar.rectTransform.localPosition;
            HandleBarCopy(body, oldPos, newPos, true);
        }
        else if (closestPart == rArm)
        {
            Vector3 oldPos = rArmHealthBar.rectTransform.localPosition;
            rArmHealth -= damage;
            if (rArmHealth < 0) { Damage(part, -rArmHealth); rArmHealth = 0; Manager.Instance.Sound(breakSound); }
            UpdateHealthBar();
            Vector3 newPos = rArmHealthBar.rectTransform.localPosition;
            HandleBarCopy(rArm, oldPos, newPos, true);
        }
        else if (closestPart == lArm)
        {
            Vector3 oldPos = lArmHealthBar.rectTransform.localPosition;
            lArmHealth -= damage;
            if (lArmHealth < 0) { Damage(part, -lArmHealth); lArmHealth = 0; Manager.Instance.Sound(breakSound); }
            UpdateHealthBar();
            Vector3 newPos = lArmHealthBar.rectTransform.localPosition;
            HandleBarCopy(lArm, oldPos, newPos, true);
        }
        else if (closestPart == rLeg)
        {
            Vector3 oldPos = rLegHealthBar.rectTransform.localPosition;
            rLegHealth -= damage;
            if (rLegHealth < 0) { Damage(part, -rLegHealth); rLegHealth = 0; Manager.Instance.Sound(breakSound); }
            UpdateHealthBar();
            Vector3 newPos = rLegHealthBar.rectTransform.localPosition;
            HandleBarCopy(rLeg, oldPos, newPos, true);
        }
        else if (closestPart == lLeg)
        {
            Vector3 oldPos = lLegHealthBar.rectTransform.localPosition;
            lLegHealth -= damage;
            if (lLegHealth < 0) { Damage(part, -lLegHealth); lLegHealth = 0; Manager.Instance.Sound(breakSound); }
            UpdateHealthBar();
            Vector3 newPos = lLegHealthBar.rectTransform.localPosition;
            HandleBarCopy(lLeg, oldPos, newPos, true);
        }
    }

    public void DamageAll(float damage)
    {
        Manager.Instance.Sound(damageSound, (damage / 100) + 0.2f);
        Vector3 oldHeadPos = healthBar.rectTransform.localPosition;
        Vector3 oldBodyPos = bodyHealthBar.rectTransform.localPosition;
        Vector3 oldRArmPos = rArmHealthBar.rectTransform.localPosition;
        Vector3 oldLArmPos = lArmHealthBar.rectTransform.localPosition;
        Vector3 oldRLegPos = rLegHealthBar.rectTransform.localPosition;
        Vector3 oldLLegPos = lLegHealthBar.rectTransform.localPosition;

        health -= damage;
        if (health < 0) health = 0;
        bodyHealth -= damage;
        if (bodyHealth < 0) { bodyHealth = 0; Manager.Instance.Sound(breakSound); }
        rArmHealth -= damage;
        if (rArmHealth < 0) { rArmHealth = 0; Manager.Instance.Sound(breakSound); }
        lArmHealth -= damage;
        if (lArmHealth < 0) { lArmHealth = 0; Manager.Instance.Sound(breakSound); }
        rLegHealth -= damage;
        if (rLegHealth < 0) { rLegHealth = 0; Manager.Instance.Sound(breakSound); }
        lLegHealth -= damage;
        if (lLegHealth < 0) { lLegHealth = 0; Manager.Instance.Sound(breakSound); }

        UpdateHealthBar();

        Vector3 newHeadPos = healthBar.rectTransform.localPosition;
        Vector3 newBodyPos = bodyHealthBar.rectTransform.localPosition;
        Vector3 newRArmPos = rArmHealthBar.rectTransform.localPosition;
        Vector3 newLArmPos = lArmHealthBar.rectTransform.localPosition;
        Vector3 newRLegPos = rLegHealthBar.rectTransform.localPosition;
        Vector3 newLLegPos = lLegHealthBar.rectTransform.localPosition;

        HandleBarCopy(head, oldHeadPos, newHeadPos, true);
        HandleBarCopy(body, oldBodyPos, newBodyPos, true);
        HandleBarCopy(rArm, oldRArmPos, newRArmPos, true);
        HandleBarCopy(lArm, oldLArmPos, newLArmPos, true);
        HandleBarCopy(rLeg, oldRLegPos, newRLegPos, true);
        HandleBarCopy(lLeg, oldLLegPos, newLLegPos, true);
    }

    public void Heal(Vector3 part, float heal)
    {
        Dictionary<GameObject, Vector3> bodyParts = new Dictionary<GameObject, Vector3>();

        if ((health > 0 || heal > 1) && health < maxHealth) bodyParts.Add(head, head.transform.position);
        if ((bodyHealth > 0 || heal > 1) && bodyHealth < maxHealth) bodyParts.Add(body, body.transform.position);
        if ((rArmHealth > 0 || heal > 1) && rArmHealth < maxHealth) bodyParts.Add(rArm, rArm.transform.position);
        if ((lArmHealth > 0 || heal > 1) && lArmHealth < maxHealth) bodyParts.Add(lArm, lArm.transform.position);
        if ((rLegHealth > 0 || heal > 1) && rLegHealth < maxHealth) bodyParts.Add(rLeg, rLeg.transform.position);
        if ((lLegHealth > 0 || heal > 1) && lLegHealth < maxHealth) bodyParts.Add(lLeg, lLeg.transform.position);

        GameObject closestPart = null;
        float minDistance = float.MaxValue;

        foreach (var kvp in bodyParts)
        {
            float dist = Vector3.Distance(part, kvp.Value);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPart = kvp.Key;
            }
        }

        if (closestPart == null) return;

        if (closestPart == head)
        {
            Vector3 oldPos = healthBar.rectTransform.localPosition;
            if (heal > 1 || health != 0) health += heal;
            if (health > maxHealth) { Heal(part, health - maxHealth); health = maxHealth; }
            if (heal > 2) UpdateHealthBar();
            Vector3 newPos = healthBar.rectTransform.localPosition;
            if (heal > 2) HandleBarCopy(head, oldPos, newPos, false);
        }
        else if (closestPart == body)
        {
            Vector3 oldPos = bodyHealthBar.rectTransform.localPosition;
            if (heal > 1 || bodyHealth != 0) bodyHealth += heal;
            if (bodyHealth > maxbodyHealth) { Heal(part, bodyHealth - maxbodyHealth); bodyHealth = maxbodyHealth; }
            if (heal > 2) UpdateHealthBar();
            Vector3 newPos = bodyHealthBar.rectTransform.localPosition;
            if (heal > 2) HandleBarCopy(body, oldPos, newPos, false);
        }
        else if (closestPart == rArm)
        {
            Vector3 oldPos = rArmHealthBar.rectTransform.localPosition;
            if (heal > 1 || rArmHealth != 0) rArmHealth += heal;
            if (rArmHealth > maxrArmHealth) { Heal(part, rArmHealth - maxrArmHealth); rArmHealth = maxrArmHealth; }
            if (heal > 2) UpdateHealthBar();
            Vector3 newPos = rArmHealthBar.rectTransform.localPosition;
            if (heal > 2) HandleBarCopy(rArm, oldPos, newPos, false);
        }
        else if (closestPart == lArm)
        {
            Vector3 oldPos = lArmHealthBar.rectTransform.localPosition;
            if (heal > 1 || lArmHealth != 0) lArmHealth += heal;
            if (lArmHealth > maxlArmHealth) { Heal(part, lArmHealth - maxlArmHealth); lArmHealth = maxlArmHealth; }
            if (heal > 2) UpdateHealthBar();
            Vector3 newPos = lArmHealthBar.rectTransform.localPosition;
            if (heal > 2) HandleBarCopy(lArm, oldPos, newPos, false);
        }
        else if (closestPart == rLeg)
        {
            Vector3 oldPos = rLegHealthBar.rectTransform.localPosition;
            if (heal > 1 || rLegHealth != 0) rLegHealth += heal;
            if (rLegHealth > maxrLegHealth) { Heal(part, rLegHealth - maxrLegHealth); rLegHealth = maxrLegHealth; }
            if (heal > 2) UpdateHealthBar();
            Vector3 newPos = rLegHealthBar.rectTransform.localPosition;
            if (heal > 2) HandleBarCopy(rLeg, oldPos, newPos, false);
        }
        else if (closestPart == lLeg)
        {
            Vector3 oldPos = lLegHealthBar.rectTransform.localPosition;
            if (heal > 1 || lLegHealth != 0) lLegHealth += heal;
            if (lLegHealth > maxlLegHealth) { Heal(part, lLegHealth - maxlLegHealth); lLegHealth = maxlLegHealth; }
            if (heal > 2) UpdateHealthBar();
            Vector3 newPos = lLegHealthBar.rectTransform.localPosition;
            if (heal > 2) HandleBarCopy(lLeg, oldPos, newPos, false);
        }
    }

    public void HealAll(float heal)
    {
        Vector3 oldHeadPos = healthBar.rectTransform.localPosition;
        Vector3 oldBodyPos = bodyHealthBar.rectTransform.localPosition;
        Vector3 oldRArmPos = rArmHealthBar.rectTransform.localPosition;
        Vector3 oldLArmPos = lArmHealthBar.rectTransform.localPosition;
        Vector3 oldRLegPos = rLegHealthBar.rectTransform.localPosition;
        Vector3 oldLLegPos = lLegHealthBar.rectTransform.localPosition;

        if (heal > 1 || health != 0) health += heal;
        if (health > maxHealth) health = maxHealth;

        if (heal > 1 || bodyHealth != 0) bodyHealth += heal;
        if (bodyHealth > maxbodyHealth) bodyHealth = maxbodyHealth;

        if (heal > 1 || rArmHealth != 0) rArmHealth += heal;
        if (rArmHealth > maxrArmHealth) rArmHealth = maxrArmHealth;

        if (heal > 1 || lArmHealth != 0) lArmHealth += heal;
        if (lArmHealth > maxlArmHealth) lArmHealth = maxlArmHealth;

        if (heal > 1 || rLegHealth != 0) rLegHealth += heal;
        if (rLegHealth > maxrLegHealth) rLegHealth = maxrLegHealth;

        if (heal > 1 || lLegHealth != 0) lLegHealth += heal;
        if (lLegHealth > maxlLegHealth) lLegHealth = maxlLegHealth;

        UpdateHealthBar();

        Vector3 newHeadPos = healthBar.rectTransform.localPosition;
        Vector3 newBodyPos = bodyHealthBar.rectTransform.localPosition;
        Vector3 newRArmPos = rArmHealthBar.rectTransform.localPosition;
        Vector3 newLArmPos = lArmHealthBar.rectTransform.localPosition;
        Vector3 newRLegPos = rLegHealthBar.rectTransform.localPosition;
        Vector3 newLLegPos = lLegHealthBar.rectTransform.localPosition;

        if (heal > 2)
        {
            HandleBarCopy(head, oldHeadPos, newHeadPos, false);
            HandleBarCopy(body, oldBodyPos, newBodyPos, false);
            HandleBarCopy(rArm, oldRArmPos, newRArmPos, false);
            HandleBarCopy(lArm, oldLArmPos, newLArmPos, false);
            HandleBarCopy(rLeg, oldRLegPos, newRLegPos, false);
            HandleBarCopy(lLeg, oldLLegPos, newLLegPos, false);
        }
    }

    void HandleBarCopy(GameObject part, Vector3 oldPos, Vector3 newPos, bool isDamage)
    {
        Image originalBar = barMap[part];
        if (!barCopies.ContainsKey(part))
        {
            Image copy = Instantiate(originalBar, originalBar.transform.parent);
            copy.rectTransform.localPosition = oldPos;
            copy.color = isDamage ? new Color(0f, 0f, 0f, 1f) : new Color(1f, 1f, 1f, 1f);
            damagePanel.color = isDamage ? new Color(1f, 0f, 0f, 1f) : new Color(0f, 1f, 0f, 1f);
            if (isDamage)
            {
                copy.transform.SetSiblingIndex(originalBar.transform.GetSiblingIndex());
            }
            else
            {
                copy.transform.SetSiblingIndex(originalBar.transform.GetSiblingIndex() + 1);
            }
            barCopies[part] = copy;
        }
        else
        {
            barCopies[part].color = isDamage ? new Color(0f, 0f, 0f, 1f) : new Color(1f, 1f, 1f, 1f);
            damagePanel.color = isDamage ? new Color(1f, 0f, 0f, 1f) : new Color(0f, 1f, 0f, 1f);
            barCopies[part].rectTransform.localPosition = oldPos;
            if (isDamage)
            {
                barCopies[part].transform.SetSiblingIndex(originalBar.transform.GetSiblingIndex());
            }
            else
            {
                barCopies[part].transform.SetSiblingIndex(originalBar.transform.GetSiblingIndex() + 1);
            }
        }

        if (copyCoroutines.ContainsKey(part))
        {
            StopCoroutine(copyCoroutines[part]);
        }
        copyCoroutines[part] = StartCoroutine(AnimateBarCopy(part, newPos));
    }

    void HandleStaminaBarCopy(Vector3 oldPos, Vector3 newPos, bool isDecrease)
    {
        Image copy = Instantiate(staminaBar, staminaBar.transform.parent);
        copy.rectTransform.localPosition = oldPos;
        copy.color = isDecrease ? new Color(0f, 0f, 0f, 1f) : new Color(1f, 1f, 1f, 1f);
        if (isDecrease)
        {
            copy.transform.SetSiblingIndex(staminaBar.transform.GetSiblingIndex());
        }
        else
        {
            copy.transform.SetSiblingIndex(staminaBar.transform.GetSiblingIndex() + 1);
        }
        StartCoroutine(AnimateStaminaBarCopy(copy, newPos));
    }

    IEnumerator AnimateStaminaBarCopy(Image copy, Vector3 targetPos)
    {
        Vector3 startPos = copy.rectTransform.localPosition;
        float startAlpha = copy.color.a;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            copy.rectTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            copy.color = new Color(copy.color.r, copy.color.g, copy.color.b, Mathf.Lerp(startAlpha, 0f, t));
            yield return null;
        }

        copy.rectTransform.localPosition = targetPos;
        copy.color = new Color(copy.color.r, copy.color.g, copy.color.b, 0f);
        Destroy(copy.gameObject);
    }

    IEnumerator AnimateBarCopy(GameObject part, Vector3 targetPos)
    {
        Image copy = barCopies[part];
        Vector3 startPos = copy.rectTransform.localPosition;
        float startAlpha = copy.color.a;
        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            copy.rectTransform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            copy.color = new Color(copy.color.r, copy.color.g, copy.color.b, Mathf.Lerp(startAlpha, 0f, t));
            damagePanel.color = new Color(damagePanel.color.r, damagePanel.color.g, damagePanel.color.b, Mathf.Lerp(startAlpha, 0f, t));
            yield return null;
        }

        copy.rectTransform.localPosition = targetPos;
        copy.color = new Color(copy.color.r, copy.color.g, copy.color.b, 0f);
        damagePanel.color = new Color(damagePanel.color.r, damagePanel.color.g, damagePanel.color.b, 0f);
        Destroy(copy.gameObject);
        barCopies.Remove(part);
        copyCoroutines.Remove(part);
    }

    void UpdateHealthBar()
    {
        float ratio = health / maxHealth;
        healthBar.rectTransform.localPosition = new Vector3(0, healthBar.rectTransform.rect.height * ratio - healthBar.rectTransform.rect.height, 0);
        healthBar.color = healthGradient.Evaluate(ratio);
        if (health > 0) healthText.text = Mathf.Ceil(health).ToString();
        else healthText.text = "X";

        ratio = bodyHealth / maxbodyHealth;
        bodyHealthBar.rectTransform.localPosition = new Vector3(0, bodyHealthBar.rectTransform.rect.height * ratio - bodyHealthBar.rectTransform.rect.height, 0);
        bodyHealthBar.color = healthGradient.Evaluate(ratio);
        if (bodyHealth > 0) bodyHealthText.text = Mathf.Ceil(bodyHealth).ToString();
        else bodyHealthText.text = "X";

        ratio = rArmHealth / maxrArmHealth;
        rArmHealthBar.rectTransform.localPosition = new Vector3(rArmHealthBar.rectTransform.rect.width * ratio - rArmHealthBar.rectTransform.rect.width, 0, 0);
        rArmHealthBar.color = healthGradient.Evaluate(ratio);
        if (rArmHealth > 0) rArmHealthText.text = Mathf.Ceil(rArmHealth).ToString();
        else rArmHealthText.text = "X";

        ratio = lArmHealth / maxlArmHealth;
        lArmHealthBar.rectTransform.localPosition = new Vector3(lArmHealthBar.rectTransform.rect.width - lArmHealthBar.rectTransform.rect.width * ratio, 0, 0);
        lArmHealthBar.color = healthGradient.Evaluate(ratio);
        if (lArmHealth > 0) lArmHealthText.text = Mathf.Ceil(lArmHealth).ToString();
        else lArmHealthText.text = "X";

        ratio = rLegHealth / maxrLegHealth;
        rLegHealthBar.rectTransform.localPosition = new Vector3(0, rLegHealthBar.rectTransform.rect.height * ratio - rLegHealthBar.rectTransform.rect.height, 0);
        rLegHealthBar.color = healthGradient.Evaluate(ratio);
        if (rLegHealth > 0) rLegHealthText.text = Mathf.Ceil(rLegHealth).ToString();
        else rLegHealthText.text = "X";

        ratio = lLegHealth / maxlLegHealth;
        lLegHealthBar.rectTransform.localPosition = new Vector3(0, lLegHealthBar.rectTransform.rect.height * ratio - lLegHealthBar.rectTransform.rect.height, 0);
        lLegHealthBar.color = healthGradient.Evaluate(ratio);
        if (lLegHealth > 0) lLegHealthText.text = Mathf.Ceil(lLegHealth).ToString();
        else lLegHealthText.text = "X";

        StartCoroutine(PlayerDied());
    }

    void UpdateStaminaBar()
    {
        float ratio = stamina / maxStamina;
        staminaBar.rectTransform.localPosition = new Vector3(staminaBar.rectTransform.rect.width * ratio - staminaBar.rectTransform.rect.width, 0, 0);
        staminaBar.color = staminaGradient.Evaluate(ratio);
        staminaText.text = Mathf.Ceil(stamina).ToString();
    }

    void OnCollisionStay(Collision collision)
    {
        if (canDamage)
        {
            if (collision.gameObject.tag == "Heal")
            {
                HealAll(float.Parse(collision.gameObject.name));
                Manager.Instance.Sound(healSound, Mathf.Max(float.Parse(collision.gameObject.name), 25f) / 100);
                if (float.Parse(collision.gameObject.name) >= 50)
                {
                    Destroy(collision.gameObject);
                    Manager.Instance.Pause(0.5f);
                    Manager.Instance.Sound(pickupSound, Mathf.Max(float.Parse(collision.gameObject.name), 50f) / 100);
                }
                Manager.Instance.Flash();
                canDamage = false;
            }
            else if (collision.gameObject.tag == "Damage")
            {
                Damage(collision.contacts[0].point, float.Parse(collision.gameObject.name));
                Manager.Instance.Sound(damageSound, float.Parse(collision.gameObject.name) / 100);
                if (float.Parse(collision.gameObject.name) >= 33)
                {
                    Vector3 direction = transform.position - collision.contacts[0].point;
                    rb.AddForce(direction * 5 * (float.Parse(collision.gameObject.name) / 10), ForceMode.Impulse);
                    multiplier += 5;
                }
                canDamage = false;
            }
        }
    }

    IEnumerator PlayerDied()
    {
        if ((health < 1 || bodyHealth < 1) && !dead)
        {
            Manager.Instance.Sound(warningSound);
            Manager.Instance.Popup(warningPanel, 0.5f);
            multiplier += 100;
            dead = true;
            yield return new WaitForSeconds(1);
            if (health < 1 || bodyHealth < 1)
            {
                Manager.Instance.Flash();
                Manager.Instance.Pause(0.5f);
                Manager.Instance.Panel(deathPanel);
                Manager.Instance.Sound(deathSound[Random.Range(0, deathSound.Length)]);
                rb.freezeRotation = false;
            }
            else dead = false;
        }
        else yield return null;
    }

    void CanDamage()
    {
        canDamage = true;
    }
}