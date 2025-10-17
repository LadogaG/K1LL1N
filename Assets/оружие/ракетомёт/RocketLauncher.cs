using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class RocketLauncher : WeaponBase
{
    [SerializeField] public float rocketDamage = 50f;
    [SerializeField] public float rocketExplosionRadius = 3f;
    [SerializeField] public float rocketSpeed = 15f;
    [SerializeField] public GameObject rocketPrefab;
    [SerializeField] float ammoRegenRate = 1f;
    [SerializeField] float rocketChargeTime = 0.5f;
    [SerializeField] float rocketChargeTimeMax = 1.5f;
    [SerializeField] int rocketChargeThreshold = 10;
    float ammoTimer;
    int rocketCharge;
    float rocketChargeTimer;
    bool isChargingRockets;
    float ammoRegenRateBase;
    float rocketChargeTimeBase;
    readonly List<GameObject> activeRockets = new List<GameObject>();

    [SerializeField] AudioClip primary;
    [SerializeField] AudioClip alt;
    [SerializeField] AudioClip altReload;
    [SerializeField] AudioClip reload;
    [SerializeField] Material material;
    float gradientSpeed = 0.2f;
    float minGradientSpeed = 0.2f;

    void Awake()
    {
        base.Initialize();
        ammo = maxAmmo;
        ammoTimer = 0f;
        ammoRegenRateBase = ammoRegenRate;
        rocketChargeTimeBase = rocketChargeTime;
    }

    void Update()
    {
        base.UpdateWeapon();
        ammoTimer += Time.deltaTime;
        if (ammoTimer >= ammoRegenRate)
        {
            if (ammo < maxAmmo && cooldownTimer < 0)
            {
                ammo = Mathf.Min(ammo + 1, maxAmmo);
                weaponAnimator.SetTrigger("ракетомёт 3");
                Manager.Instance.Sound(reload);
                Debug.Log($"[RocketLauncher] Ammo regenerated: {ammo}/{maxAmmo}");
            }
            ammoTimer = 0f;
        }
        if (gradientSpeed > minGradientSpeed) gradientSpeed -= 0.05f;
        material.SetFloat("_GradientSpeed", gradientSpeed);
    }

    public override void StartPrimaryAttack()
    {
        cooldownTimer = 0;
    }
    public override void HoldPrimaryAttack()
    {
        if (cooldownTimer > 0 || ammo < 1) return;
        Particle(0.1f);
        Manager.Instance.Sound(primary);
        Shake(0.5f, 0.5f);
        gradientSpeed++;
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("ракетомёт 1");
            Debug.Log("[RocketLauncher] Normal attack trigger set");
        }
        ammo--;
        foreach (var spawn in spawns)
        {
            Weapon.Instance.FireSingleRocket(camera.forward, 0f, spawn.position);
        }
        cooldownTimer = cooldown;
        Debug.Log($"[RocketLauncher] Fired from {spawns.Length} spawns, ammo left: {ammo}");
    }
    public override void ReleasePrimaryAttack() { }

    public override void StartAltAttack()
    {
        if (altCooldownTimer > 0) return;
        isChargingRockets = true;
        rocketChargeTimer = 0f;
        if (ammo > 0)
        {
            rocketCharge = 1;
            ammo--;
        }

        ammoRegenRate /= 2;
    }

    public override void HoldAltAttack()
    {
        if (!isChargingRockets) return;
        rocketChargeTimer += Time.deltaTime;
        float chargeTime = rocketCharge < rocketChargeThreshold
            ? rocketChargeTime
            : Mathf.Lerp(rocketChargeTime, rocketChargeTimeMax, (rocketCharge - rocketChargeThreshold) / (float)(int.MaxValue - rocketChargeThreshold));
        if (rocketChargeTimer >= chargeTime && ammo > 0)
        {
            rocketCharge++;
            ammo--;
            rocketChargeTimer = 0f;
            Manager.Instance.Sound(altReload);

            // Увеличиваем скорость перезарядки и скорость увеличения rocketCharge
            ammoRegenRate = ammoRegenRateBase / (1 + rocketCharge * 0.05f); // Пример увеличения скорости перезарядки
            rocketChargeTime = rocketChargeTimeBase / (1 + rocketCharge * 0.01f); // Пример увеличения скорости увеличения rocketCharge

            Debug.Log($"[RocketLauncher] Charge increased to: {rocketCharge}");
        }
    }

    public override void ReleaseAltAttack()
    {
        if (!isChargingRockets) return;
        isChargingRockets = false;
        altCooldownTimer = altCooldown;
        if (weaponAnimator != null && rocketCharge > 0)
        {
            if (rocketCharge != 1)
            {
                Manager.Instance.Sound(alt);
                Shake(1, 1);
                gradientSpeed++;
                weaponAnimator.SetTrigger("ракетомёт 2");
            }
            else
            {
                Manager.Instance.Sound(primary);
                Shake(0.5f, 1);
                weaponAnimator.SetTrigger("ракетомёт 1");
            }

            Debug.Log("[RocketLauncher] Charge stopped, 'ракетомёт 2' triggered");
        }
        float maxSpread = 0.2f;
        float spread = Mathf.Lerp(10.1f, maxSpread, Mathf.Min(rocketCharge / 10f, 1f));
        foreach (var spawn in spawns)
        {
            for (int i = 0; i < rocketCharge; i++)
            {
                Weapon.Instance.FireSingleRocket(camera.forward, spread, spawn.position);
            }
        }

        Particle(rocketCharge / 5f);

        Debug.Log($"[RocketLauncher] Fired {rocketCharge} charged rockets with spread {spread}, ammo left: {ammo}");
        rocketCharge = 0;
        ammoRegenRate = ammoRegenRateBase; // Сбрасываем скорость перезарядки
    }

    public override string GetAmmoText() => ammo.ToString();
    public override string GetAltAmmoText() => rocketCharge.ToString();
}