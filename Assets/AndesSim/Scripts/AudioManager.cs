using UnityEngine;

namespace AndesSim
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioManager : MonoBehaviour
    {
        [Range(0f, 1f)] public float volume = 0.2f;

        AudioSource source;
        AudioClip notice;
        AudioClip evidence;
        AudioClip result;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            notice = CreateCue("AndesSim_Aviso", 0.14f, 440f, 493.88f);
            evidence = CreateCue("AndesSim_Evidencia", 0.23f, 523.25f, 659.25f);
            result = CreateCue("AndesSim_Resultado", 0.35f, 392f, 523.25f);
        }

        public void PlayNotice() => Play(notice);
        public void PlayEvidence() => Play(evidence);
        public void PlayResult() => Play(result);

        void Play(AudioClip clip)
        {
            if (source != null && source.isActiveAndEnabled && clip != null) source.PlayOneShot(clip, volume);
        }

        void OnDestroy()
        {
            if (notice != null) Destroy(notice);
            if (evidence != null) Destroy(evidence);
            if (result != null) Destroy(result);
        }

        static AudioClip CreateCue(string name, float duration, float fromHz, float toHz)
        {
            const int sampleRate = 22050;
            var samples = new float[Mathf.CeilToInt(duration * sampleRate)];
            float phase = 0f;

            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)sampleRate;
                float frequency = Mathf.Lerp(fromHz, toHz, time / duration);
                float envelope = Mathf.Clamp01(time / 0.015f) * Mathf.Clamp01((duration - time) / 0.08f);
                phase += 2f * Mathf.PI * frequency / sampleRate;
                samples[i] = 0.5f * envelope * Mathf.Sin(phase);
            }

            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
