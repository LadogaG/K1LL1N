using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using TMPro;

public class Weapon : MonoBehaviour
{
    [Header("Sound Settings")]
    [SerializeField] AudioClip equip;
    [SerializeField] AudioClip AmmoPickupSound;
    [SerializeField] AudioClip PickupSound;

    [Header("UI Settings")]
    [SerializeField] TextMeshProUGUI ammoText;
    [SerializeField] TextMeshProUGUI altAmmoText;
    public bool randFont = true;
    [SerializeField] TMP_FontAsset[] fonts;
    [SerializeField] Transform canvases;

    [Header("Weapon Settings")]
    [SerializeField] Transform holder;
    [SerializeField] Transform offset;
    [SerializeField] Transform weaponHolder;
    [SerializeField] Axe axe;
    [SerializeField] int weaponsCount = 0;
    [SerializeField] Vector3[] weaponPoses;
    List<WeaponBase> weapons;
    int currentIndex = 0;

    [Header("Weapon Holder Animation")]
    [SerializeField] Animator weaponHolderAnimator;
    [SerializeField] float holderMaxOffset = 0.05f;
    [SerializeField] float holderMaxRotation = 5f;
    [SerializeField] float holderFollowSpeed = 5f;
    Vector3 holderInitialLocalPosition;
    Quaternion holderInitialLocalRotation;
    Vector3 weaponHolderInitialLocalPosition;
    Quaternion weaponHolderInitialLocalRotation;
    Vector3 canvasesInitialLocalPosition;
    Quaternion canvasesInitialLocalRotation;

    [Header("Hand Settings")]
    [SerializeField] Transform handHolder;
    [SerializeField] GameObject grayHandModel;
    [SerializeField] GameObject blueHandModel;
    [SerializeField] GameObject greenHandModel;
    [SerializeField] GameObject redHandModel;
    [SerializeField] GameObject darkBlueHandModel;
    [SerializeField] GameObject bordeauxHandModel;
    [SerializeField] GameObject turquoiseHandModel;
    [SerializeField] GameObject blackHandModel;
    [SerializeField] GameObject whiteHandModel;
    [SerializeField] GameObject colorfulHandModel;
    [SerializeField] float hookRange = Mathf.Infinity;
    [SerializeField] float shortHookRange = 5f;
    [SerializeField] float explosionDamage = 50f;
    [SerializeField] float electricDamagePerSecond = 5f;
    [SerializeField] float fireDamagePerSecond = 5f;
    [SerializeField] float freezeDamagePerSecond = 3f;
    [SerializeField] float invisibilityMax = 10f;
    [SerializeField] float invisibilityChargeTime = 3f;
    [SerializeField] float invisibilityDuration = 6f;
    [SerializeField] float timeSlowFactor = 0.5f;
    public enum HandType { GrayInHand, BlueInHand, GreenInHand, RedInHand, DarkBlueInHand, BordeauxInHand, TurquoiseInHand, BlackInHand, WhiteInHand, ColorfulInHand }
    public HandType currentHand = HandType.GrayInHand;

    int currentHandIndex = 0;
    bool isInvisible = false;
    float invisibilityCharge = 10f;
    float invisibilityTimer = 0f;
    bool isTimeSlowed = false;
    Movement movement;
    Rigidbody rb;
    Health health;
    float lastDashTime;
    bool isHookAiming = false;
    Vector3 hookTarget;
    LineRenderer activeHookLineRenderer;
    Rigidbody weaponHolderRb;
    Quaternion lastCameraRotation;
    new Transform camera;

    void Awake()
    {
        movement = GetComponent<Movement>();
        camera = Camera.main.transform;
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        weaponHolderRb = weaponHolder.GetComponent<Rigidbody>();

        weapons = weaponHolder.transform.Cast<Transform>()
            .Select(child => {
                var component = child.GetComponent<WeaponBase>();
                return component != null ? component : (child.childCount > 0 ? child.GetChild(0).GetComponent<WeaponBase>() : null);
            })
            .Where(component => component != null)
            .Where(component => component.gameObject.name != "copy")
            .ToList();

        SetHandActive(currentHandIndex);

        holderInitialLocalPosition = offset.localPosition;
        holderInitialLocalRotation = offset.localRotation;
        weaponHolderInitialLocalPosition = weaponHolder.localPosition;
        weaponHolderInitialLocalRotation = weaponHolder.localRotation;
        canvasesInitialLocalPosition = canvases.localPosition;
        canvasesInitialLocalRotation = canvases.localRotation;
        lastCameraRotation = camera.rotation;
    }

    void Update()
    {
        if (Time.timeScale == 0) return;
        weapons = weaponHolder.transform.Cast<Transform>()
            .Select(child => {
                var component = child.GetComponent<WeaponBase>();
                return component != null ? component : (child.childCount > 0 ? child.GetChild(0).GetComponent<WeaponBase>() : null);
            })
            .Where(component => component != null)
            .Where(component => component.gameObject.name != "copy")
            .ToList();

        if (ammoText != null) ammoText.text = weapons[currentIndex].GetAmmoText();
        if (altAmmoText != null) altAmmoText.text = weapons[currentIndex].GetAltAmmoText();

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            if (scroll > 0f)
            {
                currentIndex = (currentIndex + 1) % weapons.Count;
                SetWeaponActive(currentIndex);
            }
            else if (scroll < 0f)
            {
                currentIndex = (currentIndex - 1 + weapons.Count) % weapons.Count;
                SetWeaponActive(currentIndex);
            }
        }

        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i + 1))
            {
                currentIndex = i % weapons.Count;
                SetWeaponActive(currentIndex);
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            currentHandIndex = (currentHandIndex + 1) % 10;
            currentHand = (HandType)currentHandIndex;
            SetHandActive(currentHandIndex);
        }

        if (currentHand == HandType.BordeauxInHand)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ShootOil();
            }
            if (Input.GetKey(KeyCode.R))
            {
                UseFire();
            }
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            if (currentHand == HandType.GreenInHand || currentHand == HandType.BlueInHand)
            {
                StartHookAim();
            }
            else if (currentHand == HandType.WhiteInHand)
            {
                Time.timeScale = timeSlowFactor;
                isTimeSlowed = true;
                Debug.Log("[Weapon] Time slowed");
            }
            else
            {
                UseHand();
            }
        }
        else if (Input.GetKey(KeyCode.R))
        {
            if (currentHand == HandType.GreenInHand || currentHand == HandType.BlueInHand)
            {
                UpdateHookAim();
            }
            else
            {
                HoldHand();
            }
        }
        else if (Input.GetKeyUp(KeyCode.R))
        {
            if (currentHand == HandType.GreenInHand || currentHand == HandType.BlueInHand)
            {
                ReleaseHook();
            }
            else
            {
                ReleaseHand();
            }
            if (currentHand == HandType.WhiteInHand && isTimeSlowed)
            {
                Time.timeScale = 1;
                isTimeSlowed = false;
                Debug.Log("[Weapon] Time restored");
            }
        }

        if (currentHand == HandType.BlackInHand && !isInvisible)
        {
            invisibilityCharge += Time.deltaTime * (invisibilityMax / invisibilityChargeTime);
            invisibilityCharge = Mathf.Clamp(invisibilityCharge, 0, invisibilityMax);
        }
        if (isInvisible)
        {
            invisibilityTimer -= Time.deltaTime;
            if (invisibilityTimer <= 0)
            {
                isInvisible = false;
            }
        }

        if (weapons[currentIndex].transform.parent.Cast<Transform>().Count(t => t.name == "copy") < weaponsCount)
        {            
            foreach (var w in weapons)
            {
                while (w.transform.parent.Cast<Transform>().Count(t => t.name == "copy") < weaponsCount)
                {
                    w.gameObject.SetActive(true);
                    if (w.transform.parent.name == w.name)
                    {
                        GameObject newWeaponPos = new GameObject("copy");
                        newWeaponPos.transform.SetParent(w.transform.parent, false);
                        newWeaponPos.transform.position = w.transform.position;
                        newWeaponPos.transform.rotation = w.transform.rotation;
                        newWeaponPos.transform.localPosition = Vector3.zero;
                        int posIndex = w.transform.parent.Cast<Transform>().Count(t => t.name == "copy") - 1;
                        bool cycle = false;
                        while (posIndex > weaponPoses.Length - 1)
                        {
                            posIndex -= weaponPoses.Length;
                            cycle = true;
                        }
                        newWeaponPos.transform.localPosition += weaponPoses[posIndex] + (cycle ? Random.onUnitSphere/4f : Vector3.zero);
                        GameObject newWeapon = Instantiate(w.gameObject, newWeaponPos.transform.position, newWeaponPos.transform.rotation);
                        newWeapon.transform.SetParent(newWeaponPos.transform, false);
                    }
                    else if (w.name != "copy")
                    {
                        Transform newWeapon = new GameObject(w.name).transform;
                        newWeapon.SetParent(weaponHolder, false);
                        w.transform.parent = newWeapon;
                    }
                    SetWeaponActive(currentIndex);
                }
                if (w.transform.parent.Cast<Transform>().Count(t => t.name == "copy") > weaponsCount)
                {
                    GameObject[] cs = w.transform.parent.GetComponentsInChildren<Transform>()
                        .Where(child => child.name.Contains("copy"))
                        .Select(child => child.gameObject)
                        .ToArray();
                        
                    Destroy(cs[Random.Range(0, cs.Length)]);
                }
            }
        }

        UpdateHolder();
    }

    void SetWeaponActive(int index)
    {
        for (int i = 0; i < weapons[currentIndex].particles.Length; i++)
        {
            weapons[currentIndex].particles[i].Stop();
        }

        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].transform.parent.name == weapons[i].name || weapons[i].name == "copy")
            {
                if (weapons[i].transform.parent != null)
                {
                    weapons[i].transform.parent.gameObject.SetActive(i == index);
                    foreach (Transform c in weapons[i].transform.parent)
                    {
                        c.gameObject.SetActive(true);
                    }
                }
            }
            else if (weapons[i] != null) weapons[i].gameObject.SetActive(i == index);
        }

        TMP_FontAsset randomFont = randFont ? fonts[Random.Range(0, fonts.Length)] : fonts[0];
        ammoText.font = randomFont;
        altAmmoText.font = randomFont;

        Manager.Instance.Sound(equip);
        weaponHolderAnimator.SetTrigger("смена");
        weapons[currentIndex].cooldownTimer = 0;
        weapons[currentIndex].altCooldownTimer = 0;
        if (weapons[currentIndex].ammo <= 0) weapons[currentIndex].ammo += 1;
    }

    void UpdateHolder()
    {
        Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
        Vector3 targetOffset = new Vector3(
            Mathf.Clamp(-localVelocity.x * 0.1f, -holderMaxOffset, holderMaxOffset),
            Mathf.Clamp(-localVelocity.y * 0.1f, -holderMaxOffset, holderMaxOffset),
            Mathf.Clamp(-localVelocity.z * 0.1f, -holderMaxOffset, holderMaxOffset)
        );

        Quaternion deltaRotation = Quaternion.Inverse(lastCameraRotation) * camera.rotation;
        lastCameraRotation = camera.rotation;
        Vector3 eulerAngles = deltaRotation.eulerAngles;
        eulerAngles = new Vector3(
            Mathf.Clamp(eulerAngles.x > 180 ? eulerAngles.x - 360 : eulerAngles.x, -holderMaxRotation, holderMaxRotation),
            Mathf.Clamp(eulerAngles.y > 180 ? eulerAngles.y - 360 : eulerAngles.y, -holderMaxRotation, holderMaxRotation),
            Mathf.Clamp(eulerAngles.z > 180 ? eulerAngles.z - 360 : eulerAngles.z, -holderMaxRotation, holderMaxRotation)
        );
        eulerAngles = -eulerAngles;
        Quaternion targetRotationOffset = Quaternion.Euler(eulerAngles);
        
        Vector3 canvasesTargetOffset = targetOffset * 0.15f;
        Quaternion canvasesTargetRotationOffset = Quaternion.Euler(eulerAngles * 0.5f);
        canvases.localPosition = Vector3.Lerp(canvases.localPosition, canvasesInitialLocalPosition + canvasesTargetOffset, Time.deltaTime * holderFollowSpeed);
        canvases.localRotation = Quaternion.Lerp(canvases.localRotation, canvasesInitialLocalRotation * canvasesTargetRotationOffset, Time.deltaTime * holderFollowSpeed);

        if (weaponHolderRb.isKinematic)
        {
            offset.localPosition = Vector3.Lerp(offset.localPosition, holderInitialLocalPosition + targetOffset, Time.deltaTime * holderFollowSpeed);
            offset.localRotation = Quaternion.Lerp(offset.localRotation, holderInitialLocalRotation * targetRotationOffset, Time.deltaTime * holderFollowSpeed);
        }

        if (health.rArmHealth == 0)
        {
            weaponHolderRb.isKinematic = false;
            weaponHolderAnimator.enabled = false;
        }
        
        else if (!weaponHolderRb.isKinematic)
        {
            weaponHolderRb.isKinematic = true;
            weaponHolder.localPosition = weaponHolderInitialLocalPosition;
            weaponHolder.localRotation = weaponHolderInitialLocalRotation;
            weaponHolderAnimator.enabled = true;
        }
    }

    void SetHandActive(int index)
    {
        if (grayHandModel != null) grayHandModel.SetActive(index == 0);
        if (blueHandModel != null) blueHandModel.SetActive(index == 1);
        if (greenHandModel != null) greenHandModel.SetActive(index == 2);
        if (redHandModel != null) redHandModel.SetActive(index == 3);
        if (darkBlueHandModel != null) darkBlueHandModel.SetActive(index == 4);
        if (bordeauxHandModel != null) bordeauxHandModel.SetActive(index == 5);
        if (turquoiseHandModel != null) turquoiseHandModel.SetActive(index == 6);
        if (blackHandModel != null) blackHandModel.SetActive(index == 7);
        if (whiteHandModel != null) whiteHandModel.SetActive(index == 8);
        if (colorfulHandModel != null) colorfulHandModel.SetActive(index == 9);
        Debug.Log($"[Weapon] Hand switched to {currentHand}");
    }

    void StartHookAim()
    {
        isHookAiming = true;
        activeHookLineRenderer = Manager.Instance.GetLineRenderer(currentHand == HandType.GreenInHand ? Color.green : Color.blue);
        RaycastHit handHit;
        bool hitSomething = Physics.Raycast(handHolder.position, camera.forward, out handHit, Mathf.Infinity);
        Vector3 endPoint = hitSomething ? handHit.point : handHolder.position + camera.forward * 100f;
        Manager.Instance.ShowLineRenderer(handHolder.position, endPoint, activeHookLineRenderer, Manager.Instance.sparks);
        activeHookLineRenderer.enabled = true;
        Debug.Log($"[Weapon] Hook aim started: {currentHand}");
    }

    void UpdateHookAim()
    {
        Vector3 rayOrigin = holder.position;
        Vector3 rayDirection = camera.forward;
        RaycastHit handHit;
        float range = currentHand == HandType.GreenInHand ? hookRange : shortHookRange;
        bool hitSomething = Physics.Raycast(rayOrigin, rayDirection, out handHit, range);

        if (hitSomething)
        {
            hookTarget = handHit.point;
            Debug.Log($"[Weapon] Hook aim hit: {hookTarget}, object: {handHit.collider.name}");
        }
        else
        {
            hookTarget = rayOrigin + rayDirection * 100f;
            Debug.Log("[Weapon] Hook aim missed, default target: " + hookTarget);
        }

        if (activeHookLineRenderer != null)
        {
            activeHookLineRenderer.SetPosition(0, rayOrigin);
            activeHookLineRenderer.SetPosition(1, hookTarget);
        }

        if (currentHand == HandType.BlueInHand && hitSomething)
        {
            Enemy enemy = handHit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Damage(axe.axeDamage * Time.deltaTime);
                Debug.Log($"[Weapon] Blue hand dealing damage to {handHit.collider.name}: {axe.axeDamage * Time.deltaTime}");
            }
        }
    }

    void ReleaseHook()
    {
        if (!isHookAiming || activeHookLineRenderer == null)
        {
            return;
        }

        Destroy(activeHookLineRenderer.gameObject);

        if (Camera.main == null || holder == null)
        {
            Debug.LogError("[Weapon] Main Camera or holder not found!");
            return;
        }

        Vector3 rayOrigin = handHolder.position;
        Vector3 rayDirection = camera.forward;
        RaycastHit handHit;
        float range = currentHand == HandType.GreenInHand ? hookRange : shortHookRange;

        if (Physics.Raycast(rayOrigin, rayDirection, out handHit, range))
        {
            movement.PullToPoint(handHit.point);
            Debug.Log($"[Weapon] Hook released, pulling to: {handHit.point}, object: {handHit.collider.name}");
        }
        else
        {
            Debug.Log("[Weapon] Hook released, no target hit");
        }

        isHookAiming = false;
    }

    void UseHand()
    {
        Vector3 rayOrigin = handHolder.position;
        Vector3 rayDirection = camera.forward;
        RaycastHit handHit;

        switch (currentHand)
        {
            case HandType.GrayInHand:
                Vector3 boxSize = new Vector3(1f, 1f, axe.axeRange);
                RaycastHit[] handHits = Physics.BoxCastAll(rayOrigin, boxSize / 2, rayDirection, Quaternion.identity, axe.axeRange);
                foreach (var hit in handHits)
                {
                    Enemy enemy = hit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.Damage(axe.axeDamage);
                        Debug.Log($"[Weapon] Gray hand hit enemy: {hit.collider.name}, damage: {axe.axeDamage}");
                    }
                }
                Debug.Log("[Weapon] Gray hand attack");
                break;

            case HandType.RedInHand:
                if (Physics.Raycast(rayOrigin, rayDirection, out handHit, hookRange))
                {
                    Enemy enemy = handHit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.Damage(explosionDamage);
                        Debug.Log($"[Weapon] Red hand hit enemy: {handHit.collider.name}, damage: {explosionDamage}");
                    }
                }
                movement.PushBack(-camera.forward, 20);
                Debug.Log("[Weapon] Red hand used, pushback triggered");
                break;
        }
    }

    void HoldHand()
    {
        Vector3 rayOrigin = handHolder.position;
        Vector3 rayDirection = camera.forward;
        RaycastHit handHit;

        switch (currentHand)
        {
            case HandType.DarkBlueInHand:
                if (Physics.Raycast(rayOrigin, rayDirection, out handHit, hookRange))
                {
                    Enemy enemy = handHit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.Damage(electricDamagePerSecond * Time.deltaTime);
                        enemy.ApplyEffect(Enemy.EffectType.Electric);
                        Debug.Log($"[Weapon] DarkBlue hand dealing electric damage to {handHit.collider.name}");
                    }
                }
                break;

            case HandType.TurquoiseInHand:
                if (Physics.Raycast(rayOrigin, rayDirection, out handHit, hookRange))
                {
                    Enemy enemy = handHit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.Damage(freezeDamagePerSecond * Time.deltaTime);
                        enemy.ApplyEffect(Enemy.EffectType.Freeze);
                        Debug.Log($"[Weapon] Turquoise hand dealing freeze damage to {handHit.collider.name}");
                    }
                }
                break;

            case HandType.BlackInHand:
                if (invisibilityCharge >= invisibilityMax && !isInvisible)
                {
                    isInvisible = true;
                    invisibilityTimer = invisibilityDuration;
                    invisibilityCharge = 0;
                    Debug.Log("[Weapon] Invisibility activated");
                }
                break;

            case HandType.ColorfulInHand:
                bool hitSomething = Physics.Raycast(rayOrigin, rayDirection, out handHit, Mathf.Infinity);
                Vector3 endPoint = hitSomething ? handHit.point : rayOrigin + rayDirection * 100f;
                LineRenderer colorfulLr = Manager.Instance.GetLineRenderer(new Color(Random.value, Random.value, Random.value));
                Manager.Instance.ShowLineRenderer(rayOrigin, endPoint, colorfulLr, Manager.Instance.sparks);
                if (hitSomething)
                {
                    Enemy enemy = handHit.collider.GetComponent<Enemy>();
                    if (enemy != null)
                    {
                        enemy.Damage(10 * Time.deltaTime);
                        Debug.Log($"[Weapon] Colorful hand dealing damage to {handHit.collider.name}");
                    }
                }
                Debug.Log($"[Weapon] Colorful hand used, hit: {(hitSomething ? handHit.point.ToString() : "nothing")}");
                break;
        }
    }

    void ReleaseHand()
    {
        
    }

    void ShootOil()
    {
        Vector3 rayOrigin = handHolder.position;
        Vector3 rayDirection = camera.forward;
        RaycastHit oilHit;
        if (Physics.Raycast(rayOrigin, rayDirection, out oilHit, hookRange))
        {
            Enemy enemy = oilHit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.ApplyEffect(Enemy.EffectType.Oil);
                Debug.Log($"[Weapon] Oil applied to enemy: {oilHit.collider.name}");
            }
        }
    }

    void UseFire()
    {
        Vector3 rayOrigin = handHolder.position;
        Vector3 rayDirection = camera.forward;
        RaycastHit fireHit;
        if (Physics.Raycast(rayOrigin, rayDirection, out fireHit, hookRange))
        {
            Enemy enemy = fireHit.collider.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Damage(fireDamagePerSecond * Time.deltaTime);
                enemy.ApplyEffect(Enemy.EffectType.Burning);
                if (enemy.HasEffect(Enemy.EffectType.Oil))
                {
                    enemy.ApplyEffect(Enemy.EffectType.DoubleBurning);
                }
                Debug.Log($"[Weapon] Fire applied to enemy: {fireHit.collider.name}");
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "Ammo")
        {
            Destroy(collision.gameObject);
            foreach (WeaponBase weapon in weapons) weapon.ammo += weapon.maxAmmo * float.Parse(collision.gameObject.name);
            if (float.Parse(collision.gameObject.name) > 1) Manager.Instance.Pause(0.5f);
            Manager.Instance.Flash();
            Manager.Instance.Sound(AmmoPickupSound);
        }
        else if (collision.gameObject.tag == "BWeapon")
        {
            Destroy(collision.gameObject);
            weaponsCount += int.Parse(collision.gameObject.name);
            Manager.Instance.Pause(0.5f);
            Manager.Instance.Flash();
            Manager.Instance.Sound(PickupSound);
        }
    }
}