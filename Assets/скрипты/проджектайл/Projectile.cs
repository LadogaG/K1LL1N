using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    [Header("Projectile")]
    public bool? player;
    public enum EffectType { Burning, DoubleBurning, Oil, Freeze, Electric }
    public List<EffectType> activeEffects = new List<EffectType>();
    protected Rigidbody rb;

    public abstract void Fire(bool? isPlayer, Vector3 direction, float speed = 15, float spread = 0, List<EffectType> effectTypes = null, bool c = false);
}