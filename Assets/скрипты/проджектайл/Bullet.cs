using System.Collections;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

public class Bullet : Projectile
{
    bool crit = false;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void Fire(bool? isPlayer, Vector3 direction, float speed = 15, float spread = 0, List<EffectType> effectTypes = null, bool isCrit = false)
    {
        player = isPlayer;
        activeEffects = effectTypes;
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.AddForce((direction * speed) + (Vector3.Dot(Manager.Instance.rb.velocity, transform.forward) * transform.forward), ForceMode.VelocityChange);
        Physics.IgnoreCollision(rb.GetComponent<Collider>(), GetComponent<Collider>());
        crit = isCrit;

        Invoke("Destroy", 25);
    }

    void Update()
    {
        if (rb != null)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 0.1f);
            foreach (var hit in hits)
            {
                if (hit.isTrigger) return;

                if (player != null)
                {
                    if (hit.tag == "Player" && !player.Value)
                    {
                        Health.Instance.Damage(transform.position, crit ? 50 : 20);
                        transform.SetParent(hit.transform);
                        Destroy(rb);
                        Invoke("Destroy", 10);
                    }
                }
                
                if (hit.tag == "Enemy")
                {
                    if (player != null) if (!player.Value) continue;

                    Enemy enemy = hit.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.Damage(crit ? 50 : 20, crit, transform.position);
                        transform.SetParent(hit.transform);
                        Destroy(rb);
                    }
                }
            }
        }
    }

    void Destroy()
    {
        if (rb != null || !player.Value)
        {
            Quaternion rot = rb != null ? Quaternion.LookRotation(rb.velocity) : Quaternion.LookRotation(GameObject.FindWithTag("Player").transform.position - transform.position);
            ParticleSystem sparksParticles = Instantiate(Manager.Instance.friction, transform.position, rot).GetComponent<ParticleSystem>();
            sparksParticles.Play();
            Destroy(sparksParticles.gameObject, 1);
            Destroy(gameObject);
        }
    }
}
