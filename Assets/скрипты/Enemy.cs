using System.Collections.Generic;
using Minimalist.Bar.Quantity;
using DamageNumbersPro.Demo;
using System.Collections;
using DamageNumbersPro;
using UnityEngine;
using System.Linq;
using TMPro;

public abstract class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public string enemyName = "Enemy"; // Name for health bar display
    public float maxHealth = 100f;
    public float health = 100f;
    public float mergeRange = 2f;

    [Header("Effects")]
    [SerializeField] protected DamageNumber textPrefab;
    [SerializeField] protected DamageNumber critPrefab;
    protected DNP_PrefabSettings settings;

    [SerializeField] protected Transform healthBarPrefab;
    protected QuantityBhv quantity;
    protected TextMeshProUGUI healthBarNameText; // For displaying enemy name

    [SerializeField] protected GameObject BloodAttach;
    [SerializeField] protected GameObject[] BloodFX;
    [SerializeField] protected Color[] bloodColors = new Color[] { Color.red }; // Array of possible blood colors
    protected List<GameObject> bloodInstances = new List<GameObject>();

    [SerializeField] float timeBeforeShrink; // Время ожидания перед началом уменьшения (в секундах)
    [SerializeField] float shrinkDuration;   // Длительность уменьшения (в секундах). Если отрицательная, уменьшения и уничтожения не будет
    protected Light dirLight;
    protected int effectIdx;

    public enum EffectType { None, Burning, DoubleBurning, Oil, Freeze, Electric }
    protected List<EffectType> activeEffects = new List<EffectType>();
    protected Enemy mergedEnemy;
    protected SpringJoint springJoint;
    public float damageMultiplier = 1f;
    protected AudioSource source;

    protected virtual void Awake()
    {
        maxHealth = health;
        dirLight = FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);

        Transform healthBar = Instantiate(healthBarPrefab, transform);
        quantity = healthBar.Find("Quantity").GetComponent<QuantityBhv>();
        quantity.MaximumAmount = maxHealth;
        quantity.Amount = health;

        healthBarNameText = healthBar.GetComponentInChildren<TextMeshProUGUI>();
        if (healthBarNameText != null)
        {
            healthBarNameText.text = enemyName;
        }
        else
        {
            Debug.LogWarning($"[Enemy] No TextMeshProUGUI found in health bar for {enemyName}");
        }
    }

    protected virtual void Start() => source = GetComponent<AudioSource>();

    public virtual void Damage(float damage, bool crit = false, Vector3? target = null)
    {
        Vector3 actualTarget = target ?? transform.position;
        health -= damage * damageMultiplier;
        DNPSet(actualTarget, damage, crit);
        quantity.Amount = health;
        for (int i = 0; i < Mathf.Min(damage / 25f, 10); i++) Blood(actualTarget);

        // Heal player based on distance
        Transform player = GameObject.FindWithTag("Player")?.transform;
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            float heal = Mathf.Clamp01(1f - (distance / 10)) * damage * 2;
            if (heal > 0f) player.GetComponent<Health>().Heal(transform.position, heal);
        }

        if (health <= 0)
        {
            if (mergedEnemy != null)
            {
                mergedEnemy.damageMultiplier -= 0.5f;
            }

            Manager.Instance.Kill(damage, source);
            for (int i = 0; i < 10; i++) Blood(actualTarget);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.AddForce(Random.insideUnitSphere * 10);
            rb.AddTorque(Random.insideUnitSphere * 10);

            gameObject.tag = "Untagged";
            Component[] components = GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp is Enemy)
                {
                    Destroy(comp);
                }
            }
            if (shrinkDuration >= 0) Manager.Instance.Shrink(transform, timeBeforeShrink, shrinkDuration);
        }
        else
        {
            Manager.Instance.Damage(damage, source);
        }

        Debug.Log($"[{enemyName}] Took damage: {damage}, health left: {health}/{maxHealth}");
    }

    public void ApplyEffect(EffectType effect)
    {
        if (!activeEffects.Contains(effect) || effect == EffectType.Oil)
        {
            activeEffects.Add(effect);
        }
    }

    public bool HasEffect(EffectType effect)
    {
        return activeEffects.Contains(effect);
    }

    protected virtual void ApplyEffects()
    {
        foreach (var effect in activeEffects.ToArray())
        {
            switch (effect)
            {
                case EffectType.Burning:
                    Damage(5f * Time.deltaTime);
                    break;
                case EffectType.DoubleBurning:
                    Damage(10f * Time.deltaTime);
                    break;
                case EffectType.Freeze:
                    // Handled in derived classes
                    break;
                case EffectType.Electric:
                    Damage(5f * Time.deltaTime);
                    break;
            }
        }
    }

    protected void DNPSet(Vector3 target, float number, bool crit = false)
    {
        DamageNumber pref = crit ? critPrefab : textPrefab;
        settings = pref.gameObject.GetComponent<DNP_PrefabSettings>();
        if (pref.digitSettings.decimals == 0)
        {
            number = Mathf.Floor(number);
        }
        DamageNumber newDamageNumber = pref.Spawn(target, number);
        settings.Apply(newDamageNumber);
        newDamageNumber.enableFollowing = true;
        newDamageNumber.followedTarget = transform;
    }

    protected void Blood(Vector3 target)
    {
        float angle = Random.Range(0f, 360f);
        if (effectIdx == BloodFX.Length) effectIdx = 0;
        var instance = Instantiate(BloodFX[effectIdx], target, Quaternion.Euler(0, angle, 0));

        // Apply random blood color
        Color selectedColor = bloodColors.Length > 1 ? bloodColors[Random.Range(0, bloodColors.Length)] : bloodColors[0];
        var bloodRenderer = instance.GetComponentInChildren<Renderer>();
        if (bloodRenderer != null)
        {
            bloodRenderer.material.color = selectedColor;
        }

        effectIdx++;
        var settings = instance.GetComponent<BFX_BloodSettings>();
        if (dirLight != null) settings.LightIntensityMultiplier = dirLight.intensity;
        Destroy(instance.gameObject, 30);

        GameObject attachBloodInstance = Instantiate(BloodAttach, target, Quaternion.identity);
        bloodInstances.Add(attachBloodInstance);
        if (bloodInstances.Count > 10)
        {
            Destroy(bloodInstances[0]);
            bloodInstances.RemoveAt(0);
        }

        Transform bloodT = attachBloodInstance.transform;
        bloodT.localRotation = Quaternion.identity;
        bloodT.localScale = Vector3.one * Random.Range(0.75f, 1.2f);
        bloodT.Rotate(90, 0, 0);
        bloodT.parent = transform;
        Destroy(attachBloodInstance.gameObject, 30);
    }
}
