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
    private int shotgunPoints;

    [SerializeField] private AudioClip primary;
    [SerializeField] private AudioClip crit;
    [SerializeField] private AudioClip alt;
    public GameObject altParticlePrefab;
    ParticleSystem altParticle;
    public float altParticleTimer;

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

    public override void StartPrimaryAttack() { }
    public override void HoldPrimaryAttack()
    {
        if (cooldownTimer > 0) return;
        bool isCrit = shotgunPoints > 0;
        float finalDamage = shotgunDamage * (isCrit ? shotgunDamageMultiplier : 1f);
        if (isCrit)
        {
            Manager.Instance.Sound(crit);
            Particle(1);
            Shake(0.5f, 1);
            Manager.Instance.rb.AddForce(-camera.forward * 2, ForceMode.Impulse);
            shotgunPoints--;
            Debug.Log($"[Shotgun] Normal attack with damage: {finalDamage}, points left: {shotgunPoints}");
        }
        else
        {
            Manager.Instance.Sound(primary);
            Particle(0.25f);
            Shake(0.2f, 1);
            Manager.Instance.rb.AddForce(-camera.forward, ForceMode.Impulse);
        }
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("дробовик 1");
            Debug.Log("[Shotgun] Normal attack trigger set");
        }
        foreach (var spawn in spawns)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 spread = camera.forward + Random.insideUnitSphere * 0.1f;
                Weapon.Instance.FireBullet(spawn.position, spawn.parent.rotation.eulerAngles, spread, finalDamage, true, isCrit);
            }
        }

        cooldownTimer = cooldown;
        Debug.Log("[Shotgun] Fired 16 bullets");
    }
    public override void ReleasePrimaryAttack() { }

    private IEnumerator ShotgunAltAttack()
    {
        Manager.Instance.Sound(alt);
        Shake(0.1f, 0.5f);
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("дробовик 2");
            Debug.Log("[Shotgun] Alt attack trigger set");
        }
        altCooldownTimer = altCooldown;
        yield return new WaitForSeconds(0.1f);

        Vector3 boxSize = new Vector3(shotgunAltRadius * 2, shotgunAltRadius * 2, shotgunAltRange);
        RaycastHit[] shotgunAltHits = Physics.BoxCastAll(transform.position, boxSize / 2, transform.forward, Quaternion.identity, shotgunAltRange);
        int kills = 0;
        bool isHit = false;
        foreach (var shotgunAltHit in shotgunAltHits)
        {
            Enemy enemy = shotgunAltHit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                isHit = true;
                float oldHealth = enemy.health;
                bool crit = enemy.health < enemy.maxHealth * 0.1f;
                enemy.Damage(crit ? shotgunAltDamage * 20 : shotgunAltDamage, crit, shotgunAltHit.point == Vector3.zero ? enemy.transform.position : shotgunAltHit.point);
                if (oldHealth > 0 && enemy.health <= 0)
                {
                    kills++;
                }
                Debug.Log($"[Shotgun] Alt attack hit enemy: {shotgunAltHit.collider.name}, damage: {shotgunAltDamage}");
            }
        }
        if (isHit) AltParticle(0.1f);
        shotgunPoints += kills;
        Debug.Log($"[Shotgun] Alt attack, kills: {kills}, points: {shotgunPoints}");
    }

    public override void StartAltAttack() { }
    public override void HoldAltAttack()
    {
        if (altCooldownTimer > 0) return;
        Weapon.Instance.StartCoroutine(ShotgunAltAttack());
    }
    public override void ReleaseAltAttack()
    {
        altCooldownTimer = 0;
    }

    public override string GetAmmoText() => "";
    public override string GetAltAmmoText() => shotgunPoints.ToString();
}