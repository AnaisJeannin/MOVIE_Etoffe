using UnityEngine;

public class ClickDepthControl : MonoBehaviour
{
    public GameObject pointPrefab;     // ton prefab de sphère
    public Camera mainCamera;          // la caméra utilisée
    public float spawnDistance = 10f;  // distance initiale devant la caméra
    public float scrollSpeed = 5f;     // vitesse de changement de profondeur

    void Start()
    {
        // Si la caméra n'est pas assignée, on prend la principale
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        // Contrôle de la profondeur avec la molette
        spawnDistance += Input.mouseScrollDelta.y * scrollSpeed;
        spawnDistance = Mathf.Clamp(spawnDistance, 1f, 100f); // borne entre 1 et 100 unités

        // Clic gauche -> création du point
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 spawnPos = ray.origin + ray.direction * spawnDistance;

            Instantiate(pointPrefab, spawnPos, Quaternion.identity);
        }
    }
}

