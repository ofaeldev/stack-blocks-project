using UnityEngine;

public class StackFeedbackController : MonoBehaviour
{
    [SerializeField] private Color placementColor = new(0.2f, 1f, 0.45f);
    [SerializeField] private Color dangerColor = new(1f, 0.18f, 0.08f);
    [SerializeField] private float baseToneFrequency = 220f;

    private ParticleSystem placementParticles;
    private AudioSource audioSource;
    private int toneIndex;

    public static StackFeedbackController Create()
    {
        GameObject feedbackObject = new("StackFeedbackController");
        StackFeedbackController feedback = feedbackObject.AddComponent<StackFeedbackController>();
        feedback.Build();

        return feedback;
    }

    private void Build()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.55f;

        GameObject particleObject = new("PlacementParticles");
        particleObject.transform.SetParent(transform, false);
        placementParticles = particleObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = placementParticles.main;
        main.startLifetime = 0.35f;
        main.startSpeed = 2.4f;
        main.startSize = 0.08f;
        main.startColor = placementColor;
        main.maxParticles = 80;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = placementParticles.emission;
        emission.enabled = false;

        ParticleSystem.ShapeModule shape = placementParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.45f;
    }

    public void PlayPlacement(Vector3 position, int combo)
    {
        placementParticles.transform.position = position;

        ParticleSystem.MainModule main = placementParticles.main;
        main.startColor = combo > 1 ? Color.Lerp(placementColor, Color.white, 0.35f) : placementColor;
        placementParticles.Emit(18 + Mathf.Clamp(combo, 0, 8) * 4);

        float frequency = baseToneFrequency * Mathf.Pow(1.12246f, toneIndex % 8);
        toneIndex++;
        audioSource.PlayOneShot(CreateTone(frequency, 0.08f, 0.18f));
    }

    public void PlayDanger(Vector3 position)
    {
        placementParticles.transform.position = position;

        ParticleSystem.MainModule main = placementParticles.main;
        main.startColor = dangerColor;
        placementParticles.Emit(32);

        audioSource.PlayOneShot(CreateTone(90f, 0.16f, 0.25f));
    }

    private static AudioClip CreateTone(float frequency, float duration, float volume)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = 1f - time / duration;
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * volume * envelope;
        }

        AudioClip clip = AudioClip.Create("StackTone", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);

        return clip;
    }
}
