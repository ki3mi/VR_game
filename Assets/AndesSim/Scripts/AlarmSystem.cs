using UnityEngine;

namespace AndesSim
{
    [DisallowMultipleComponent]
    public sealed class AlarmSystem : MonoBehaviour
    {
        public GameManager game;
        public Light redLight;
        public Renderer redBeacon;
        [Range(0f, 1f)] public float volume = 0.26f;

        public bool IsActive { get; private set; }
        public bool IsPlaying => source != null && source.isPlaying;

        AudioSource source;
        AudioClip clip;

        void Awake()
        {
            clip = CreateAlarm();
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0;
            source.volume = volume;
            source.clip = clip;
            if (redLight != null) redLight.color = new Color(1f, 0.06f, 0.025f);
        }

        void OnEnable()
        {
            if (game != null) game.Changed += OnGameChanged;
            OnGameChanged();
        }

        void OnDisable()
        {
            if (game != null) game.Changed -= OnGameChanged;
            SetActive(false);
        }

        void OnDestroy()
        {
            if (source != null) Destroy(source);
            if (clip != null) Destroy(clip);
        }

        void OnGameChanged() => SetActive(game != null && game.SessionBlocked);

        /// <summary>Acknowledge the sound while retaining the visible alarm and evidence.</summary>
        public void Silence()
        {
            if (source != null) source.Stop();
        }

        void SetActive(bool value)
        {
            if (redLight != null) redLight.enabled = value;
            if (redBeacon != null) redBeacon.enabled = value;
            if (IsActive == value) return;

            IsActive = value;
            if (source == null) return;
            if (value) source.Play();
            else source.Stop();
        }

        static AudioClip CreateAlarm()
        {
            const int sampleRate = 22050;
            const float duration = 1.45f;
            const float pulseLength = 0.22f;
            const float secondPulse = 0.34f;
            var samples = new float[Mathf.CeilToInt(duration * sampleRate)];

            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)sampleRate;
                float pulseTime = time < secondPulse ? time : time - secondPulse;
                if (pulseTime >= pulseLength) continue;

                float frequency = time < secondPulse ? 520f : 660f;
                float envelope = Mathf.Clamp01(pulseTime / 0.025f) * Mathf.Clamp01((pulseLength - pulseTime) / 0.05f);
                samples[i] = 0.48f * envelope * Mathf.Sin(2f * Mathf.PI * frequency * pulseTime);
            }

            var result = AudioClip.Create("AndesSim_Alarma_Acceso", samples.Length, 1, sampleRate, false);
            result.SetData(samples, 0);
            return result;
        }
    }
}
