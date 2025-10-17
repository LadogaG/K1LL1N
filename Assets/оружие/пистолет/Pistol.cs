using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using TMPro;

public class Pistol : WeaponBase
{
    [SerializeField] private float pistolDamage = 15f;
    [SerializeField] private float pistolAltChargeTimeBase = 1f;
    private float pistolAltChargeTimeBaseStart = 1f;
    [SerializeField] public float bulletSpeed = 20f;
    [SerializeField] public float lineFadeTime = 0.5f;
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
        Debug.Log($"[Pistol] Found {spawns.Length} spawn points");
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
        if (weaponAnimator != null)
        {
            weaponAnimator.SetTrigger("пистолет 1");
            Debug.Log("[Pistol] Normal attack trigger set");
        }
        foreach (var spawn in spawns)
        {
            Vector3 rayOrigin = spawn.position;
            Vector3 rayDirection = camera.forward;
            RaycastHit weaponHit;
            Vector3 currentOrigin = rayOrigin;
            bool hitSomething = Physics.Raycast(currentOrigin, rayDirection, out weaponHit, Mathf.Infinity);
            
            // Игнорируем попадания в объекты с тегом "Player", удлиняя рейкаст
            while (hitSomething && weaponHit.collider.tag == "Player")
            {
                // Продолжаем рейкаст от точки попадания с небольшим смещением
                currentOrigin = weaponHit.point + rayDirection * 0.1f; // 0.01f - маленький offset, чтобы избежать повторного попадания
                hitSomething = Physics.Raycast(currentOrigin, rayDirection, out weaponHit, Mathf.Infinity);
            }
            
            Vector3 endPoint = hitSomething ? weaponHit.point : currentOrigin + rayDirection * 100f;
            LineRenderer pistolLr = Weapon.Instance.GetLineRenderer(new Color(Random.value, Random.value, Random.value));

            Weapon.Instance.StartCoroutine(ShowRaycastLine(rayOrigin, endPoint, pistolLr));
            if (hitSomething)
            {
                Enemy enemy = weaponHit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    Manager.Instance.Flash();
                    enemy.Damage(pistolDamage, false, weaponHit.point);
                    intensity = 1;
                    gradientSpeed += 2;
                    Debug.Log($"[Pistol] Hit enemy: {weaponHit.collider.name}, damage: {pistolDamage}");
                }
            }
            Debug.Log($"[Pistol] Shot from spawn, hit: {(hitSomething ? weaponHit.point.ToString() : "nothing")}");
        }
        cooldownTimer = cooldown;
    }
    public override void ReleasePrimaryAttack() { }

    private IEnumerator PistolAltAttack()
    {
        if (pistolCharge == 0)
        {
            Debug.LogWarning("[Pistol] No charge!");
            yield break;
        }

        List<Enemy> enemies = new List<Enemy>(FindObjectsOfType<Enemy>());

        // Сортируем врагов по расстоянию от игрока
        enemies.Sort((e1, e2) => Vector3.Distance(transform.position, e1.transform.position).CompareTo(
            Vector3.Distance(transform.position, e2.transform.position)));

        int enemyCount = enemies.Count;
        int shotsFired = 0;
        float delay = 0.1f;
        int Charges = pistolCharge;
        
        for (int i = 0; i < Charges; i++)
        {
            if (enemyCount > 0)
            {
                // Выбираем врага (циклически проходим по отсортированному списку)
                Enemy targetEnemy = enemies[i % enemyCount];

                // Выбираем случайную позицию из spawns для каждого выстрела
                Vector3 rayOrigin = spawns[Random.Range(0, spawns.Length)].position;

                bool crit = pistolCharge == 1 || pistolCharge == Charges;

                // Наносим урон
                targetEnemy.Damage(crit ? (pistolDamage + pistolCharge - 1) * 2 : pistolDamage + pistolCharge, crit);

                // Создаем визуальный эффект
                LineRenderer lr = Weapon.Instance.GetLineRenderer(new Color(Random.value, Random.value, Random.value));
                Weapon.Instance.StartCoroutine(ShowRaycastLine(rayOrigin, targetEnemy.transform.position, lr));

                if (pistolCharge == 1) Debug.Log($"[Pistol] Alt attack hit enemy: {targetEnemy.name}, damage: {pistolDamage * 2}, {pistolCharge}");
                else Debug.Log($"[Pistol] Alt attack hit enemy: {targetEnemy.name}, damage: {pistolDamage}, {pistolCharge}");
                shotsFired++;

                if (weaponAnimator != null)
                {
                    weaponAnimator.SetBool("пистолет 2", false);
                    weaponAnimator.SetTrigger("пистолет 3");
                    Debug.Log("[Pistol] Charge stopped, 'пистолет 3' triggered");
                }

                // Задержка
                yield return new WaitForSeconds(delay);

                // Уменьшаем задержку до 0.001
                if (delay > 0.001f)
                {
                    delay = Mathf.Max(0.001f, delay - (0.1f / 100));
                }

                enemies = new List<Enemy>(FindObjectsOfType<Enemy>());

                enemies.Sort((e1, e2) => Vector3.Distance(transform.position, e1.transform.position).CompareTo(
                Vector3.Distance(transform.position, e2.transform.position)));

                enemyCount = enemies.Count;

                Manager.Instance.Sound(primary);
                Particle(0.1f);
                Shake(0.2f, 0.5f);
                Manager.Instance.Flash();
                intensity = 1;
                gradientSpeed++;
                pistolCharge--;
            }

        }
        Manager.Instance.Sound(alt);
        Particle(Charges / 5f);
        pistolCharge = 0;
        Debug.Log($"[Pistol] Fired {shotsFired} alt attacks");
        yield return new WaitForSeconds(0.3f);
    }


    private IEnumerator ShowRaycastLine(Vector3 start, Vector3 end, LineRenderer lr)
    {
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        float elapsed = 0f;
        ParticleSystem sparksParticles = Instantiate(Manager.Instance.sparks, end, Quaternion.LookRotation(start - end)).GetComponent<ParticleSystem>();
        sparksParticles.Play();
        Destroy(sparksParticles.gameObject, 5);
        while (elapsed < lineFadeTime)
        {
            elapsed += Time.deltaTime;
            lr.startWidth /= ((elapsed + 1) / lineFadeTime) + 1;
            lr.endWidth /= ((elapsed + 1) / lineFadeTime) + 1;
            yield return null;
        }
        Destroy(lr.gameObject);
        Debug.Log("[Weapon] LineRenderer faded out");
    }

    public override void StartAltAttack()
    {
        if (altCooldownTimer > 0) return;
        isChargingPistol = true;
        pistolChargeTimer = 0f;
        if (weaponAnimator != null)
        {
            weaponAnimator.SetBool("пистолет 2", true);
            Debug.Log("[Pistol] Charge animation 'пистолет 2' started");
        }
        Weapon.Instance.StartCoroutine(AltAttackStartPistolCharge());
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
            Debug.Log($"[Pistol] Charge increased to: {pistolCharge}");
            weaponAnimator.SetBool("пистолет 2", true);
        }
    }

    public override void ReleaseAltAttack()
    {
        if (!isChargingPistol) return;
        isChargingPistol = false;
        altCooldownTimer = altCooldown;
        Weapon.Instance.StartCoroutine(PistolAltAttack());
        pistolAltChargeTimeBase = pistolAltChargeTimeBaseStart;
    }

    public override string GetAmmoText() => "";
    public override string GetAltAmmoText() => pistolCharge.ToString();
}