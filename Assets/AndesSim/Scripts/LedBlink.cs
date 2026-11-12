using UnityEngine;

namespace AndesSim
{
    public sealed class LedBlink : MonoBehaviour
    {
        public Renderer[] leds;
        public Color color = new Color(0.1f, 1f, 0.48f);
        public float period = 1.6f;
        MaterialPropertyBlock properties;

        void Awake() => properties = new MaterialPropertyBlock();

        void Update()
        {
            if (leds == null) return;
            for (int i = 0; i < leds.Length; i++)
            {
                if (leds[i] == null) continue;
                float signal = Mathf.PerlinNoise(i * 13.3f + transform.position.x, Time.time / Mathf.Max(0.1f, period));
                float brightness = signal > 0.45f ? 1.8f : 0.2f;
                leds[i].GetPropertyBlock(properties);
                properties.SetColor("_EmissionColor", color * brightness);
                leds[i].SetPropertyBlock(properties);
            }
        }
    }
}
