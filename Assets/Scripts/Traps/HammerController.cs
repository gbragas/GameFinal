using UnityEngine;

public class HammerController : MonoBehaviour
{
    public float speed = 2f;
    public float maxAngle = 90f;

    private Quaternion startRotation;

    void Start()
    {
        startRotation = transform.localRotation;
    }

    void Update()
    {
        float angle = Mathf.Sin(Time.time * speed) * maxAngle;

        transform.localRotation = startRotation * Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
