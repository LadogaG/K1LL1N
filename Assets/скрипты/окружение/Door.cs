using UnityEngine;

public abstract class Door : MonoBehaviour
{
    protected GameObject player;
    [SerializeField] protected float speed = 5f;
    [SerializeField] protected float openDistance = 5f;
    [SerializeField] protected float closeDistance = 5f;
    [SerializeField] protected GameObject lockIndicator;

    protected bool isLocked = false;
    protected bool isOpen = false;

    protected virtual void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (lockIndicator != null)
        {
            lockIndicator.SetActive(false);
        }
    }

    protected virtual void Update()
    {
        if (isLocked) return;

        float distance = Vector3.Distance(transform.position, player.transform.position);
        if (distance <= openDistance && !isOpen)
        {
            OpenDoor();
        }
        else if (distance > closeDistance && isOpen)
        {
            CloseDoor();
        }
    }

    public void LockDoor()
    {
        isLocked = true;
        CloseDoor();
        if (lockIndicator != null)
        {
            lockIndicator.SetActive(true);
        }
    }

    public void UnlockDoor()
    {
        isLocked = false;
        if (lockIndicator != null)
        {
            lockIndicator.SetActive(false);
        }
    }

    protected abstract void OpenDoor();
    protected abstract void CloseDoor();
}