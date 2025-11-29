using System.Collections;
using UnityEngine;

public class Turret5 : Enemy
{
    [Header("Turret Settings")]
    [SerializeField] ParticleSystem particle;
    [SerializeField] float attackRange = 15f;
    [SerializeField] float attackCooldown = 2f;
    [SerializeField] Transform rayStart;
    float lastAttackTime;
    protected Transform[] spawns;
    ParticleSystem[] particles;

    protected override void Awake()
    {
        base.Awake();
        lastAttackTime = 0f;

        spawns = gameObject.GetComponentsInChildren<Transform>();
        spawns = System.Array.FindAll(spawns, t => t.name == "spawn");

        particles = new ParticleSystem[spawns.Length];
        for (int i = 0; i < spawns.Length; i++)
        {
            Transform spawn = spawns[i];
            particles[i] = Instantiate(particle, spawn.position, spawn.rotation).GetComponent<ParticleSystem>();
            particles[i].Stop();
        }
    }

    protected override void Update()
    {
        base.Update();
        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < attackRange && Time.time > lastAttackTime + attackCooldown && !HasEffect(EffectType.Freeze))
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
            rayStart.LookAt(Health.Instance.head.transform);
            rayStart.rotation = Quaternion.Euler(rayStart.rotation.eulerAngles.x - 90, rayStart.rotation.eulerAngles.y, rayStart.rotation.eulerAngles.z);
            for (int i = 0; i < spawns.Length; i++)
            {
                Manager.Instance.Fire(false, Manager.Instance.rocket, spawns[i].position, spawns[i].rotation, spawns[i].forward, 20, 0.1f);
                particles[i].transform.position = spawns[i].position;
                particles[i].transform.rotation = spawns[i].rotation;
                particles[i].Play();
            }
        }
        lastAttackTime = Time.time;
    }

    protected override void ApplyEffects()
    {
        base.ApplyEffects();
    }
}