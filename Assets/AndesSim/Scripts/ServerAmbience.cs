using UnityEngine;

namespace AndesSim
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class ServerAmbience : MonoBehaviour
    {
        [Range(0, 1)] public float volume = 0.14f;
        AudioClip generatedClip;

        void Start()
        {
            const int sampleRate = 22050;
            const int seconds = 4;
            var samples = new float[sampleRate * seconds];
            var noise = new System.Random(3042016);
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)sampleRate;
                float hum = Mathf.Sin(time * Mathf.PI * 2 * 60) * 0.15f + Mathf.Sin(time * Mathf.PI * 2 * 120) * 0.07f;
                samples[i] = hum + ((float)noise.NextDouble() * 2 - 1) * 0.035f;
            }
            generatedClip = AudioClip.Create("ANDES_SIM_ServerHum_Original", samples.Length, 1, sampleRate, false);
            generatedClip.SetData(samples, 0);
            var source = GetComponent<AudioSource>();
            source.clip = generatedClip;
            source.loop = true;
            source.spatialBlend = 0;
            source.volume = volume;
            source.Play();
        }

        void OnDestroy()
        {
            if (generatedClip != null) Destroy(generatedClip);
        }
    }
}
