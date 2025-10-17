using UnityEngine;
using System.Collections;

public class Turret : Enemy
{
    [Header("Turret Settings")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 15f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private Transform head; // Head transform for raycast origin
    [SerializeField] private GameObject muzzleFlashPrefab; // Visual effect for shooting
    [SerializeField] private LineRenderer laserLine; // Optional laser effect
    [SerializeField] private float laserFadeTime = 0.5f;

    private Transform player;
    private float lastAttackTime;

    protected override void Awake()
    {
        base.Awake();
        enemyName = "Turret";
        player = GameObject.FindWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogWarning("[Turret] Player not found.");
        }
        if (head == null)
        {
            head = transform.Find("Head");
            if (head == null)
            {
                Debug.LogWarning("[Turret] Head transform not found.");
            }
        }
        lastAttackTime = 0f;
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance < attackRange && Time.time > lastAttackTime + attackCooldown && !HasEffect(EffectType.Freeze))
        {
            Attack();
        }

        ApplyEffects();
    }

    private void Attack()
    {
        if (head == null) return;

        Vector3 rayOrigin = head.position;
        Vector3 rayDirection = (player.position - head.position).normalized;
        RaycastHit hit;

        // Perform raycast
        bool hitSomething = Physics.Raycast(rayOrigin, rayDirection, out hit, attackRange);
        while (hitSomething && hit.collider.tag == "Player")
        {
            Health playerHealth = hit.collider.GetComponent<Health>();
            if (playerHealth != null)
            {
                Transform playerHead = player.Find("Head"); // Assuming player has a head transform
                Vector3 target = playerHead != null ? playerHead.position : hit.point;
                playerHealth.Damage(target, attackDamage);
                StartCoroutine(ShowAttackEffect(rayOrigin, hit.point));
                break;
            }
            rayOrigin = hit.point + rayDirection * 0.1f;
            hitSomething = Physics.Raycast(rayOrigin, rayDirection, out hit, attackRange);
        }

        lastAttackTime = Time.time;
    }

    private IEnumerator ShowAttackEffect(Vector3 start, Vector3 end)
    {
        if (muzzleFlashPrefab != null)
        {
            GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, head.position, Quaternion.identity);
            Destroy(muzzleFlash, 0.2f);
        }

        if (laserLine != null)
        {
            laserLine.SetPosition(0, start);
            laserLine.SetPosition(1, end);
            float elapsed = 0f;
            while (elapsed < laserFadeTime)
            {
                elapsed += Time.deltaTime;
                laserLine.startWidth = Mathf.Lerp(0.1f, 0f, elapsed / laserFadeTime);
                laserLine.endWidth = Mathf.Lerp(0.1f, 0f, elapsed / laserFadeTime);
                yield return null;
            }
            Destroy(laserLine.gameObject);
        }
    }

    protected override void ApplyEffects()
    {
        base.ApplyEffects();
        // No additional velocity changes needed for turret
    }
}