using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

public class Axe : WeaponBase
{
    [SerializeField] public float axeRange = 2f;
    [SerializeField] public float axeDamage = 10f;
    [SerializeField] private float axeAltDamageMultiplier = 0.2f;
    [SerializeField] private float axeAltDamageMultiplierLowHealth = 1f;

    [SerializeField] private AudioClip primaryHit;
    [SerializeField] private AudioClip primaryMiss;
    [SerializeField] private AudioClip altHit;
    [SerializeField] private AudioClip altMiss;
    [SerializeField] private AudioClip altSwing;
    [SerializeField] private AudioClip altImpact;
    float bonus;
    Movement movement;
    public GameObject altParticlePrefab;
    ParticleSystem altParticle;
    public float altParticleTimer;

    void Awake()
    {
        base.Initialize();
        movement = GameObject.FindWithTag("Player").GetComponent<Movement>();
        altParticle = Instantiate(altParticlePrefab, transform.position, transform.rotation).GetComponent<ParticleSystem>();
        altParticle.Stop();
    }

    void AltParticle(float timer)
    {
        altParticle.Play();
        altParticleTimer += timer;
    }

    void Update()
    {
        base.UpdateWeapon();
        if (altParticle != null)
        {
            if (altParticleTimer > 0)
            {
                altParticleTimer -= Time.deltaTime;
                altParticle.transform.position = transform.position;
                altParticle.transform.rotation = transform.rotation;
            }
            else
            {
                altParticle.Stop();
            }
        }
    }

    public override void StartPrimaryAttack()
    {
        cooldownTimer = 0;
    }
    
    public override void HoldPrimaryAttack()
    {
        if (cooldownTimer > 0) return;
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("топор 1");
            Debug.Log("[Axe] Normal attack trigger set");
        }
        Vector3 boxSize = new Vector3(axeRange + (Manager.Instance.rb.velocity.magnitude / 10), axeRange + (Manager.Instance.rb.velocity.magnitude / 10), axeRange + (Manager.Instance.rb.velocity.magnitude / 5));
        RaycastHit[] axeHits = Physics.BoxCastAll(transform.position, boxSize / 2, transform.forward, Quaternion.identity, axeRange + (Manager.Instance.rb.velocity.magnitude / 3));
        bool isHit = false;
        foreach (var axeHit in axeHits)
        {
            Enemy enemy = axeHit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Damage(axeDamage, false, axeHit.point == Vector3.zero ? enemy.transform.position : axeHit.point);
                Debug.Log($"[Axe] Hit enemy: {axeHit.collider.name}, damage: {axeDamage}");
                isHit = true;
            }
        }
        if (isHit)
        {
            Manager.Instance.Sound(primaryHit);
            Particle(0.1f);
            Shake(0.1f, 0.5f);
        }
        else
        {
            Manager.Instance.Sound(primaryMiss);
        }
        cooldownTimer = cooldown;
        Debug.Log("[Axe] Normal attack");
    }
    public override void ReleasePrimaryAttack() { }

    private IEnumerator HoldAxeAltAttackCoroutine()
    {
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("топор 2");
            Debug.Log("[Axe] Alt attack trigger set");
        }
        bonus++;
        yield break;
    }

    private IEnumerator AxeAltAttackCoroutine()
    {
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("топор 3");
            Debug.Log("[Axe] Alt attack trigger set");
        }

        Vector3 boxSize = new Vector3(axeRange + (Manager.Instance.rb.velocity.magnitude / 20), axeRange + (Manager.Instance.rb.velocity.magnitude / 20), axeRange + (Manager.Instance.rb.velocity.magnitude / 10));
        RaycastHit[] axeHits = Physics.BoxCastAll(transform.position, boxSize / 2, transform.forward, Quaternion.identity, axeRange + (Manager.Instance.rb.velocity.magnitude / 3));
        bool isHit = false;
        foreach (var axeHit in axeHits)
        {
            Enemy enemy = axeHit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                float multiplier = (enemy.health < enemy.maxHealth * 0.5f) ? axeAltDamageMultiplierLowHealth : axeAltDamageMultiplier;
                float finalDamage = ((axeDamage + Manager.Instance.rb.velocity.magnitude * 5) * multiplier) + bonus + ammo;
                ammo = 0;
                enemy.Damage(finalDamage, enemy.health < enemy.maxHealth * 0.5f, axeHit.point == Vector3.zero ? enemy.transform.position : axeHit.point);
                isHit = true;
            }
        }
        if (isHit)
        {
            Manager.Instance.Sound(altImpact);

            Manager.Instance.Flash();
            Manager.Instance.Pause(Mathf.Min(1, Manager.Instance.rb.velocity.magnitude / 100));

            Manager.Instance.Sound(altHit);
            Shake(0.2f, 1f);
            AltParticle(0.1f);
            movement.stamina += Mathf.Min(Manager.Instance.rb.velocity.magnitude, movement.maxStamina * 1.5f - movement.stamina);

            if (Manager.Instance.rb.velocity.magnitude > 50) Manager.Instance.rb.velocity = -camera.forward * (Manager.Instance.rb.velocity.magnitude / 3);
            else Manager.Instance.rb.AddForce(-camera.forward * (Manager.Instance.rb.velocity.magnitude / 3), ForceMode.Impulse);
        }
        else
        {
            Manager.Instance.Sound(altMiss);
        }

        bonus = 0;
        Debug.Log("[Axe] Alt attack");

        yield break;
    }

    public override void StartAltAttack() { }
    public override void HoldAltAttack()
    {
        if (altCooldownTimer > 0) return;
        Manager.Instance.Sound(altSwing);
        Weapon.Instance.StartCoroutine(HoldAxeAltAttackCoroutine());
        altCooldownTimer = altCooldown;
    }
    public override void ReleaseAltAttack()
    {
        Weapon.Instance.StartCoroutine(AxeAltAttackCoroutine());
    }
    
    public override string GetAmmoText() => "";
    public override string GetAltAmmoText() => bonus > 0 ? $"{Mathf.Round(Manager.Instance.rb.velocity.magnitude * 5 * axeAltDamageMultiplier)}+{bonus+ammo}" : $"0+{ammo}";
}