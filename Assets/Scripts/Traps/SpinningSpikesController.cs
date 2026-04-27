using UnityEngine;

public class SpinningSpikesController : MonoBehaviour
{
    public float speed = 100f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per framepublic float speed = 100f;
    void Update()
    {
        Rotate();
    }

    void Rotate()
    {
        transform.Rotate(-Vector3.up * speed * Time.deltaTime);
    }
}
