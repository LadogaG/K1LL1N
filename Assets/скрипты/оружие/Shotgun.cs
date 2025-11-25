using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class Shotgun : WeaponBase
{
    [SerializeField] private float shotgunDamage = 5f;
    [SerializeField] private float shotgunAltDamage = 5f;
    [SerializeField] private float shotgunAltRange = 3f;
    [SerializeField] private float shotgunAltRadius = 2f;
    [SerializeField] private float shotgunDamageMultiplier = 5f;

    [SerializeField] private AudioClip primary;
    [SerializeField] private AudioClip crit;
    [SerializeField] private AudioClip alt;
    public GameObject altParticlePrefab;
    ParticleSystem altParticle;
    public float altParticleTimer;
    int altAmmo;

    void Awake()
    {
        base.Initialize();
        Quaternion rotation = camera.rotation * Quaternion.Euler(0, 0, 180);
        altParticle = Instantiate(altParticlePrefab, transform.position, rotation).GetComponent<ParticleSystem>();
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
        if (ammo < maxAmmo) ammo += 0.02f;

        if (altParticle != null)
        {
            if (altParticleTimer > 0)
            {
                altParticleTimer -= Time.deltaTime;
                altParticle.transform.position = transform.position;
                altParticle.transform.rotation = camera.rotation * Quaternion.Euler(0, 0, 180);
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
        if (cooldownTimer > 0 || (ammo < 1 && altAmmo < 1)) return;

        foreach (var spawn in spawns)
        {
            for (int i = 0; i < 8; i++)
            {
                Manager.Instance.Fire(true, Manager.Instance.bullet, spawn.position, spawn.rotation, camera.forward, 15, 0.1f, altAmmo > 0);
            }
        }
        if (altAmmo > 0)
        {
            altAmmo--;
            Manager.Instance.Sound(crit);
            Particle(0.5f);
            Shake(0.5f, 1);
            Manager.Instance.rb.AddForce(-camera.forward * 15, ForceMode.Impulse);
        }
        else
        {
            ammo--;
            Manager.Instance.Sound(primary);
            Particle(0.25f);
            Shake(0.2f, 1);
            Manager.Instance.rb.AddForce(-camera.forward * 10, ForceMode.Impulse);
        }
        weaponAnimator.SetTrigger("1");
        cooldownTimer = cooldown;
    }
    public override void ReleasePrimaryAttack() { }

    private IEnumerator ShotgunAltAttack()
    {
        Manager.Instance.Sound(alt);
        Shake(0.1f, 0.5f);
        weaponAnimator.SetTrigger("2");
        altCooldownTimer = altCooldown;
        yield return new WaitForSeconds(0.1f);

        Vector3 boxSize = new Vector3(shotgunAltRadius * 2, shotgunAltRadius * 2, shotgunAltRange);
        RaycastHit[] shotgunAltHits = Physics.BoxCastAll(transform.position, boxSize / 2, transform.forward, Quaternion.identity, shotgunAltRange);
        bool isHit = false;
        foreach (var shotgunAltHit in shotgunAltHits)
        {
            Enemy enemy = shotgunAltHit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                isHit = true;
                bool crit = enemy.health < enemy.maxHealth * 0.1f;
                enemy.Damage(crit ? shotgunAltDamage * 20 : shotgunAltDamage, crit, shotgunAltHit.point == Vector3.zero ? enemy.transform.position : shotgunAltHit.point);
                ammo += 2;
                if (enemy.health <= 0)
                {
                    if (ammo < maxAmmo) ammo += 3;
                    altAmmo += 1;
                }
            }
        }
        if (isHit) AltParticle(0.1f);
    }

    public override void StartAltAttack() { }
    public override void HoldAltAttack()
    {
        if (altCooldownTimer > 0) return;
        StartCoroutine(ShotgunAltAttack());
    }
    public override void ReleaseAltAttack()
    {
        altCooldownTimer = 0;
    }

    public override string GetAmmoText() => Mathf.Floor(ammo).ToString();
    public override string GetAltAmmoText() => altAmmo > 0 ? altAmmo.ToString() : "";
}