using UnityEngine;

public class Spin : MonoBehaviour
{
    [SerializeField] float speed = 45;
    Vector3 dir;

    void Awake()
    {
        dir = new Vector3(Random.Range(-speed, speed), Random.Range(-speed, speed), Random.Range(-speed, speed));
    }

    void Update()
    {
        float spd = 90f * Time.deltaTime;
        transform.Rotate(dir.x * spd, dir.y * spd, dir.z * spd);
    }
}