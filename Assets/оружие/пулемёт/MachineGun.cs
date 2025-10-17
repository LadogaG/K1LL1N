using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MachineGun : WeaponBase
{
    [SerializeField] public float machineGunDamage = 8f;
    [SerializeField] float machineGunAltDamageMultiplier = 5f;
    [SerializeField] float ammoRegenRate = 10f;
    float ammoTimer;
    bool ammoRegen = true;

    [SerializeField] AudioClip primary;
    [SerializeField] AudioClip primaryStop;
    [SerializeField] AudioClip alt;
    [SerializeField] AudioClip altStop;

    void Awake()
    {
        base.Initialize();

        ammo = maxAmmo;
        ammoTimer = 0f;
    }

    void Update()
    {
        base.UpdateWeapon();
        ammoTimer += Time.deltaTime;
        if (ammoTimer >= 0.1f)
        {
            if (ammo < maxAmmo && cooldownTimer < 0 && ammoRegen)
            {
                ammo = Mathf.Min(ammo + ammoRegenRate / 10f, maxAmmo);
                Debug.Log($"[MachineGun] Ammo regenerated: {ammo}/{maxAmmo}");
            }
            ammoTimer = 0f;
        }
    }

    public override void StartPrimaryAttack()
    {
        ammo += 1;
        cooldownTimer = 0;
    }
    public override void HoldPrimaryAttack()
    {
        if (cooldownTimer > 0) return;
        if (ammo < 1 && !ammoRegen)
        {
            Weapon.Instance.StartCoroutine(AmmoRegen());
            Manager.Instance.Sound(primaryStop);
        }
        if (ammo < 1) return;
        ammoRegen = false;

        Manager.Instance.Sound(primary);
        Shake(0.05f, 1);
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("пулемёт 1");
            Debug.Log("[MachineGun] Normal attack trigger set");
        }
        ammo -= 1f;
        foreach (var spawn in spawns)
        {
            Weapon.Instance.FireBullet(spawn.position, new Vector3(0, 0, spawn.eulerAngles.z), camera.forward, machineGunDamage, true, false);
        }

        Particle(0.015f);

        cooldownTimer = cooldown;
        Debug.Log($"[MachineGun] Fired, damage: {machineGunDamage}, ammo left: {ammo}");
    }
    public override void ReleasePrimaryAttack()
    {
        Weapon.Instance.StartCoroutine(AmmoRegen());
        Manager.Instance.Sound(primaryStop);
    }


    IEnumerator AmmoRegen()
    {
        yield return new WaitForSeconds(0.5f);
        ammoRegen = true;
    }

    public override void StartAltAttack() { }
    public override void HoldAltAttack()
    {
        if (altCooldownTimer > 0) return;
        if (ammo < 10f)
        {
            Manager.Instance.Sound(altStop);
            return;
        }
        Manager.Instance.Sound(alt);
        Shake(0.1f, 2);
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("пулемёт 2");
            Debug.Log("[MachineGun] Alt attack trigger set");
        }
        ammo -= 5f;
        foreach (var spawn in spawns)
        {
            Weapon.Instance.FireBullet(spawn.position, Vector3.zero, camera.forward, machineGunDamage * machineGunAltDamageMultiplier, true, true);
        }
        
        Particle(0.1f);

        altCooldownTimer = altCooldown;
        Debug.Log($"[MachineGun] Alt attack, damage: {machineGunDamage * machineGunAltDamageMultiplier}, ammo left: {ammo}");
    }
    public override void ReleaseAltAttack()
    {
        Manager.Instance.Sound(primaryStop);
        Manager.Instance.Sound(altStop);
    }
    
    public override string GetAmmoText() => ammo.ToString();
    public override string GetAltAmmoText() => "";
}