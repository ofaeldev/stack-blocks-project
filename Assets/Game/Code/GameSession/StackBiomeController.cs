using UnityEngine;

public class StackBiomeController : MonoBehaviour
{
    [SerializeField] private int blocksPerBiome = 20;
    [SerializeField] private Color[] skyColors =
    {
        new(0.21f, 0.34f, 0.52f),
        new(0.47f, 0.58f, 0.75f),
        new(0.05f, 0.07f, 0.16f),
        new(0.3f, 0.05f, 0.35f)
    };

    [SerializeField] private Color[] ambientColors =
    {
        new(0.72f, 0.78f, 0.86f),
        new(0.8f, 0.82f, 0.9f),
        new(0.32f, 0.38f, 0.58f),
        new(0.7f, 0.28f, 0.85f)
    };

    private Camera sceneCamera;
    private int currentBiome = -1;

    public static StackBiomeController Create(Camera camera)
    {
        GameObject biomeObject = new("StackBiomeController");
        StackBiomeController biome = biomeObject.AddComponent<StackBiomeController>();
        biome.sceneCamera = camera;

        return biome;
    }

    public string UpdateForBlocks(int blockCount)
    {
        int biomeIndex = Mathf.Clamp(blockCount / blocksPerBiome, 0, skyColors.Length - 1);

        if (biomeIndex == currentBiome)
        {
            return GetBiomeName(biomeIndex);
        }

        currentBiome = biomeIndex;

        if (sceneCamera != null)
        {
            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = skyColors[biomeIndex];
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientColors[biomeIndex];

        return GetBiomeName(biomeIndex);
    }

    public void ResetBiome()
    {
        currentBiome = -1;
        UpdateForBlocks(0);
    }

    private static string GetBiomeName(int biomeIndex)
    {
        return biomeIndex switch
        {
            0 => "City",
            1 => "Clouds",
            2 => "Orbit",
            _ => "Glitch"
        };
    }
}
