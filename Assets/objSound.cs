using UnityEngine;
using UnityEngine.Audio;

public class objSound : MonoBehaviour {
	public static void refreshSound(GameObject obj) {
		obj.AddComponent<SuperpoweredSpatializer> ();
		AudioSource voice = obj.AddComponent<AudioSource> ();
        AudioMixer master = Resources.Load("spatializerreverb", typeof(AudioMixer)) as AudioMixer;
        voice.outputAudioMixerGroup = master.FindMatchingGroups ("Master") [0];
		AudioClip speech;
        speech = Resources.Load("speech", typeof(AudioClip)) as AudioClip;
        voice.clip = speech;
	
		voice.loop = true;
		voice.spatialize = true;
		voice.spatialBlend = 1.0f;
		voice.rolloffMode = AudioRolloffMode.Logarithmic;
		voice.maxDistance = 150;
		voice.minDistance = 1;
		voice.Play();
	}
}
