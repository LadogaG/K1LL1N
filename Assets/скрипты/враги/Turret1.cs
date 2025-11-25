using UnityEngine;
using System.Collections;

public class Turret1 : Enemy
{
    [Header("Turret Settings")]
    [SerializeField] ParticleSystem particle;
    [SerializeField] float attackDamage = 10f;
    [SerializeField] float attackRange = 15f;
    [SerializeField] Transform rayStart;
    float attackCooldown = 2f;
    float lastAttackTime;

    protected override void Awake()
    {
        base.Awake();
        lastAttackTime = 0f;
    }

    protected override void Update()
    {
        base.Update();
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < attackRange && Time.time > lastAttackTime + attackCooldown + (distance / 25) && !HasEffect(EffectType.Freeze))
        {
            Attack();
        }

        ApplyEffects();
    }

    private void Attack()
    {
        Vector3 rayDirection = (player.position - rayStart.position).normalized;
        RaycastHit hit;

        bool hitSomething = Physics.Raycast(rayStart.position, rayDirection, out hit, attackRange);
        if (hitSomething && hit.collider.tag == "Player")
        {
            for (int i = 0; i < 3; i++)
            {
                LineRenderer lr = Manager.Instance.GetLineRenderer(new Color(Random.value, Random.value, Random.value));
                float f = 0.1f;
                Vector3 randomVector = new Vector3(Random.Range(-f, f), Random.Range(-f, f), Random.Range(-f, f));
                Manager.Instance.ShowLineRenderer(rayStart.position + randomVector, player.position + randomVector, lr, Manager.Instance.small);
                Health.Instance.Damage(player.position + (randomVector * 5), attackDamage);
            }
            rayStart.LookAt(player);
            rayStart.rotation = Quaternion.Euler(rayStart.rotation.eulerAngles.x - 90, rayStart.rotation.eulerAngles.y, rayStart.rotation.eulerAngles.z);
            particle.Play();
            attackCooldown = 0;
        }
        else attackCooldown = 2f;
        lastAttackTime = Time.time;
    }

    protected override void ApplyEffects()
    {
        base.ApplyEffects();
    }
}