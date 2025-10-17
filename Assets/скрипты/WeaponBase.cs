using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Weapon Settings")]
    public GameObject particle;
    [HideInInspector] public Animator weaponAnimator;
    [HideInInspector] public ParticleSystem[] particles;
    protected Transform[] spawns;

    public float cooldown;
    [HideInInspector] public float cooldownTimer;
    public float altCooldown;
    [HideInInspector] public float altCooldownTimer;
    [HideInInspector] public float ammo;
    public float maxAmmo;
    public float particleTimer;
    protected new Transform camera;
    protected Vector3 cameraPos;

    public virtual void Initialize()
    {
        camera = Camera.main.transform;
        cameraPos = camera.localPosition;
        cooldownTimer = 0f;
        altCooldownTimer = 0f;
        if (gameObject != null) weaponAnimator = gameObject.GetComponent<Animator>();
        if (weaponAnimator == null && gameObject != null) Debug.LogError($"[Weapon] Animator not found for {gameObject.name}!");

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

    public virtual void UpdateWeapon()
    {
        cooldownTimer -= Time.deltaTime;
        altCooldownTimer -= Time.deltaTime;

        if (particle != null)
        {
            if (particleTimer > 0)
            {
                particleTimer -= Time.deltaTime;
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].transform.position = spawns[i].position;
                    particles[i].transform.rotation = spawns[i].rotation;
                }
            }
            else
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    particles[i].Stop();
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            StartPrimaryAttack();
        }
        if (Input.GetMouseButton(0))
        {
            HoldPrimaryAttack();
        }
        if (Input.GetMouseButtonUp(0))
        {
            ReleasePrimaryAttack();
        }

        if (Input.GetMouseButtonDown(1))
        {
            StartAltAttack();
        }
        if (Input.GetMouseButton(1))
        {
            HoldAltAttack();
        }
        if (Input.GetMouseButtonUp(1))
        {
            ReleaseAltAttack();
        }
    }

    public virtual void Particle(float timer)
    {
        for (int i = 0; i < particles.Length; i++)
        {
            particles[i].Play();
            particleTimer += timer;
        }
    }

    public virtual void Shake(float strength = 0.1f, float duration = 0.1f, int vibrato = 100, Ease ease = Ease.OutQuad)
    {
        if (camera.localPosition == cameraPos)
        {            
            camera.DOShakePosition(duration, strength, vibrato, 90f).SetEase(ease);
            camera.DOShakeRotation(duration, strength, vibrato, 90f).SetEase(ease);
        }
    }

    public abstract void StartPrimaryAttack();
    public abstract void HoldPrimaryAttack();
    public abstract void ReleasePrimaryAttack();
    public abstract void StartAltAttack();
    public abstract void HoldAltAttack();
    public abstract void ReleaseAltAttack();
    public abstract string GetAmmoText();
    public abstract string GetAltAmmoText();
}