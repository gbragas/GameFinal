using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterSound : MonoBehaviour
{
	[Header("Sounds")]
	public List<AudioClip> idleSounds;

    private AudioSource audioSource;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		audioSource = GetComponent<AudioSource>();
    }

	// public void PlayNoise()
	// {
	// 	AudioClip clip = null;

	// 	clip = idleSounds[Random.Range(0, idleSounds.Count)];

	// 	audioSource.clip = clip;
	// 	audioSource.volume = Random.Range(minRange, maxRange);
	// 	audioSource.pitch = Random.Range(0.8f, 1.2f);
	// 	audioSource.Play();
	// }
}
