using UnityEngine;

public class Wall : Enemy
{
    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        ApplyEffects();
    }

    protected override void ApplyEffects()
    {
        base.ApplyEffects();
    }
}