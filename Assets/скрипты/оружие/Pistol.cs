using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

public class Pistol : WeaponBase
{
    [SerializeField] private float damage = 20;
    [SerializeField] private float altDamage = 10;
    [SerializeField] private float pistolAltChargeTimeBase = 1;
    private float pistolAltChargeTimeBaseStart = 1;
    private float pistolChargeTimer;
    private int pistolCharge;
    private bool isChargingPistol;

    [SerializeField] private AudioClip primary;
    [SerializeField] private AudioClip alt;
    [SerializeField] private AudioClip reload;
    Material material;
    float intensity = 0.3f;
    float minIntensity = 0.3f;
    float gradientSpeed = 1;
    float minGradientSpeed = 1;

    void Awake()
    {
        base.Initialize();
        material = GetComponent<Renderer>().sharedMaterial;
        pistolAltChargeTimeBaseStart = pistolAltChargeTimeBase;
    }

    void Update()
    {
        base.UpdateWeapon();
        if (intensity > minIntensity) intensity -= 0.05f;
        if (gradientSpeed > minGradientSpeed) gradientSpeed -= 0.05f;
        material.SetFloat("_OverlayIntensity", intensity);
        material.SetFloat("_GradientSpeed", gradientSpeed);
    }

    public override void StartPrimaryAttack()
    {
        cooldownTimer = 0;
    }
    public override void HoldPrimaryAttack()
    {
        if (cooldownTimer > 0) return;
        Manager.Instance.Sound(primary);
        Particle(0.1f);
        Shake(0.1f, 0.3f);
        Manager.Instance.Flash(0.3f);
        weaponAnimator.SetTrigger("1");
        foreach (var spawn in spawns)
        {
            Vector3 rayOrigin = spawn.position;
            Vector3 rayDirection = camera.forward;
            RaycastHit weaponHit;
            Vector3 currentOrigin = rayOrigin;
            bool hitSomething = Physics.Raycast(currentOrigin, rayDirection, out weaponHit, Mathf.Infinity);
            
            while (hitSomething && (weaponHit.collider.tag == "Player" || weaponHit.collider.isTrigger))
            {
                currentOrigin = weaponHit.point + rayDirection * 0.1f;
                hitSomething = Physics.Raycast(currentOrigin, rayDirection, out weaponHit, Mathf.Infinity);
            }
            
            Vector3 endPoint = hitSomething ? weaponHit.point : currentOrigin + rayDirection * 100f;
            LineRenderer lr = Manager.Instance.GetLineRenderer(new Color(Random.value, Random.value, Random.value));

            Manager.Instance.ShowLineRenderer(rayOrigin, endPoint, lr, Manager.Instance.sparks);
            if (hitSomething)
            {
                Enemy enemy = weaponHit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    Manager.Instance.Flash();
                    enemy.Damage(damage, false, weaponHit.point);
                    intensity = 1;
                    gradientSpeed += 2;
                }
                else
                {
                    Rocket rocket = weaponHit.collider.GetComponent<Rocket>();
                    if (rocket != null)
                    {
                        Manager.Instance.Flash();
                        rocket.Explode(3);
                        intensity = 1;
                        gradientSpeed += 2;
                    }
                }
            }
        }
        cooldownTimer = cooldown;
    }
    public override void ReleasePrimaryAttack() { }

    private IEnumerator PistolAltAttack()
    {
        if (pistolCharge == 0)
        {
            yield break;
        }

        List<Enemy> enemies = new List<Enemy>(FindObjectsOfType<Enemy>());

        enemies.Sort((e1, e2) => Vector3.Distance(transform.position, e1.transform.position).CompareTo(
            Vector3.Distance(transform.position, e2.transform.position)));

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Vector3 rayOrigin = transform.position;
            Vector3 rayDirection = (enemies[i].transform.position - rayOrigin).normalized;
            RaycastHit weaponHit;
            Vector3 currentOrigin = rayOrigin;
            bool hitSomething = Physics.Raycast(currentOrigin, rayDirection, out weaponHit, Mathf.Infinity);
            
            while (hitSomething && (weaponHit.collider.tag == "Player" || weaponHit.collider.isTrigger))
            {
                currentOrigin = weaponHit.point + rayDirection * 0.1f;
                hitSomething = Physics.Raycast(currentOrigin, rayDirection, out weaponHit, Mathf.Infinity);
            }
            
            if (!hitSomething || weaponHit.collider.GetComponent<Enemy>() != enemies[i])
            {
                enemies.RemoveAt(i);
            }
        }

        int enemyCount = enemies.Count;
        int shotsFired = 0;
        float delay = 0.1f;
        int Charges = pistolCharge;
        
        for (int i = 0; i < Charges; i++)
        {
            if (enemyCount > 0)
            {
                Enemy targetEnemy = enemies[i % enemyCount];
                Vector3 rayOrigin = spawns[Random.Range(0, spawns.Length)].position;
                
                bool crit = pistolCharge == 1 || pistolCharge == Charges;
                targetEnemy.Damage(crit ? (altDamage + pistolCharge - 1) * 2 : altDamage + pistolCharge, crit);
                LineRenderer lr = Manager.Instance.GetLineRenderer(new Color(Random.value, Random.value, Random.value));
                Manager.Instance.ShowLineRenderer(rayOrigin, targetEnemy.transform.position, lr, Manager.Instance.sparks);

                shotsFired++;
                weaponAnimator.SetBool("2", false);
                weaponAnimator.SetTrigger("R");
                Manager.Instance.Sound(primary);
                Particle(0.1f);
                Shake(0.2f, 0.5f);
                Manager.Instance.Flash();
                intensity = 1;
                gradientSpeed++;
                pistolCharge--;

                yield return new WaitForSeconds(delay);

                if (delay > 0.001f)
                {
                    delay = Mathf.Max(0.001f, delay - (0.1f / 100));
                }

                enemies = new List<Enemy>(FindObjectsOfType<Enemy>());

                enemies.Sort((e1, e2) => Vector3.Distance(transform.position, e1.transform.position).CompareTo(
                    Vector3.Distance(transform.position, e2.transform.position)));

                for (int j = enemies.Count - 1; j >= 0; j--)
                {
                    rayOrigin = transform.position;
                    Vector3 rayDirection = (enemies[j].transform.position - rayOrigin).normalized;
                    RaycastHit weaponHit;
                    Vector3 currentOrigin = rayOrigin;
                    bool hitSomething = Physics.Raycast(currentOrigin, rayDirection, out weaponHit, Mathf.Infinity);
                    
                    while (hitSomething && (weaponHit.collider.tag == "Player" || weaponHit.collider.isTrigger))
                    {
                        currentOrigin = weaponHit.point + rayDirection * 0.1f;
                        hitSomething = Physics.Raycast(currentOrigin, rayDirection, out weaponHit, Mathf.Infinity);
                    }
                    
                    if (!hitSomething || weaponHit.collider.GetComponent<Enemy>() != enemies[j])
                    {
                        enemies.RemoveAt(j);
                    }
                }
                enemyCount = enemies.Count;
            }
        }
        Manager.Instance.Sound(alt);
        Particle(Charges / 5f);
        pistolCharge = 0;
        yield return new WaitForSeconds(0.3f);
    }

    public override void StartAltAttack()
    {
        if (altCooldownTimer > 0) return;
        isChargingPistol = true;
        pistolChargeTimer = 0f;
        weaponAnimator.SetBool("2", true);
        StartCoroutine(AltAttackStartPistolCharge());
    }

    private IEnumerator AltAttackStartPistolCharge()
    {
        yield return new WaitForSeconds(0.5f);
        Manager.Instance.Sound(reload);
        pistolCharge = 1;
        Shake(0.1f, 0.5f);
    }

    public override void HoldAltAttack()
    {
        if (!isChargingPistol) return;
        pistolChargeTimer += Time.deltaTime;
        if (pistolChargeTimer >= pistolAltChargeTimeBase)
        {
            pistolCharge++;
            pistolAltChargeTimeBase = Mathf.Lerp(pistolAltChargeTimeBaseStart, 0.001f, pistolCharge / 100f);
            pistolChargeTimer = 0f;
            Manager.Instance.Sound(reload);
            Shake(0.1f, 0.5f);
            if (intensity < 0.5) intensity += 0.5f;
            gradientSpeed++;
            weaponAnimator.SetBool("2", true);
        }
    }

    public override void ReleaseAltAttack()
    {
        if (!isChargingPistol) return;
        isChargingPistol = false;
        altCooldownTimer = altCooldown;
        StartCoroutine(PistolAltAttack());
        pistolAltChargeTimeBase = pistolAltChargeTimeBaseStart;
    }

    public override string GetAmmoText() => "";
    public override string GetAltAmmoText() => pistolCharge.ToString();
}