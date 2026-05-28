using UnityEngine;

public class PlacementGuide : MonoBehaviour
{
    [SerializeField] private Color validColor = new(0.1f, 1f, 0.25f, 0.45f);
    [SerializeField] private Color invalidColor = new(1f, 0.1f, 0.05f, 0.35f);
    [SerializeField] private float pulseAmount = 0.08f;
    [SerializeField] private float pulseSpeed = 8f;

    private Renderer guideRenderer;
    private Material guideMaterial;
    private Vector3 baseScale;

    public static PlacementGuide Create(float radius)
    {
        GameObject guideObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        guideObject.name = "PlacementGuide";

        Collider guideCollider = guideObject.GetComponent<Collider>();
        Destroy(guideCollider);

        PlacementGuide guide = guideObject.AddComponent<PlacementGuide>();
        guide.SetRadius(radius);

        return guide;
    }

    private void Awake()
    {
        guideRenderer = GetComponent<Renderer>();
        guideMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        guideMaterial.SetFloat("_Surface", 1f);
        guideMaterial.SetFloat("_AlphaClip", 0f);
        guideRenderer.material = guideMaterial;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y, baseScale.z * pulse);
    }

    public void SetTarget(Vector3 targetPosition, float radius, float blockHeight)
    {
        SetRadius(radius);
        transform.position = new Vector3(targetPosition.x, targetPosition.y - blockHeight * 0.5f + 0.03f, targetPosition.z);
    }

    public void SetState(bool isValid)
    {
        guideMaterial.color = isValid ? validColor : invalidColor;
    }

    public void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }

    private void SetRadius(float radius)
    {
        baseScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
        transform.localScale = baseScale;
    }
}
