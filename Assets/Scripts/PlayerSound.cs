using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSound : MonoBehaviour
{

    [Header("Weapon")]
    public AudioClip[] swingSounds;

    [Header("Walking")]
    public List<AudioClip> grassWalk;
    public List<AudioClip> rockWalk;
    public List<AudioClip> dirtWalk;
    public List<AudioClip> woodWalk;

    [Header("Runnig")]
    public List<AudioClip> grassRun;
    public List<AudioClip> rockRun;
    public List<AudioClip> dirtRun;
    public List<AudioClip> woodRun;

    [Header("Jumpig")]
    public List<AudioClip> grassJump;
    public List<AudioClip> rockJump;
    public List<AudioClip> dirtJump;
    public List<AudioClip> woodJump;

    enum GroundMaterial
    {
        Grass, Rock, Dirt, Wood, Empty
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

    private GroundMaterial SurfaceSelect()
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
                    return GroundMaterial.Grass;
                }
                else if (surfaceMaterial.name.Contains("Rock"))
                {
                    return GroundMaterial.Rock;
                }
                else if (surfaceMaterial.name.Contains("Wood"))
                {
                    return GroundMaterial.Wood;
                }
                else
                {
                    return GroundMaterial.Empty;
                }
            }
        }
        return GroundMaterial.Empty;
    }

    void PlayFootsetp()
    {
        AudioClip clip = null;

        GroundMaterial surface = SurfaceSelect();

		switch (surface)
		{
			case GroundMaterial.Grass:
                clip = grassWalk[Random.Range(0, grassWalk.Count)];
				break;
			case GroundMaterial.Rock:
                clip = rockWalk[Random.Range(0, rockWalk.Count)];
				break;
			case GroundMaterial.Dirt:
                clip = dirtWalk[Random.Range(0, dirtWalk.Count)];
				break;
			case GroundMaterial.Wood:
                clip = woodWalk[Random.Range(0, woodWalk.Count)];
				break;
			default:
				break;
		}

        Debug.Log(surface);

        if (surface != GroundMaterial.Empty)
        {
            footstepSource.clip = clip;
            footstepSource.volume = Random.Range(0.02f, 0.05f);
            footstepSource.pitch = Random.Range(0.8f, 1.2f);
            footstepSource.Play();
        }
	}
    void PlayRunFootsetp()
    {
        AudioClip clip = null;

        GroundMaterial surface = SurfaceSelect();

		switch (surface)
		{
			case GroundMaterial.Grass:
                clip = grassRun[Random.Range(0, grassRun.Count)];
				break;
			case GroundMaterial.Rock:
                clip = rockRun[Random.Range(0, rockRun.Count)];
				break;
			case GroundMaterial.Dirt:
                clip = dirtRun[Random.Range(0, dirtRun.Count)];
				break;
			case GroundMaterial.Wood:
                clip = woodRun[Random.Range(0, woodRun.Count)];
				break;
			default:
				break;
		}

        Debug.Log(surface);

        if (surface != GroundMaterial.Empty)
        {
            footstepSource.clip = clip;
            footstepSource.volume = Random.Range(0.02f, 0.05f);
            footstepSource.pitch = Random.Range(0.8f, 1.2f);
            footstepSource.Play();
        }
	}
    void PlayJump()
    {
        AudioClip clip = null;

        GroundMaterial surface = SurfaceSelect();

		switch (surface)
		{
			case GroundMaterial.Grass:
                clip = grassJump[Random.Range(0, grassJump.Count)];
				break;
			case GroundMaterial.Rock:
                clip = rockJump[Random.Range(0, rockJump.Count)];
				break;
			case GroundMaterial.Dirt:
                clip = dirtJump[Random.Range(0, dirtJump.Count)];
				break;
			case GroundMaterial.Wood:
                clip = woodJump[Random.Range(0, woodJump.Count)];
				break;
			default:
				break;
		}

        Debug.Log(surface);

        if (surface != GroundMaterial.Empty)
        {
            footstepSource.clip = clip;
            footstepSource.volume = Random.Range(0.02f, 0.05f);
            footstepSource.pitch = Random.Range(0.8f, 1.2f);
            footstepSource.Play();
        }
	}
}
