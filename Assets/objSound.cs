using UnityEngine;
using UnityEngine.Audio;

public class objSound : MonoBehaviour {
	
	public static void refreshSound(GameObject obj, string audioCue) {
		obj.AddComponent<SuperpoweredSpatializer> ();
		AudioSource voice = obj.AddComponent<AudioSource> ();
        AudioMixer master = Resources.Load("spatializerreverb", typeof(AudioMixer)) as AudioMixer;
        voice.outputAudioMixerGroup = master.FindMatchingGroups ("Master") [0];
		
		AudioClip cue;
		cue = Resources.Load((string) audioCue, typeof(AudioClip)) as AudioClip;
        voice.clip = cue;
	
		voice.loop = true;
		voice.spatialize = true;
		voice.spatialBlend = 1.0f;
		voice.rolloffMode = AudioRolloffMode.Logarithmic;
		voice.maxDistance = 150;
		voice.minDistance = 1;
		voice.Play();
	}
}
