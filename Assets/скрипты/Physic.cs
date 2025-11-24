using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Physic : MonoBehaviour
{
    AudioSource source;
    Rigidbody rb;
    ParticleSystem sparks;
    ParticleSystem explosion;
    ParticleSystem friction;
    ParticleSystem walk;
    ParticleSystem step;
    ParticleSystem land;
    string lastTag;
    bool isGrounded;
    TrailRenderer trailRenderer;

    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.spatialBlend = 0.9f;
        source.minDistance = 5;
        rb = GetComponent<Rigidbody>();
        InvokeRepeating("FootStep", 0, 0.5f);
        InvokeRepeating("Fall", 0, 1f);
    }

    void FixedUpdate()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        else
        {
            float radius = transform.localScale.x / 2;
            float rayDistance = new Vector3(transform.localScale.x, 0, transform.localScale.z).magnitude / 2f;
            RaycastHit[] hits = new RaycastHit[10];
            int hitCount = Physics.CapsuleCastNonAlloc(transform.position, transform.position + rb.velocity.normalized * rayDistance, radius, rb.velocity.normalized, hits, rayDistance);

            if (rb.velocity.magnitude > 1)
            {
                for (int i = 0; i < hitCount; i++)
                {
                    var hit = hits[i];
                    if (hit.collider.isTrigger) continue;
                    if (hit.transform != transform && hit.point != Vector3.zero && hit.transform.tag != "Bullet")
                    {
                        AudioClip stepType = Manager.Instance.hitSound;
                        if (hit.transform.tag != "Untagged")
                        {
                            switch (hit.transform.tag)
                            {
                                case "Enemy": stepType = Manager.Instance.damageSound; break;
                                case "Grass": stepType = Manager.Instance.grassSound; break;
                                case "Metal": stepType = Manager.Instance.metalSound; break;
                            }
                        }
                        Manager.Instance.Sound(stepType, rb.velocity.magnitude / 10, source);

                        if (friction == null)
                        {
                            friction = Instantiate(Manager.Instance.friction, hit.point, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                            friction.transform.SetParent(transform, false);
                            friction.name = "Friction";
                        }
                        else
                        {
                            friction.transform.position = hit.point;
                            friction.transform.rotation = Quaternion.LookRotation(rb.velocity);
                        }
                        friction.Play();

                        if (rb.velocity.magnitude > 10)
                        {
                            if ((gameObject.tag == "Player" && rb.velocity.magnitude > 25) || gameObject.tag != "Player")
                            {                                
                                if (sparks == null)
                                {
                                    sparks = Instantiate(Manager.Instance.sparks, hit.point, Quaternion.LookRotation(transform.position - hit.point)).GetComponent<ParticleSystem>();
                                    sparks.transform.SetParent(transform, false);
                                    sparks.name = "Spark";
                                }
                                else
                                {
                                    sparks.transform.position = hit.point;
                                    sparks.transform.rotation = Quaternion.LookRotation(transform.position - hit.point);
                                }
                                sparks.Play();
                                Manager.Instance.Sound(Manager.Instance.sparksSound, 1, source);
                            }
                        }
                        else if (rb.velocity.magnitude > 100)
                        {
                            if (explosion == null)
                            {
                                explosion = Instantiate(Manager.Instance.explosion, hit.point, Manager.Instance.explosion.transform.rotation).GetComponent<ParticleSystem>();
                                explosion.transform.SetParent(transform, false);
                                explosion.name = "Explosion";
                            }
                            else
                            {
                                explosion.transform.position = hit.point;
                            }
                            explosion.Play();
                            Collider[] targets = Physics.OverlapSphere(transform.position, 3);
                            foreach (var target in targets)
                            {
                                Rigidbody rb = target.GetComponent<Rigidbody>();
                                if (rb != null)
                                {
                                    rb.AddExplosionForce(10, transform.position, 3, 0, ForceMode.Impulse);
                                }
                            }
                        }
                    }
                }
                if (new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude > 1)
                {
                    RaycastHit[] footHits = Physics.RaycastAll(transform.position, Vector3.down, (transform.localScale.y * 1.5f) + 0.1f);
                    foreach (var hit in footHits)
                    {
                        if (hit.collider.isTrigger || hit.transform == transform) continue;
                        ParticleSystem stepType = Manager.Instance.walk;
                        if (hit.transform.tag != "Untagged")
                        {
                            switch (hit.transform.tag)
                            {
                                case "Grass": stepType = Manager.Instance.grassSparks; break;
                                case "Metal": stepType = Manager.Instance.metalSparks; break;
                            }
                        }

                        if (walk != null && lastTag != hit.transform.tag) Destroy(walk.gameObject, 5);
                        if ((lastTag != hit.transform.tag && transform.childCount < 20) || walk == null)
                        {
                            walk = Instantiate(stepType, hit.point, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                            walk.transform.SetParent(transform, true);
                            walk.name = "Walk";
                        }

                        walk.transform.position = hit.point;
                        walk.Play();
                        lastTag = hit.transform.tag;
                    }
                }

                if (transform.tag == "Bullet" && rb.velocity.magnitude > 5)
                {
                    Collider[] bulletHits = Physics.OverlapSphere(transform.position, 0.2f);
                    foreach (var hit in bulletHits)
                    {
                        if (hit.isTrigger) continue;
                        if (hit.transform.tag != "Bullet")
                        {
                            if (friction == null)
                            {
                                friction = Instantiate(Manager.Instance.friction, transform.position, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                                friction.transform.SetParent(transform, false);
                                friction.name = "Friction";
                            }
                            else
                            {
                                friction.transform.position = transform.position;
                                friction.transform.rotation = Quaternion.LookRotation(rb.velocity);
                            }
                            friction.Play();
                        }
                    }

                    //if (walk == null)
                    //{
                    //    walk = Instantiate(Manager.Instance.walk, transform.position, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                    //    walk.transform.SetParent(transform, true);
                    //    walk.name = "walk";
                    //}
                    //else walk.transform.position = transform.position;
                    //walk.Play();
                }

                if (trailRenderer == null)
                {
                    trailRenderer = gameObject.AddComponent<TrailRenderer>();
                    trailRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
                    trailRenderer.startWidth = Mathf.Max(rb.velocity.magnitude / 100f, 0.1f);
                    trailRenderer.endWidth = 0.0f;
                    trailRenderer.time = 2;
                    trailRenderer.endColor = new Color(0f, 0f, 0f, 0f);
                }
                if (gameObject.tag == "Player")
                {
                    trailRenderer.startWidth = Mathf.Min(rb.velocity.magnitude / 200f, 0.1f);
                    trailRenderer.startColor = new Color(0f, 0f, 0f, Mathf.Min(rb.velocity.magnitude / 200f, 0.5f));
                }
                else
                {
                    trailRenderer.startWidth = Mathf.Min(rb.velocity.magnitude / 100f, 0.2f);
                    trailRenderer.startColor = new Color(0f, 0f, 0f, Mathf.Min(rb.velocity.magnitude / 100f, 0.5f));
                }
            }

            bool wasGrounded = isGrounded;
            isGrounded = Physics.CapsuleCast(
                transform.position + Vector3.up * (transform.localScale.y / 2 - transform.localScale.x / 2) + new Vector3(0, 0.1f, 0),
                transform.position - Vector3.up * (transform.localScale.y / 2 - transform.localScale.x / 2) - new Vector3(0, 0.1f, 0),
                transform.localScale.x / 2,
                Vector3.down,
                out RaycastHit landHit,
                transform.localScale.y * 1.5f
            );
            if (isGrounded && landHit.collider.isTrigger)
            {
                isGrounded = false;
            }
            if (isGrounded && !wasGrounded && rb.velocity.y > 1)
            {
                Manager.Instance.Sound(Manager.Instance.landSound, Mathf.Abs(rb.velocity.y/500), source);
                if (land == null)
                {
                    land = Instantiate(Manager.Instance.land, landHit.point, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                    land.transform.SetParent(transform, false);
                    land.name = "Land";
                }
                else land.transform.position = landHit.point;
                land.Play();
            }
        }
    }

    void FootStep()
    {
        if (rb != null && gameObject.tag != "Bullet")
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, (transform.localScale.y * 1.5f) + 0.1f) && rb.velocity.magnitude > 1)
            {
                if (hit.collider.isTrigger || hit.transform == transform) return;
                AudioClip stepType = Manager.Instance.hitSound;
                if (hit.transform.tag != "Untagged")
                {
                    switch (hit.transform.tag)
                    {
                        case "Enemy": stepType = Manager.Instance.damageSound; break;
                        case "Grass": stepType = Manager.Instance.grassSound; break;
                        case "Metal": stepType = Manager.Instance.metalSound; break;
                    }
                }
                Manager.Instance.Sound(stepType, 1, source);
                if (step == null)
                {
                    step = Instantiate(Manager.Instance.step, hit.point, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                    step.transform.SetParent(transform, true);
                    step.name = "Step";
                }
                step.transform.position = hit.point;
                step.Play();
            }
        }
    }
    
    void Fall()
    {
        if (rb != null) if (Mathf.Abs(rb.velocity.y) > 25 && !isGrounded) Manager.Instance.Sound(Manager.Instance.fallSound, Mathf.Abs(rb.velocity.y/250), source);
    }
}