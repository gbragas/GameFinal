using UnityEngine;

public class SawController : MonoBehaviour
{
    public float rotationSpeed = 360f;
    public float moveSpeed = 2f;
    public float distance = 1.5f;

    private Vector3 startLocalPos;

    void Start()
    {
        startLocalPos = transform.localPosition;
    }

    void Update()
    {
        Rotate();
        Move();
    }

    void Rotate()
    {
        transform.rotation *= Quaternion.AngleAxis(rotationSpeed * Time.deltaTime, Vector3.right);
    }

    void Move()
    {
        if (distance <= 0f) return;

        float offset = Mathf.PingPong(Time.time * moveSpeed, distance * 2f) - distance;

        if (float.IsNaN(offset)) return;

        transform.localPosition = startLocalPos + new Vector3(0f, 0f, offset);
    }
}