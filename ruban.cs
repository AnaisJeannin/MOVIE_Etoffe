using System;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshGeneratorV1 : MonoBehaviour
{
    private Mesh mesh;
    public event Action<Mesh> MeshCreated;

    private Vector3[] vertices;
    private int[] triangles;
    private int verticesCount = 4;
    private int trianglesCount = 2;

    private float lastClickTime = 0f;
    private float doubleClickDelay = 0.3f; // max time between two clicks

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        vertices = new Vector3[verticesCount];

        vertices[0] = new Vector3(0, 0, 0);
        vertices[1] = new Vector3(0, 1, 0);
        vertices[2] = new Vector3(1, 0, 0);
        vertices[3] = new Vector3(1, 1, 0);

        if (verticesCount > 4)
        {
            for (int i = 4; i < verticesCount; i += 2)
            {
                vertices[i] = new Vector3(i / 2, 0, 0);
                vertices[i + 1] = new Vector3(i / 2, 1, 0);
            }
        }

        triangles = new int[trianglesCount * 3];

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 3;
        triangles[4] = 2;
        triangles[5] = 1;

        if (trianglesCount > 2)
        {
            for (int i = 2; i < trianglesCount; i += 2)
            {
                int tri = i * 3;
                triangles[tri + 0] = i;
                triangles[tri + 1] = i + 1;
                triangles[tri + 2] = i + 2;
                triangles[tri + 3] = i + 3;
                triangles[tri + 4] = i + 2;
                triangles[tri + 5] = i + 1;
            }
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void Update()
    {
        if (Input.GetMouseButtonUp(0))
        {
            // double click check
            if (Time.time - lastClickTime < doubleClickDelay)
            {
                ToggleCloth();
            }
            else
            {
                verticesCount += 2;
                trianglesCount += 2;
                CreateShape();
                UpdateMesh();
                MeshCreated?.Invoke(mesh);
            }

            lastClickTime = Time.time;
        }
    }

    void ToggleCloth()
    {
        Cloth cloth = GetComponent<Cloth>();

        if (cloth == null)
        {
            cloth = gameObject.AddComponent<Cloth>();

            // Le Cloth utilisera automatiquement le MeshFilter existant
            cloth.useGravity = true;
            cloth.worldVelocityScale = 1.0f;
            cloth.worldAccelerationScale = 1.0f;
            cloth.damping = 0.2f;
            cloth.stretchingStiffness = 0.6f;
            cloth.bendingStiffness = 0.6f;

            // Facultatif : éviter qu’il tombe dans le vide en ajoutant un collider sol
            if (!FindObjectOfType<BoxCollider>())
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.transform.position = new Vector3(0, -2, 0);
            }

            Debug.Log("💡 Cloth activé !");
        }
        else
        {
            Destroy(cloth);
            Debug.Log("🧱 Cloth supprimé !");
        }
    }
}
