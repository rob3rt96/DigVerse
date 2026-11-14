using UnityEngine;

/// <summary>
/// Gestionează fundalul subteran cu gradiente circulare (Radial Gradient)
/// și schimbarea culorii pe adâncimi conform cerințelor:
/// 0 la -1000m: Maro foarte deschis
/// -1000m la -1500m: Maro închis
/// -1500m la -2000m: Negru
/// -2000m la -3500m: Negru roșiatic
/// -3500m la -4500m: Verde negricios
/// de la -5500m încolo: Roșu puțin întunecat
/// </summary>
public class SubterraneanBackground : MonoBehaviour
{
    [Header("Referințe")]
    public Transform targetCamera;
    public Transform player;

    [Header("Configurare Suprafață")]
    public float surfaceY = 0f;

    [Header("Culori pe Straturi (0 la -8000m)")]
    public Color skyColor = new Color(0.29f, 0.56f, 0.89f);          // Cer albastru la suprafață
    public Color depth0To1000 = new Color(0.60f, 0.44f, 0.30f);      // Maro foarte deschis
    public Color depth1500 = new Color(0.24f, 0.13f, 0.06f);         // Maro închis
    public Color depth2000 = new Color(0.04f, 0.04f, 0.04f);         // Negru
    public Color depth3500 = new Color(0.18f, 0.04f, 0.04f);         // Negru roșiatic
    public Color depth4500 = new Color(0.03f, 0.12f, 0.04f);         // Verde negricios
    public Color depth5500Plus = new Color(0.35f, 0.06f, 0.06f);     // Roșu puțin întunecat

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Asigurăm că fundalul este desenat în spatele pământului
        spriteRenderer.sortingOrder = -20;

        // Încărcăm textura de gradient circular dacă nu e setată
        if (spriteRenderer.sprite == null)
        {
            Sprite radialSprite = Resources.Load<Sprite>("RadialGradient");
            if (radialSprite == null)
            {
                // Încărcăm din folderul Assets dacă nu e în Resources
                Texture2D tex = Resources.Load<Texture2D>("RadialGradient");
                if (tex != null)
                {
                    radialSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
                }
            }
            if (radialSprite != null)
            {
                spriteRenderer.sprite = radialSprite;
            }
        }
    }

    void Start()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
        if (player == null)
        {
            GameObject p = GameObject.Find("Player");
            if (p != null) player = p.transform;
        }

        // Setăm cerul ca fundal nativ al camerei pentru suprafață (Nivelul 0)
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = skyColor;
        }

        // Mărim scara fundalului pentru a acoperi tot ecranul pe lățime și adâncime
        transform.localScale = new Vector3(120f, 100f, 1f);
    }

    void LateUpdate()
    {
        // Fundalul subteran urmărește camera, dar marginea sa superioară nu depășește niciodată suprafața (surfaceY)
        if (targetCamera != null)
        {
            float halfHeight = transform.localScale.y * 0.5f;
            float targetY = targetCamera.position.y;
            // Dacă privim spre cer, fundalul subteran rămâne la suprafață (sub iarbă)
            if (targetY + halfHeight > surfaceY)
            {
                targetY = surfaceY - halfHeight;
            }

            transform.position = new Vector3(targetCamera.position.x, targetY, 5f);
        }

        // Calculăm culoarea în funcție de adâncimea curentă
        float currentY = player != null ? player.position.y : (targetCamera != null ? targetCamera.position.y : 0f);
        float depth = Mathf.Max(0f, surfaceY - currentY);

        Color targetColor = GetColorAtDepth(depth);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, 5f * Time.deltaTime);
        }
    }

    public Color GetColorAtDepth(float depth)
    {
        if (depth <= 1000f)
        {
            // 0 la 1000m: Maro foarte deschis (fondul pământului săpat / golurilor)
            return depth0To1000;
        }
        else if (depth <= 1500f)
        {
            // 1000 la 1500m: Tranziție spre maro închis
            float t = (depth - 1000f) / 500f;
            return Color.Lerp(depth0To1000, depth1500, t);
        }
        else if (depth <= 2000f)
        {
            // 1500 la 2000m: Tranziție spre negru
            float t = (depth - 1500f) / 500f;
            return Color.Lerp(depth1500, depth2000, t);
        }
        else if (depth <= 3500f)
        {
            // 2000 la 3500m: Tranziție spre negru roșiatic
            float t = (depth - 2000f) / 1500f;
            return Color.Lerp(depth2000, depth3500, t);
        }
        else if (depth <= 4500f)
        {
            // 3500 la 4500m: Tranziție spre verde negricios
            float t = (depth - 3500f) / 1000f;
            return Color.Lerp(depth3500, depth4500, t);
        }
        else if (depth <= 5500f)
        {
            // 4500 la 5500m: Tranziție spre roșu întunecat
            float t = (depth - 4500f) / 1000f;
            return Color.Lerp(depth4500, depth5500Plus, t);
        }
        else
        {
            // 5500m la 8000m+: Roșu puțin întunecat
            return depth5500Plus;
        }
    }
}
