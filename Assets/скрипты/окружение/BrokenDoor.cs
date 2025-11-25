using UnityEngine;

public class BrokenDoor : Door
{
    bool isArena = false;

    protected override void OpenDoor()
    {
        if (!isArena) isArena = isLocked;
        if (!isArena || isLocked) return;

        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 10f;
            rb.useGravity = true;
        }
        Destroy(this);
    }

    protected override void CloseDoor()
    {

    }

    public void ForceOpen()
    {
        OpenDoor();
    }
}