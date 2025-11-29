using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class Copier : WeaponBase
{
    [SerializeField] float raycastRange = 100f;
    [SerializeField] AudioClip primary;
    [SerializeField] AudioClip alt;

    [SerializeField] GameObject preview;
    GameObject saved;
    //float size = 1;

    void Awake()
    {
        base.Initialize();
    }

    void Update()
    {
        base.UpdateWeapon();
        if (saved != null)
        {            
            saved.transform.position = preview.transform.position;
            saved.transform.rotation = preview.transform.rotation;
        }
    }

    public override void StartPrimaryAttack() { }

    public override void HoldPrimaryAttack()
    {
        if (cooldownTimer > 0 ) return;
        if (saved == null) return;

        Manager.Instance.Sound(primary);
        Particle(0.1f);
        Shake(0.1f, 0.3f);
        weaponAnimator.SetTrigger("1");
        GameObject p = null;
        foreach (var spawn in spawns)
        {
            if (saved.GetComponent<Throwable>() == null) p = Manager.Instance.Fire(null, saved, spawn.position, spawn.rotation, camera.forward, 15, 0, false);
            else p = Manager.Instance.Fire(true, saved, spawn.position, spawn.rotation, camera.forward, 15, 0, false);
        }

        p.transform.localScale = saved.transform.localScale;
        //p.transform.localScale *= size;

        cooldownTimer = cooldown;
        if (saved.GetComponent<Bullet>() != null) cooldownTimer = cooldown/10f;
    }

    public override void ReleasePrimaryAttack() { }

    public override void StartAltAttack()
    {
        weaponAnimator.SetBool("2", true);
    }

    public override void HoldAltAttack()
    {
        if (altCooldownTimer > 0) return;

        if (Physics.BoxCast(camera.position, new Vector3(0.1f, 0.1f, 0f), camera.forward, out RaycastHit hit, camera.rotation, raycastRange))
        {
            Projectile projectile = hit.collider.GetComponent<Projectile>();
            if (projectile.gameObject == saved) return;
            if (projectile != null)
            {
                Destroy(saved);
                saved = Instantiate(projectile.gameObject, preview.transform.position, preview.transform.rotation);
                saved.transform.SetParent(preview.transform, false);
                
                Vector3 v = saved.transform.localScale;
                float axis = Mathf.Abs(v.x) > Mathf.Abs(v.y) ? (Mathf.Abs(v.x) > Mathf.Abs(v.z) ? 0 : 2) : (Mathf.Abs(v.y) > Mathf.Abs(v.z) ? 1 : 2);
                //if (saved.tag != "Bullet" && saved.tag != "Rocket" && axis > 0.1f)
                //{
                //    size = axis / 0.1f;
                //    saved.transform.localScale /= size;
                //}
                //else
                //{
                //    size = 1;
                //}
                saved.GetComponent<Projectile>().enabled = false;
                Collider[] sc = saved.GetComponents<Collider>();
                foreach (Collider c in sc)
                {
                    c.isTrigger = true;
                }
                if (saved.GetComponent<Rigidbody>() != null) Destroy(saved.GetComponent<Rigidbody>());
                if (saved.GetComponent<Physic>() != null) Destroy(saved.GetComponent<Physic>());
                if (saved.GetComponent<TrailRenderer>() != null) Destroy(saved.GetComponent<TrailRenderer>());
                if (saved.GetComponent<AudioSource>() != null) Destroy(saved.GetComponent<AudioSource>());
            }
//            else if (hit.transform.lossyScale.magnitude < 0.9 && !hit.collider.isTrigger)
//            {
//                Destroy(prev);
//                prev = Instantiate(hit.collider.gameObject, preview.transform.position, preview.transform.rotation);
//
//                savedProjectile = prev;
//            }

            if (saved == hit.collider.gameObject)
            {                
                Manager.Instance.Sound(alt);
                Shake(0.05f, 0.5f);
                Particle(0.05f);
            }
        }
        weaponAnimator.SetTrigger("2");

        altCooldownTimer = altCooldown;
    }

    public override void ReleaseAltAttack() 
    { 
        weaponAnimator.SetBool("2", false);
    }

    public override string GetAmmoText() => "";
    public override string GetAltAmmoText() => "";
}