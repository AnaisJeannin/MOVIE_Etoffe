using System;
using System.Collections.Generic;
using UnityEngine;

public class ClickMesh : MonoBehaviour
{
    //public GameObject pointPrefab;     // ton prefab de sphère -> à transfomer en sommet
    

    Mesh mesh;

    public event Action<Mesh> MeshCreated;
    public Camera mainCamera;          // la caméra utilisée
    public float spawnDistance = 10f;  // distance initiale devant la caméra
    public float scrollSpeed = 5f;     // vitesse de changement de profondeur

    Vector3[] verticesAct;
    Vector3[] verticesPre;
    List<Vector3> spawnPosition = new List<Vector3>();
    int[] triangles;
    int verticesCount = 4;
    int trianglesCount = 2;

    //Vector3 spawnPos;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Si la caméra n'est pas assignée, on prend la principale
        if (mainCamera == null)
            mainCamera = Camera.main;

        verticesPre = new Vector3[4];
        verticesPre[0] = new Vector3(0, 0, 0);
        verticesPre[1] = new Vector3(0, 1, 0);
        verticesPre[2] = new Vector3(1, 0, 0);
        verticesPre[3] = new Vector3(1, 1, 0);

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

       
        CreateShape();
        UpdateMesh();

    }

    void CreateShape()
    {
        verticesAct = new Vector3[verticesCount];

        for (int j=0; j < verticesPre.Length ;j++)
        {
            verticesAct[j] = verticesPre[j];
        }

        if (verticesCount > 4)
        {
            int offset = 4;
            for (int n = 0; n < spawnPosition.Count; n++)
            {
                int i = offset + n * 2;
                verticesAct[i] = spawnPosition[n]; // fut ure position pouce
                verticesAct[i + 1] = new Vector3(spawnPosition[n].x, spawnPosition[n].y + 1, spawnPosition[n].z); //future position index
            }
        }
    ;

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
        mesh.vertices = verticesAct;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
    }
    // Update is called once per frame
    void Update()
    {
        // Contrôle de la profondeur avec la molette
        spawnDistance += Input.mouseScrollDelta.y * scrollSpeed;
        spawnDistance = Mathf.Clamp(spawnDistance, 1f, 100f); // borne entre 1 et 100 unités

        // Clic gauche -> création du sommet
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            spawnPosition.Add(ray.origin + ray.direction * spawnDistance);

            //Instantiate(pointPrefab, spawnPos, Quaternion.identity);

            verticesCount += 2;
            trianglesCount += 2;
            CreateShape();
            UpdateMesh();
            MeshCreated?.Invoke(mesh);
        }
    }
}