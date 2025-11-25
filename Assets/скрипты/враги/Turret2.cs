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
    float lastLineTime;

    protected override void Awake()
    {
        base.Awake();
        lastAttackTime = 0f;
    }

    protected override void Update()
    {
        base.Update();
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < attackRange && !HasEffect(EffectType.Freeze))
        {
            if (Time.time > lastAttackTime + attackCooldown && Time.time > lastLineTime + attackCooldown) Attack();
            else if (Time.time > lastAttackTime + (attackCooldown / 2) || Time.time > lastAttackTime + (attackCooldown / 2)) Line();
        }
        else lastLineTime = Time.time;

        ApplyEffects();
    }

    private void Attack()
    {
        Vector3 rayDirection = (Health.Instance.head.transform.position - rayStart.position).normalized;
        RaycastHit hit;

        bool hitSomething = Physics.Raycast(rayStart.position, rayDirection, out hit, attackRange);
        if (hitSomething && hit.collider.tag == "Player" && Health.Instance.health >= 0)
        {
            Health.Instance.Damage(Health.Instance.head.transform.position, attackDamage);
            LineRenderer lr = Manager.Instance.GetLineRenderer(new Color(0, 255, 0));
            Manager.Instance.ShowLineRenderer(rayStart.position, player.position, lr, Manager.Instance.small);

            rayStart.LookAt(player);
            rayStart.rotation = Quaternion.Euler(rayStart.rotation.eulerAngles.x - 90, rayStart.rotation.eulerAngles.y, rayStart.rotation.eulerAngles.z);
            particle.Play();
        }
        else lastLineTime = Time.time;
        lastAttackTime = Time.time;
    }
    
    private void Line()
    {
        Vector3 rayDirection = (Health.Instance.head.transform.position - rayStart.position).normalized;
        RaycastHit hit;

        bool hitSomething = Physics.Raycast(rayStart.position, rayDirection, out hit, attackRange);
        if (hitSomething && hit.collider.tag == "Player" && Health.Instance.health >= 0)
        {    
            LineRenderer lr = Manager.Instance.GetLineRenderer(new Color(0, 0, 0));
            Manager.Instance.ShowLineRenderer(rayStart.position, player.position, lr);

            rayStart.LookAt(player);
            rayStart.rotation = Quaternion.Euler(rayStart.rotation.eulerAngles.x - 90, rayStart.rotation.eulerAngles.y, rayStart.rotation.eulerAngles.z);
        }
    }

    protected override void ApplyEffects()
    {
        base.ApplyEffects();
    }
}