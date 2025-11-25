using System.Collections;
using UnityEngine;

public class SlidingDoor : Door
{
    [SerializeField] private GameObject leftHalf;
    [SerializeField] private GameObject rightHalf;
    [SerializeField] private Vector3 openOffset = new Vector3(1f, 0f, 0f);

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;

    protected override void Start()
    {
        base.Start();
        if (leftHalf != null) leftClosedPos = leftHalf.transform.localPosition;
        if (rightHalf != null) rightClosedPos = rightHalf.transform.localPosition;
    }

    protected override void OpenDoor()
    {
        isOpen = true;
        StartCoroutine(MoveDoor(leftHalf, leftClosedPos - openOffset, speed));
        StartCoroutine(MoveDoor(rightHalf, rightClosedPos + openOffset, speed));
    }

    protected override void CloseDoor()
    {
        isOpen = false;
        StartCoroutine(MoveDoor(leftHalf, leftClosedPos, speed));
        StartCoroutine(MoveDoor(rightHalf, rightClosedPos, speed));
    }

    private IEnumerator MoveDoor(GameObject doorPart, Vector3 targetPos, float speed)
    {
        if (doorPart == null) yield break;
        while (Vector3.Distance(doorPart.transform.localPosition, targetPos) > 0.01f)
        {
            doorPart.transform.localPosition = Vector3.MoveTowards(doorPart.transform.localPosition, targetPos, speed * Time.deltaTime);
            yield return null;
        }
    }
}