using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Throwable : Projectile
{
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public override void Fire(bool? isPlayer, Vector3 direction, float speed = 15, float spread = 0, List<EffectType> effectTypes = null, bool c = false)
    {
        player = isPlayer;
        activeEffects = effectTypes;
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.AddForce((direction * speed) + (Vector3.Dot(Manager.Instance.rb.velocity, transform.forward) * transform.forward), ForceMode.VelocityChange);
        Physics.IgnoreCollision(rb.GetComponent<Collider>(), GetComponent<Collider>());
    }

    void Update()
    {
        Collider[] hits = Physics.OverlapSphere(gameObject.transform.position, 0.5f);
        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject || hit.isTrigger) return;
            if (player != null)
            {
                if (hit.tag == "Player" && player.Value) return;
                Explode();
            }
        }
    }

    public void Explode()
    {
        Destroy(rb);

        Collider[] targets = Physics.OverlapSphere(gameObject.transform.position, 1);
        foreach (var target in targets)
        {
            if (player == null && target.tag == "Player") Health.Instance.Damage(transform.position, 25);
            if (player == null && target.tag == "Enemy") target.GetComponent<Enemy>().Damage(25, false, gameObject.transform.position);
            if (player != null && !player.Value && target.tag == "Player") Health.Instance.Damage(transform.position, 25);
            if (player != null && !player.Value && target.tag == "Enemy") target.GetComponent<Enemy>().Damage(5, false, gameObject.transform.position);
            if (player != null && player.Value && target.tag == "Enemy") target.GetComponent<Enemy>().Damage(25, false, gameObject.transform.position);

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
                trb.AddExplosionForce(25, gameObject.transform.position, 1, 0f, ForceMode.Impulse);
            }
        }

        ParticleSystem detonate = Instantiate(Manager.Instance.smallExp, transform.position, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
        detonate.Play();
        Destroy(detonate.gameObject, 5);

        AudioClip stepType = Manager.Instance.hitSound;
        if (transform.tag != "Untagged")
        {
            switch (transform.tag)
            {
                case "Enemy": stepType = Manager.Instance.damageSound; break;
                case "Grass": stepType = Manager.Instance.grassSound; break;
                case "Glass": stepType = Manager.Instance.glassSound; break;
                case "Metal": stepType = Manager.Instance.metalSound; break;
            }
        }
        Manager.Instance.Sound(stepType);

        Destroy(gameObject);
    }
}
