using UnityEngine;
using System.Collections.Generic;

public class Slime : Enemy
{
    [Header("Slime Settings")]
    [SerializeField] float attackDamage = 5f;
    [SerializeField] float attackRange = 5f;
    [SerializeField] float attackCooldown = 1f;
    [SerializeField] float baseMoveSpeed = 3f;
    [SerializeField] float jumpForce = 5f;
    [SerializeField] float minJumpInterval = 1f;
    [SerializeField] float maxJumpInterval = 3f;
    [SerializeField] float groundCheckDistance = 0.1f;
    [SerializeField] float dashForce = 10f;

    Rigidbody rb;
    Transform player;
    Material slimeMaterial;
    float nextJumpTime;
    float moveSpeed;
    float lastAttackTime;
    [HideInInspector] public bool isLeader = true;
    [Header("Sound Settings")]
    [SerializeField] AudioClip mergeSound;
    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip footStep;

    protected override void Awake()
    {
        base.Awake();
        enemyName = "Slime";
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogWarning("[Slime] Rigidbody not found, adding one.");
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        rb.useGravity = true;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            slimeMaterial = rend.material;
            slimeMaterial.shader = Shader.Find("Custom/Slime");
        }
        else
        {
            Debug.LogWarning("[Slime] Renderer not found.");
        }

        player = GameObject.FindWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogWarning("[Slime] Player not found.");
        }

        nextJumpTime = Time.time + Random.Range(minJumpInterval, maxJumpInterval);
        moveSpeed = baseMoveSpeed;
        lastAttackTime = 0f;
        InvokeRepeating("FootStep", 0, 0.5f);
    }

    protected override void Start() => base.Start();

    void Update()
    {
        List<Vector3> nearbyPositions = new List<Vector3>();
        if (isLeader)
        {
            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, mergeRange);
            foreach (var col in nearbyEnemies)
            {
                if (col.tag == "Enemy" && col.gameObject != gameObject)
                {
                    Slime other = col.GetComponent<Slime>();
                    nearbyPositions.Add(col.transform.position);
                    if (other != null && other.isLeader && Random.value > 0.5f)
                    {
                        MergeWith(other);
                    }
                }
            }
        }

        if (slimeMaterial != null)
        {
            Vector3 vel = rb.velocity;
            float currentSpeed = vel.magnitude;
            float horizontalSpeed = new Vector2(vel.x, vel.z).magnitude;
            float fallSpeed = Mathf.Max(-vel.y, 0f);
            Vector3 moveDir = horizontalSpeed > 0.01f ? new Vector3(vel.x, 0, vel.z).normalized : Vector3.zero;

            slimeMaterial.SetFloat("_CurrentSpeed", currentSpeed);
            slimeMaterial.SetFloat("_HorizontalSpeed", horizontalSpeed);
            slimeMaterial.SetFloat("_FallSpeed", fallSpeed);
            slimeMaterial.SetVector("_MoveDir", moveDir);

            for (int i = 0; i < 3; i++)
            {
                string propName = "_NearbySlimePos" + (i + 1);
                if (i < nearbyPositions.Count)
                {
                    slimeMaterial.SetVector(propName, nearbyPositions[i]);
                }
                else
                {
                    slimeMaterial.SetVector(propName, Vector3.zero);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (player == null) return;

        Vector3 direction = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);

        if (!HasEffect(EffectType.Freeze))
        {
            rb.AddForce(direction * moveSpeed * damageMultiplier);
        }

        if (distance < attackRange && Time.time > lastAttackTime + attackCooldown)
        {
            var playerScript = player.GetComponent<Health>();
            if (playerScript != null)
            {
                playerScript.Damage(transform.position, attackDamage);
            }
            rb.AddForce(direction * dashForce, ForceMode.Impulse);
            lastAttackTime = Time.time;
        }

        if (isLeader && !HasEffect(EffectType.Freeze))
        {
            if (Time.time > nextJumpTime && IsGrounded())
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                Manager.Instance.Sound(jumpSound, 1, source);
                nextJumpTime = Time.time + Random.Range(minJumpInterval, maxJumpInterval);
            }
        }

        ApplyEffects();
    }

    void FootStep()
    {
        if (rb.velocity.magnitude > 1 && Random.Range(0, 3) == 0)
        {
            Manager.Instance.Sound(footStep, 1, source);
        }
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, -Vector3.up, groundCheckDistance);
    }

    protected override void ApplyEffects()
    {
        base.ApplyEffects();
        if (HasEffect(EffectType.Freeze))
        {
            rb.velocity = Vector3.zero;
        }
    }

    void MergeWith(Slime other)
    {
        Manager.Instance.Sound(mergeSound, 1, source);

        isLeader = Random.value > 0.5f;
        if (!isLeader)
        {
            other.isLeader = true;
            Rigidbody otherRigidbody = other.GetComponent<Rigidbody>();
            if (otherRigidbody != null)
            {
                springJoint = gameObject.AddComponent<SpringJoint>();
                springJoint.connectedBody = otherRigidbody;
                springJoint.spring = 1000f;
                springJoint.damper = 50f;
                springJoint.autoConfigureConnectedAnchor = false;
                springJoint.anchor = Vector3.zero;
                springJoint.connectedAnchor = Vector3.zero;
                other.damageMultiplier += 0.5f;
                mergedEnemy = other;
            }
        }
    }
}