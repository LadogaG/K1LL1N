using UnityEngine;
using System.Collections;

public class Turret2 : Enemy
{
    [Header("Turret Settings")]
    [SerializeField] ParticleSystem particle;
    [SerializeField] float attackDamage = 10f;
    [SerializeField] float attackRange = 15f;
    [SerializeField] float attackCooldown = 2f;
    [SerializeField] Transform rayStart;
    [SerializeField] float laserFadeTime = 0.5f;
    float lastAttackTime;

    protected override void Awake()
    {
        base.Awake();
        enemyName = "Turret";
        lastAttackTime = 0f;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < attackRange && Time.time > lastAttackTime + attackCooldown && !HasEffect(EffectType.Freeze))
        {
            Attack();
        }

        ApplyEffects();
    }

    private void Attack()
    {
        Vector3 rayDirection = (Health.Instance.head.transform.position - rayStart.position).normalized;
        RaycastHit hit;

        // Perform raycast
        bool hitSomething = Physics.Raycast(rayStart.position, rayDirection, out hit, attackRange);
        if (hitSomething && hit.collider.tag == "Player" && Health.Instance.health >= 0)
        {
            Health.Instance.Damage(Health.Instance.head.transform.position, attackDamage);
            for (int i = 0; i < 3; i++)
            {                
                LineRenderer lr = Manager.Instance.GetLineRenderer(new Color(Random.value, Random.value, Random.value));
                Vector3 randomVector = new Vector3(Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f), Random.Range(-0.1f, 0.1f));
                Manager.Instance.StartCoroutine(Manager.Instance.ShowLineRenderer(rayStart.position + randomVector, player.position + randomVector, lr, Manager.Instance.friction));
            }
            rayStart.LookAt(player);
            rayStart.rotation = Quaternion.Euler(rayStart.rotation.eulerAngles.x - 90, rayStart.rotation.eulerAngles.y, rayStart.rotation.eulerAngles.z);
            particle.Play();
        }
        lastAttackTime = Time.time;
    }

    protected override void ApplyEffects()
    {
        base.ApplyEffects();
        // No additional velocity changes needed for turret
    }
}