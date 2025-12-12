using System.Collections.Generic;
using Minimalist.Bar.Quantity;
using Unity.VisualScripting;
using DamageNumbersPro.Demo;
using System.Collections;
using DamageNumbersPro;
using UnityEngine;
using System.Linq;
using TMPro;

public abstract class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public string enemyName = "Enemy";
    public float maxHealth = 100f;
    public float health = 100f;

    [Header("Effects")]
    QuantityBhv quantity;

    [SerializeField] protected Color[] bloodColors = new Color[] { Color.red };
    protected List<GameObject> bloodInstances = new List<GameObject>();

    [SerializeField] float timeBeforeShrink;
    [SerializeField] float shrinkDuration;
    protected Light dirLight;
    protected int effectIdx;

    public enum EffectType { Burning, DoubleBurning, Oil, Freeze, Electric }
    protected List<EffectType> activeEffects = new List<EffectType>();
    protected SpringJoint springJoint;
    public float damageMultiplier = 1f;
    protected AudioSource source;
    protected Transform player;
    Transform healthBar;

    protected virtual void Awake()
    {
        maxHealth = health;
        player = GameObject.FindWithTag("Player").transform;
        dirLight = FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
    }

    protected virtual void Start()
    {
        if (maxHealth > 1)
        {            
            healthBar = Instantiate(Manager.Instance.healthBarPrefab, transform);
            quantity = healthBar.Find("Quantity").GetComponent<QuantityBhv>();
            quantity.MaximumAmount = maxHealth;
            quantity.Amount = health;

            TextMeshProUGUI healthBarNameText = healthBar.GetChild(0).Find("Name").GetComponent<TextMeshProUGUI>();
            healthBarNameText.text = enemyName;
        }
        source = GetComponent<AudioSource>();
    }

    protected virtual void Update()
    {
        ApplyEffects();
        if (transform.position.y < -100) Damage(100);
    }

    public virtual void Damage(float damage, bool crit = false, Vector3? target = null, List<EffectType> effectTypes = null)
    {
        if (maxHealth > 1)
        {
            quantity.Amount = health;
        }

        activeEffects = effectTypes;
        if (target == null) target = transform.position;
        health -= damage * damageMultiplier;
        for (int i = 0; i < Mathf.Min(damage / 25f, 10); i++) Blood(target.Value);

        Transform player = GameObject.FindWithTag("Player")?.transform;
        if (player != null && bloodColors.Length != 0)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            float heal = Mathf.Clamp01(1f - (distance / 10)) * damage * 2;
            if (heal > 0f) player.GetComponent<Health>().Heal(transform.position, heal);
        }

        if (health <= 0 && gameObject.tag != "Untagged")
        {
            if (maxHealth > 1)
            {                
                Manager.Instance.totalKills++;
                Manager.Instance.levelKills++;
                Manager.Instance.Kill(damage, target.Value, transform, crit, source);
            }

            gameObject.tag = "Untagged";
            if (maxHealth > 1) Destroy(healthBar.gameObject);
            for (int i = 0; i < 10; i++) Blood(target.Value);

            if (transform.childCount <= 2)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = gameObject.AddComponent<Rigidbody>();
                }
                rb.constraints = RigidbodyConstraints.None;
                rb.isKinematic = false;
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
                    rb.constraints = RigidbodyConstraints.None;
                    rb.isKinematic = false;
                    rb.AddForce(Random.insideUnitSphere * 100);
                    rb.AddTorque(Random.insideUnitSphere * 100);
                }
            }
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
    }

    protected void Blood(Vector3 target)
    {
        if (bloodColors.Length == 0) return;
        Vector3 direction = player.position - target;
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        angle += 90 + Random.Range(-45, 45);

        if (effectIdx == Manager.Instance.BloodFX.Length) effectIdx = 0;
        var instance = Instantiate(Manager.Instance.BloodFX[effectIdx], target, Quaternion.Euler(0, angle, 0));

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
        if (activeEffects != null) return activeEffects.Contains(effect);
        else return false;
    }

    protected virtual void ApplyEffects()
    {
        if (activeEffects == null) return;
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
                    break;
                case EffectType.Electric:
                    Damage(5f * Time.deltaTime);
                    break;
            }
        }
    }
}