using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Physic : MonoBehaviour
{
    AudioSource source;
    Rigidbody rb;
    ParticleSystem sparks;
    ParticleSystem frictionSparks;
    ParticleSystem stepSparks;
    ParticleSystem stepParticle;
    ParticleSystem landSparks;
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
            float radius = transform.localScale.x / 2;  // Радиус капсулы
            float rayDistance = new Vector3(transform.localScale.x, 0, transform.localScale.z).magnitude / 2f;
            RaycastHit[] hits = new RaycastHit[10];  // Буфер
            int hitCount = Physics.CapsuleCastNonAlloc(transform.position, transform.position + rb.velocity.normalized * rayDistance, radius, rb.velocity.normalized, hits, rayDistance);

            if (rb.velocity.magnitude > 1)
            {
                for (int i = 0; i < hitCount; i++)
                {
                    var hit = hits[i];
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

                        if (frictionSparks == null)
                        {
                            frictionSparks = Instantiate(Manager.Instance.frictionSparks, hit.point, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                            frictionSparks.transform.SetParent(transform, false);
                            frictionSparks.name = "frictionSpark";
                        }
                        else
                        {
                            frictionSparks.transform.position = hit.point;
                            frictionSparks.transform.rotation = Quaternion.LookRotation(rb.velocity);
                        }
                        frictionSparks.Play();

                        if (rb.velocity.magnitude > 10)
                        {
                            if (sparks == null)
                            {
                                sparks = Instantiate(Manager.Instance.sparks, hit.point, Quaternion.LookRotation(transform.position - hit.point)).GetComponent<ParticleSystem>();
                                sparks.transform.SetParent(transform, false);
                                sparks.name = "spark";
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
                }
                if (new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude > 1)
                {
                    RaycastHit[] footHits = Physics.RaycastAll(transform.position, Vector3.down, transform.localScale.y * 1.5f);
                    foreach (var hit in footHits)
                    {
                        ParticleSystem stepType = Manager.Instance.stepSparks;
                        if (hit.transform.tag != "Untagged")
                        {
                            switch (hit.transform.tag)
                            {
                                case "Grass": stepType = Manager.Instance.grassSparks; break;
                                case "Metal": stepType = Manager.Instance.metalSparks; break;
                            }
                        }

                        if (stepSparks != null && lastTag != hit.transform.tag) Destroy(stepSparks.gameObject, 5);
                        if ((lastTag != hit.transform.tag && transform.childCount < 20) || stepSparks == null)
                        {
                            stepSparks = Instantiate(stepType, hit.point, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                            stepSparks.transform.SetParent(transform, true);
                            stepSparks.name = "stepSparks";
                        }

                        stepSparks.transform.position = hit.point;
                        stepSparks.Play();
                        lastTag = hit.transform.tag;
                    }
                }

                if (transform.tag == "Bullet" && rb.velocity.magnitude > 5)
                {
                    Collider[] bulletHits = Physics.OverlapSphere(transform.position, 0.1f);
                    foreach (var hit in bulletHits)
                    {
                        if (hit.transform.tag != "Bullet")
                        {
                            if (frictionSparks == null)
                            {
                                frictionSparks = Instantiate(Manager.Instance.frictionSparks, transform.position, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                                frictionSparks.transform.SetParent(transform, false);
                                frictionSparks.name = "frictionSparks";
                            }
                            else
                            {
                                frictionSparks.transform.position = transform.position;
                                frictionSparks.transform.rotation = Quaternion.LookRotation(rb.velocity);
                            }
                            frictionSparks.Play();
                        }
                    }

                    if (stepSparks == null)
                    {
                        stepSparks = Instantiate(Manager.Instance.stepSparks, transform.position, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                        stepSparks.transform.SetParent(transform, true);
                        stepSparks.name = "stepSparks";
                    }
                    else stepSparks.transform.position = transform.position;
                    stepSparks.Play();
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
                    trailRenderer.startColor = new Color(0f, 0f, 0f, Mathf.Min(rb.velocity.magnitude / 150f, 0.1f));
                }
                else
                {
                    trailRenderer.startWidth = Mathf.Min(rb.velocity.magnitude / 100f, 0.1f);
                    trailRenderer.startColor = new Color(0f, 0f, 0f, Mathf.Min(rb.velocity.magnitude / 100f, 0.1f));
                }
            }
            
            bool wasGrounded = isGrounded;
            isGrounded = Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitLand, transform.localScale.y * 1.5f);
            if (isGrounded && !wasGrounded)
            {
                Manager.Instance.Sound(Manager.Instance.landSound, Mathf.Abs(rb.velocity.y/100), source);
                if (landSparks == null)
                {
                    landSparks = Instantiate(Manager.Instance.landSparks, hitLand.point, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                    landSparks.transform.SetParent(transform, false);
                    landSparks.name = "landSparks";
                }
                else landSparks.transform.position = hitLand.point;
                landSparks.Play();
            }
        }
    }

    void FootStep()
    {
        if (rb != null && gameObject.tag != "Bullet")
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, transform.localScale.y * 1.5f) && rb.velocity.magnitude > 1)
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
                Manager.Instance.Sound(stepType, 1, source);
                if (stepParticle == null)
                {
                    stepParticle = Instantiate(Manager.Instance.stepParticle, hit.point, Quaternion.LookRotation(rb.velocity)).GetComponent<ParticleSystem>();
                    stepParticle.transform.SetParent(transform, true);
                    stepParticle.name = "stepParticle";
                }
                stepParticle.transform.position = hit.point;
                stepParticle.Play();
            }
        }
    }
    
    void Fall()
    {
        if (rb != null) if (Mathf.Abs(rb.velocity.y) > 25) Manager.Instance.Sound(Manager.Instance.fallSound, Mathf.Abs(rb.velocity.y/250), source);
    }
}