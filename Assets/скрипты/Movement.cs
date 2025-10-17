using UnityEngine;

public class Movement : MonoBehaviour
{
    [Header("Movement Settings")]
    Rigidbody rb;
    [SerializeField] float speed = 5f;
    [SerializeField] float jumpHeight = 3f;
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float dashForce = 20f;
    public float stamina = 100;
    [HideInInspector] public float maxStamina = 100f;
    [SerializeField] float hookPullSpeed = 20f;
    [SerializeField] Animator holderAnimator;

    [Header("Sound Settings")]
    [SerializeField] AudioClip footStep;
    [SerializeField] AudioClip grassFootStep;
    [SerializeField] AudioClip metalFootStep;

    [SerializeField] AudioClip jumpSound;
    [SerializeField] AudioClip landSound;
    [SerializeField] AudioClip staminaSound;
    [SerializeField] AudioClip dashSound;
    [SerializeField] AudioClip dashFailSound;
    [Header("Effects")]
    [SerializeField] ParticleSystem speedParticle;
    bool Anim = true;

    Transform cam;
    float camPitch;
    float dashCooldownTimer;
    float pushBackCooldownTimer;

    bool isGrounded;
    bool isHooking;
    Vector3 hookTarget;
    Vector3 move;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = true;
        maxStamina = stamina;
        cam = Camera.main.transform.parent;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        InvokeRepeating("FootStep", 0, 0.5f);
    }

    void Update()
    {
        if (Time.timeScale == 0) return;
        HandleCamera();
        CheckGrounded();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(horizontal, 0, vertical).normalized;
        Vector3 desiredVelocity = (transform.right * input.x + transform.forward * input.z) * speed;
        move = Vector3.ProjectOnPlane(desiredVelocity, transform.up);
        if (new Vector3(rb.velocity.x, 0, rb.velocity.z).magnitude < 20)
        {
            rb.AddForce(desiredVelocity - new Vector3(rb.velocity.x / 2, 0, rb.velocity.z / 2), ForceMode.VelocityChange);
        }

        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * Physics.gravity.magnitude), ForceMode.Impulse);
            Manager.Instance.Sound(jumpSound);
        }

        dashCooldownTimer = Mathf.Max(0, dashCooldownTimer - Time.deltaTime);
        pushBackCooldownTimer = Mathf.Max(0, pushBackCooldownTimer - Time.deltaTime);

        if (isHooking)
        {
            rb.velocity += (hookTarget - transform.position) * hookPullSpeed * Time.fixedDeltaTime;

            if (Vector3.Distance(transform.position, hookTarget) < 0.5f || Input.GetKeyDown(KeyCode.R)) isHooking = false;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (stamina > 30)
            {
                Dash();
                stamina -= 30;
            }
            else Manager.Instance.Sound(dashFailSound);
        }

        if (rb.velocity.magnitude > 20)
        {
            RaycastHit[] hits = Physics.RaycastAll(transform.position, rb.velocity.normalized, transform.localScale.magnitude * 2);
            foreach (var hit in hits)
            {
                Enemy enemy = hit.collider.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.Damage(rb.velocity.magnitude, false, hit.point);
                }
            }

            speedParticle.Play();
        }

        bool isHit = false;
        Collider[] hitsEnemy = Physics.OverlapSphere(transform.position, transform.localScale.magnitude * 25);
        foreach (var hit in hitsEnemy)
        {
            if (hit.tag == "Enemy" || hit.tag == "Arena")
            {
                isHit = true;
                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (Manager.Instance.music) distance /= 2;

                if (distance < 20)
                {
                    Manager.Instance.music = true;
                }
                else Manager.Instance.music = false;
            }
        }
        if (!isHit) Manager.Instance.music = false;
    }

    void FixedUpdate()
    {
        bool wasMax = stamina >= maxStamina;
        if (stamina < maxStamina) stamina += 0.5f;
        if (stamina >= maxStamina && !wasMax) Manager.Instance.Sound(staminaSound);
    }

    void HandleCamera()
    {
        if (!cam) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0, mouseX, 0);
        camPitch = Mathf.Clamp(camPitch - mouseY, -150f, 150f);
        cam.localRotation = Quaternion.Euler(camPitch, 0, 0);
    }

    void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, transform.localScale.y * 1.5f);
        if (isGrounded && !wasGrounded)
        {
            Manager.Instance.Sound(landSound);
        }
    }

    void FootStep()
    {
        if (isGrounded && move != Vector3.zero)
        {
            if (Anim) { holderAnimator.SetTrigger("ходьба"); Anim = false; }
            else Anim = true;
        }
        else { holderAnimator.SetTrigger("не ходьба"); Anim = true; }
    }

    public void Dash()
    {
        rb.AddForce(new Vector3(cam.forward.x, Mathf.Max(0, cam.forward.y), cam.forward.z) * dashForce, ForceMode.VelocityChange);
        Manager.Instance.Sound(dashSound);
        Collider[] dashHits = Physics.OverlapSphere(transform.position, 2f);
        foreach (var dashHit in dashHits)
        {
            Enemy enemy = dashHit.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Damage(10);
                Debug.Log($"[Weapon] Dash hit enemy: {dashHit.name}, damage: {20}");
            }
        }
        Debug.Log("[Weapon] Dash performed");
    }

    public void PushBack(Vector3 dir, float pushBackForce)
    {
        rb.AddForce(dir * pushBackForce, ForceMode.VelocityChange);
    }

    public void PullToPoint(Vector3 target)
    {
        hookTarget = target;
        isHooking = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (other.tag == "Start") Manager.Instance.game = true;
        if (other.tag == "Arena") Manager.Instance.music = true;
    }
}