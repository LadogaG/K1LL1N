using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rocket : Projectile
{
    [SerializeField] ParticleSystem particle;
    public bool rocketHit = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (player != null)
        {
            particle.Play();
            rb.useGravity = false;
        }
        else particle.Stop();
    }

    public override void Fire(bool? isPlayer, Vector3 direction, float speed = 15, float spread = 0, List<EffectType> effectTypes = null, bool c = false)
    {
        player = isPlayer;
        activeEffects = effectTypes;
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.AddForce((direction * speed) + (Vector3.Dot(Manager.Instance.rb.velocity, transform.forward) * transform.forward), ForceMode.VelocityChange);
        Physics.IgnoreCollision(rb.GetComponent<Collider>(), GetComponent<Collider>());

        if (player != null)
        {
            particle.Play();
            rb.useGravity = false;
        }
        else particle.Stop();
    }

    void Update()
    {
        Collider[] hits = Physics.OverlapSphere(gameObject.transform.position, 0.5f);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject || hit.isTrigger) return;
            if (player == null && (hit.GetComponent<Rigidbody>() != null || hit.tag == "Enemy") && hit.gameObject != Manager.Instance.player)
            {
                if (hit.tag == "Rocket" && rocketHit) Explode(3);
                else Explode(2);
            }
            if (player != null)
            {
                if (hit.tag == "Player" && player.Value) return;
                Explode();
            }
        }
    }

    public void Explode(int multiplier = 1)
    {
        Destroy(rb);

        Collider[] targets = Physics.OverlapSphere(gameObject.transform.position, 3 * multiplier);
        foreach (var target in targets)
        {
            if (player == null && target.tag == "Player") Health.Instance.Damage(transform.position, 50 * multiplier);
            if (player == null && target.tag == "Enemy") target.GetComponent<Enemy>().Damage(50 * multiplier, false, gameObject.transform.position);
            if (player != null && !player.Value && target.tag == "Player") Health.Instance.Damage(transform.position, 50 * multiplier);
            if (player != null && !player.Value && target.tag == "Enemy") target.GetComponent<Enemy>().Damage(5 * multiplier, false, gameObject.transform.position);
            if (player != null && player.Value && target.tag == "Enemy") target.GetComponent<Enemy>().Damage(25 * multiplier, false, gameObject.transform.position);

            if (target.tag == "Bullet")
            {
                List<Enemy> enemies = new List<Enemy>(FindObjectsOfType<Enemy>());
        
                enemies.Sort((e1, e2) => Vector3.Distance(transform.position, e1.transform.position).CompareTo(
                    Vector3.Distance(transform.position, e2.transform.position)));
        
                if (enemies.Count > 0)
                {
                    Enemy e = enemies[Random.Range(0, enemies.Count)];
            
                    if (e != null)
                    {
                        Manager.Instance.Fire(true, Manager.Instance.bullet, target.transform.position, Quaternion.LookRotation(e.transform.position - target.transform.position), e.transform.position - target.transform.position);
                        Destroy(target.gameObject);
                    }
                }
            }
            
            Rigidbody trb = target.GetComponent<Rigidbody>();
            if (trb != null)
            {
                trb.AddExplosionForce(50 * multiplier, gameObject.transform.position, 3 * multiplier, 0f, ForceMode.Impulse);
            }
        }

        for (int i = 0; i <= multiplier*2; i++)
        {            
            float f = multiplier - 1;
            Vector3 randomVector = new Vector3(Random.Range(-f, f), Random.Range(-f, f), Random.Range(-f, f));
            ParticleSystem detonate = Instantiate(Manager.Instance.explosion, gameObject.transform.position + randomVector, Manager.Instance.explosion.transform.rotation).GetComponent<ParticleSystem>();
            detonate.Play();
            Destroy(detonate.gameObject, 5);
        }

        Destroy(gameObject);
    }
}
