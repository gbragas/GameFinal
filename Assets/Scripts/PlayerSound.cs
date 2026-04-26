using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSound : MonoBehaviour
{

    [Header("Weapon")]
    public AudioClip[] swingSounds;

    [Header("Footsteps")]
    public List<AudioClip> grassFS;
    public List<AudioClip> rockFS;
    public List<AudioClip> woodFS;

    enum FSMaterial
    {
        Grass, Rock, Wood, Empty
    }

    private AudioSource weaponSource, footstepSource;

    void Start()
    {
        weaponSource = GetComponents<AudioSource>()[0];
        footstepSource = GetComponents<AudioSource>()[1];
    }

    void PlaySwing()
    {
        AudioClip clip = swingSounds[Random.Range(0, swingSounds.Length)];
        weaponSource.clip = clip;
        weaponSource.Play();
        Debug.Log(clip.name);
    }

    private FSMaterial SurfaceSelect()
    {
        RaycastHit hit;
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, -Vector3.up);
        Material surfaceMaterial;

        if(Physics.Raycast(ray, out hit, 1.0f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
        {
            Renderer surfaceRenderer = hit.collider.GetComponentInChildren<Renderer>();
            if (surfaceRenderer)
            {
                surfaceMaterial = surfaceRenderer ? surfaceRenderer.sharedMaterial : null;
                if (surfaceMaterial.name.Contains("Grass"))
                {
                    return FSMaterial.Grass;
                }
                else if (surfaceMaterial.name.Contains("Rock"))
                {
                    return FSMaterial.Rock;
                }
                else if (surfaceMaterial.name.Contains("Wood"))
                {
                    return FSMaterial.Wood;
                }
                else
                {
                    return FSMaterial.Empty;
                }
            }
        }
        return FSMaterial.Empty;
    }

    void PlayFootsetp()
    {
        AudioClip clip = null;

        FSMaterial surface = SurfaceSelect();

		switch (surface)
		{
			case FSMaterial.Grass:
                clip = grassFS[Random.Range(0, grassFS.Count)];
				break;
			case FSMaterial.Rock:
                clip = rockFS[Random.Range(0, rockFS.Count)];
				break;
			case FSMaterial.Wood:
                clip = woodFS[Random.Range(0, woodFS.Count)];
				break;
			default:
				break;
		}

        Debug.Log(surface);

        if (surface != FSMaterial.Empty)
        {
            footstepSource.clip = clip;
            footstepSource.volume = Random.Range(0.02f, 0.05f);
            footstepSource.pitch = Random.Range(0.8f, 1.2f);
            footstepSource.Play();
        }
	}
}
