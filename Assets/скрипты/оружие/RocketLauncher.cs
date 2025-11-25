using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class RocketLauncher : WeaponBase
{
    [SerializeField] public float rocketDamage = 50f;
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
                weaponAnimator.SetTrigger("R");
                Manager.Instance.Sound(reload);
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
        weaponAnimator.SetTrigger("1");
        ammo--;
        foreach (var spawn in spawns)
        {
            Manager.Instance.Fire(true, Manager.Instance.rocket, spawn.position, spawn.rotation, camera.forward);
        }
        cooldownTimer = cooldown;
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

            ammoRegenRate = ammoRegenRateBase / (1 + rocketCharge * 0.05f);
            rocketChargeTime = rocketChargeTimeBase / (1 + rocketCharge * 0.01f);
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
                weaponAnimator.SetTrigger("2");
            }
            else
            {
                Manager.Instance.Sound(primary);
                Shake(0.5f, 1);
                weaponAnimator.SetTrigger("1");
            }
        }
        float maxSpread = 0.2f;
        float spread = Mathf.Lerp(10.1f, maxSpread, Mathf.Min(rocketCharge / 10f, 1f));
        foreach (var spawn in spawns)
        {
            for (int i = 0; i < rocketCharge; i++)
            {
                Rocket rocket = Manager.Instance.Fire(true, Manager.Instance.rocket, spawn.position, spawn.rotation, camera.forward, 15, spread).GetComponent<Rocket>();
                rocket.rocketHit = false;
            }
        }

        Particle(rocketCharge / 5f);

        rocketCharge = 0;
        ammoRegenRate = ammoRegenRateBase;
    }

    public override string GetAmmoText() => ammo.ToString();
    public override string GetAltAmmoText() => rocketCharge.ToString();
}