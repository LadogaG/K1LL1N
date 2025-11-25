using System.Collections;
using UnityEngine;

public class SwingDoor : Door
{
    [SerializeField] GameObject leftHalf;
    [SerializeField] GameObject rightHalf;
    [SerializeField] float swingAngle = 90f;

    private Quaternion leftClosedRot;
    private Quaternion rightClosedRot;

    protected override void Start()
    {
        base.Start();
        if (leftHalf != null) leftClosedRot = leftHalf.transform.localRotation;
        if (rightHalf != null) rightClosedRot = rightHalf.transform.localRotation;
    }

    protected override void OpenDoor()
    {
        isOpen = true;
        Vector3 direction = (player.transform.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.right, direction);

        if (dot > 0)
        {
            StartCoroutine(RotateDoor(leftHalf, leftClosedRot * Quaternion.Euler(0, -swingAngle, 0), speed));
            StartCoroutine(RotateDoor(rightHalf, rightClosedRot * Quaternion.Euler(0, swingAngle, 0), speed));
        }
        else
        {
            StartCoroutine(RotateDoor(leftHalf, leftClosedRot * Quaternion.Euler(0, swingAngle, 0), speed));
            StartCoroutine(RotateDoor(rightHalf, rightClosedRot * Quaternion.Euler(0, -swingAngle, 0), speed));
        }
    }

    protected override void CloseDoor()
    {
        isOpen = false;
        StartCoroutine(RotateDoor(leftHalf, leftClosedRot, speed));
        StartCoroutine(RotateDoor(rightHalf, rightClosedRot, speed));
    }

    private IEnumerator RotateDoor(GameObject doorPart, Quaternion targetRot, float speed)
    {
        if (doorPart == null) yield break;
        while (Quaternion.Angle(doorPart.transform.localRotation, targetRot) > 0.1f)
        {
            doorPart.transform.localRotation = Quaternion.RotateTowards(doorPart.transform.localRotation, targetRot, speed * Time.deltaTime * 100f);
            yield return null;
        }
    }
}