using UnityEngine;

public class MouseTrailPoints3D : MonoBehaviour
{
    public GameObject pointPrefab;      // Le prefab de ton point (sphère)
    public float spacing = 0.1f;        // Distance minimale entre deux points
    public float distanceFromCamera = 10f; // Distance devant la caméra

    private Vector3 lastPosition;

    void Start()
    {
        // On enregistre la position de départ de la souris
        lastPosition = GetMouseWorldPosition();
        CreatePoint(lastPosition);
    }

    void Update()
    {
        Vector3 mousePos = GetMouseWorldPosition();

        // Crée un nouveau point si la souris s’est déplacée suffisamment
        if (Vector3.Distance(lastPosition, mousePos) >= spacing)
        {
            CreatePoint(mousePos);
            lastPosition = mousePos;
        }
    }

    // Convertit la position écran de la souris en position monde 3D
    Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z) + distanceFromCamera;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    // Instancie un point à la position donnée
    void CreatePoint(Vector3 position)
    {
        Instantiate(pointPrefab, position, Quaternion.identity);
    }
}
