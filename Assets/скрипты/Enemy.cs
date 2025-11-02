using System.Collections.Generic;
using Minimalist.Bar.Quantity;
using DamageNumbersPro.Demo;
using System.Collections;
using DamageNumbersPro;
using UnityEngine;
using System.Linq;
using TMPro;
using Unity.VisualScripting;

public abstract class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public string enemyName = "Enemy"; // Name for health bar display
    public float maxHealth = 100f;
    public float health = 100f;

    [Header("Effects")]
    QuantityBhv quantity;
    TextMeshProUGUI healthBarNameText; // For displaying enemy name

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
    protected Transform player;

    protected virtual void Awake()
    {
        maxHealth = health;
        player = GameObject.FindWithTag("Player").transform;
        dirLight = FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);

        Transform healthBar = Instantiate(Manager.Instance.healthBarPrefab, transform);
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

    public virtual void Damage(float damage, bool crit = false, Vector3? target = null, Vector3? playerPos = null)
    {
        if (target == null) target = transform.position;
        health -= damage * damageMultiplier;
        quantity.Amount = health;
        for (int i = 0; i < Mathf.Min(damage / 25f, 10); i++) Blood(target.Value, playerPos);

        // Heal player based on distance
        Transform player = GameObject.FindWithTag("Player")?.transform;
        if (player != null && bloodColors.Length == 0)
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

            Manager.Instance.Kill(damage, target.Value, transform, crit, source);
            for (int i = 0; i < 10; i++) Blood(target.Value, playerPos);

            if (transform.childCount <= 2)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                }
                rb.AddForce(Random.insideUnitSphere * 100);
                rb.AddTorque(Random.insideUnitSphere * 100);
            }
            else
            {
                foreach (Collider c in gameObject.GetComponents<Collider>()) Destroy(c);
                foreach (Transform c in transform)
                {
                    if (c.name == "Health Bar(Clone)") continue;

                    MeshCollider mc = c.AddComponent<MeshCollider>();
                    mc.convex = true;
                    Rigidbody rb = c.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = c.AddComponent<Rigidbody>();
                    }
                    rb.AddForce(Random.insideUnitSphere * 100);
                    rb.AddTorque(Random.insideUnitSphere * 100);
                }
            }

            gameObject.tag = "Untagged";
            foreach (Component c in GetComponents<Component>())
            {
                if (c is Enemy)
                {
                    Destroy(c);
                }
            }
            if (shrinkDuration >= 0) Manager.Instance.Shrink(transform, timeBeforeShrink, shrinkDuration);
        }
        else
        {
            Manager.Instance.Damage(damage, target.Value, transform, crit, source);
        }

        Debug.Log($"[{enemyName}] Took damage: {damage}, health left: {health}/{maxHealth}");
    }

    protected void Blood(Vector3 target, Vector3? playerPos = null)
    {
        if (bloodColors.Length == 0) return;
        if (playerPos == null) playerPos = player.position;
        Vector3 direction = playerPos.Value - target;
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        angle += 90 + Random.Range(-45, 45);

        if (effectIdx == Manager.Instance.BloodFX.Length) effectIdx = 0;
        var instance = Instantiate(Manager.Instance.BloodFX[effectIdx], target, Quaternion.Euler(0, angle, 0));

        // Apply random blood color
        Color selectedColor = bloodColors.Length > 1 ? bloodColors[Random.Range(0, bloodColors.Length)] : bloodColors[0];
        foreach (Renderer r in instance.GetComponentsInChildren<Renderer>())
        {            
            if (r.name == "Decal") r.GetComponent<Renderer>().material.SetColor("_TintColor", selectedColor);
            else r.GetComponent<Renderer>().material.color = selectedColor;
        }

        effectIdx++;
        var settings = instance.GetComponent<BFX_BloodSettings>();
        if (dirLight != null) settings.LightIntensityMultiplier = dirLight.intensity;
        Destroy(instance.gameObject, 30);

        GameObject attachBloodInstance = Instantiate(Manager.Instance.BloodAttach, target, Quaternion.identity);
        attachBloodInstance.transform.GetChild(0).GetComponent<Renderer>().material.SetColor("_TintColor", selectedColor);
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
}