using UnityEngine;

public class PlataformController : MonoBehaviour
{
    [Header("Rotação")]
    public float rotationSpeed = 40f;


    void Start()
    {
        
    }

    void Update()
    {
        Rotate();
    }

    void Rotate()
    {
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime);
    }
}
