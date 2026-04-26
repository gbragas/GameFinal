using UnityEngine;
using System.Collections;

public class SpikesController : MonoBehaviour
{
    public float downY = -0.6f;
    public float speed = 2f;
    public float waitTime = 1f;

    private Vector3 startPos;
    private Vector3 downPos;

    void Start()
    {
        startPos = transform.position;
        downPos = new Vector3(startPos.x, downY, startPos.z);

        StartCoroutine(MoveCycle());
    }

    IEnumerator MoveCycle()
    {
        while (true)
        {
            // Desce
            yield return MoveTo(downPos);

            // Espera embaixo
            yield return new WaitForSeconds(waitTime);

            // Sobe
            yield return MoveTo(startPos);

            // Espera em cima (opcional)
            yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator MoveTo(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                speed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = target;
    }
}
