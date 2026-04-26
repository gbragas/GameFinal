using UnityEngine;
using UnityEngine.Rendering;

public class MirrorReflectionScript : MonoBehaviour
{
    private MirrorCameraScript childScript;

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void Start()
    {
        childScript = gameObject.transform.parent.gameObject.GetComponentInChildren<MirrorCameraScript>();

        if (childScript == null)
        {
            Debug.LogError("Child script (MirrorCameraScript) should be in sibling object");
        }
    }

    // No URP, usamos esse callback em vez de OnWillRenderObject
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (childScript != null && (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.SceneView))
        {
            childScript.RenderMirror(camera);
        }
    }
}
