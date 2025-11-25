using System.Collections;
using UnityEngine;

public class UpDoor : Door
{
    [SerializeField] Vector3 openOffset = new Vector3(0f, 2f, 0f);

    private Vector3 closedPos;

    protected override void Start()
    {
        base.Start();
        closedPos = transform.localPosition;
    }

    protected override void OpenDoor()
    {
        isOpen = true;
        StartCoroutine(MoveDoor(closedPos + openOffset, speed));
    }

    protected override void CloseDoor()
    {
        isOpen = false;
        StartCoroutine(MoveDoor(closedPos, speed));
    }

    private IEnumerator MoveDoor(Vector3 targetPos, float speed)
    {
        while (Vector3.Distance(transform.localPosition, targetPos) > 0.01f)
        {
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, speed * Time.deltaTime);
            yield return null;
        }
    }
}