using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ClickPointsWithLine : MonoBehaviour
{
    public GameObject pointPrefab;      // prefab du point (sphère)
    public Camera mainCamera;           // caméra principale
    public LineRenderer lineRenderer;   // line renderer pour relier les points
    public float spawnDistance = 10f;   // distance par défaut si rien n'est touché
    public float scrollSpeed = 5f;      // vitesse de zoom

    private List<Vector3> points = new List<Vector3>(); // stockage des points

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (lineRenderer == null) Debug.LogError("LineRenderer non assigné !");
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        if (Mouse.current == null) return;
        Vector2 scroll = Mouse.current.scroll.ReadValue();
        spawnDistance += scroll.y * scrollSpeed;
        spawnDistance = Mathf.Clamp(spawnDistance, 1f, 100f);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            Vector3 spawnPos;

            // 👇 On essaie d'abord de toucher le plan
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                spawnPos = hit.point; // point sur le plan
            }
            else
            {
                spawnPos = ray.origin + ray.direction * spawnDistance; // point libre dans l'espace
            }

            AddPoint(spawnPos);
        }

#else
        spawnDistance += Input.mouseScrollDelta.y * scrollSpeed;
        spawnDistance = Mathf.Clamp(spawnDistance, 1f, 100f);

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 spawnPos;

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                spawnPos = hit.point; // touche un plan
            }
            else
            {
                spawnPos = ray.origin + ray.direction * spawnDistance; // rien de touché
            }

            AddPoint(spawnPos);
        }
#endif
    }

    void AddPoint(Vector3 position)
    {
        // Crée un point visuel
        Instantiate(pointPrefab, position, Quaternion.identity);

        // Ajoute la position
        points.Add(position);

        // Met à jour la ligne
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }
}

