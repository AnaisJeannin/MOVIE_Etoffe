using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MeshTimer : MonoBehaviour
{
    Mesh mesh;

    public event Action<Mesh> MeshCreated;
    public Camera mainCamera;          // la caméra utilisée
    public float spawnDistance = 10f;  // distance initiale devant la caméra
    public float scrollSpeed = 5f;     // vitesse de changement de profondeur

    Vector3[] verticesAct;
    Vector3[] verticesPre;

    List<Vector3> mousePosition = new List<Vector3>();
    public float followSpeed = 5f;

    int[] triangles;
    int verticesCount = 4;
    int trianglesCount = 2;
    int clicCount = 0;

    List<GameObject> rubans = new List<GameObject>();

    Coroutine MeshCreation;
    Coroutine MeshDeplacement;

    bool ModeCloth = false;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Si la caméra n'est pas assignée, on prend la principale
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void CreateShape()
    {
        verticesAct = new Vector3[verticesCount];

        for (int j = 0; j < verticesPre.Length; j++)
        {
            verticesAct[j] = verticesPre[j];
        }

        if (verticesCount > 4)
        {
            int offset = 4;
            for (int n = 0; n < mousePosition.Count; n++)
            {
                int i = offset + n * 2;
                verticesAct[i] = mousePosition[n]; // future position pouce
                verticesAct[i + 1] = new Vector3(mousePosition[n].x, mousePosition[n].y + 1, mousePosition[n].z); //future position index
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

    void NewMesh()
    {
        //Création nouvel objet
        GameObject newRuban = new GameObject();
        newRuban.transform.SetParent(this.transform);
        newRuban.name = "Ruban";
        rubans.Add(newRuban);

        MeshRenderer meshrenderer = newRuban.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = newRuban.AddComponent<MeshFilter>();
        
        meshrenderer.material = GetComponent<MeshRenderer>().material;
        meshrenderer.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        Mesh newMesh = new Mesh();
        meshFilter.mesh = newMesh;

        //On initialise le rectangle de départ du ruban
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 position = ray.origin + ray.direction * spawnDistance;

        verticesPre = new Vector3[4];
        verticesPre[0] = new Vector3(position.x - 0.1f, position.y, position.z);
        verticesPre[1] = new Vector3(position.x - 0.1f, position.y + 1, position.z);
        verticesPre[2] = position;
        verticesPre[3] = new Vector3(position.x, position.y + 1, position.z);

        //Réinialisation des compteurs et de la liste mousePosition
        verticesCount = 4;
        trianglesCount = 2;
        mousePosition.Clear();

        //Création mesh
        mesh = newMesh;
        CreateShape();
        UpdateMesh();
        MeshCreated?.Invoke(mesh);
    }
    IEnumerator NewVertexes()
    {
        while (true)
        {
            mousePosition.Add(GetMousePosition());

            verticesCount += 2;
            trianglesCount += 2;
            CreateShape();
            UpdateMesh();
            MeshCreated?.Invoke(mesh);
            yield return new WaitForSeconds(0.01f);
        }
    }

    IEnumerator MoveMesh(GameObject ruban)
    {
        while (true)
        {
            Vector3 targetPosition = GetMousePosition();
            Vector3 direction = (targetPosition - ruban.transform.position);

            // Pour savoir quand s'arrêter
            if (direction.magnitude > 0.1f)
            {
                ruban.transform.position = Vector3.MoveTowards(ruban.transform.position, targetPosition, followSpeed*Time.deltaTime);
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    void ToggleCloth(GameObject ruban)
    {
        Cloth cloth = ruban.GetComponent<Cloth>();
        SkinnedMeshRenderer smr = ruban.GetComponent<SkinnedMeshRenderer>();
        MeshRenderer renderer = ruban.GetComponent<MeshRenderer>();
        MeshFilter filter = ruban.GetComponent<MeshFilter>();

        if (cloth == null)
        {
            // Convertir en SkinnedMeshRenderer
            Mesh mesh = filter.mesh;
            Material mat = renderer.material;

            Destroy(renderer);
            Destroy(filter);

            smr = ruban.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.material = mat;

            // Ajouter le cloth
            cloth = ruban.AddComponent<Cloth>();
            cloth.useGravity = true;
            cloth.worldVelocityScale = 1.0f;
            cloth.worldAccelerationScale = 1.0f;
            cloth.damping = 0.2f;
            cloth.stretchingStiffness = 0.6f;
            cloth.bendingStiffness = 0.6f;

            ClothSkinningCoefficient[] coefficients = cloth.coefficients;

            // On trosforme la position locale des vertex en position globale
            Vector3 worldVertexPos = transform.TransformPoint(smr.sharedMesh.vertices[0]);

            coefficients[0].maxDistance = 0f; // Fixer le premier point
            cloth.coefficients = coefficients; // Réassigner le tableau modifié

            Debug.Log("Cloth activé !");
            ModeCloth = true;

        }

        else
        {
            // Sauvegarder mesh et material avant de tout détruire
            Mesh mesh = smr.sharedMesh;
            Material mat = smr.material;

            // Supprimer Cloth et SkinnedMeshRenderer
            Destroy(cloth);
            Destroy(smr);

            // Recréer MeshFilter et MeshRenderer
            MeshFilter newFilter = ruban.AddComponent<MeshFilter>();
            newFilter.mesh = mesh;

            MeshRenderer newRenderer = ruban.AddComponent<MeshRenderer>();
            newRenderer.material = mat;

            Debug.Log("Cloth désactivé !");
            ModeCloth = false;
        }
    }


    Vector3 GetMousePosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        return ray.origin + ray.direction * spawnDistance;
    }


// Update is called once per frame
void Update()
    {
        // Contrôle de la profondeur avec la molette
        spawnDistance += Input.mouseScrollDelta.y * scrollSpeed;
        spawnDistance = Mathf.Clamp(spawnDistance, 1f, 100f); // borne entre 1 et 100 unités

        // Clic gauche -> lancement de coroutine
        if (Input.GetMouseButtonUp(0))
        {
            if (MeshCreation == null && ModeCloth == false)
            {
                NewMesh();
                MeshCreation = StartCoroutine(NewVertexes());
                clicCount++;
            }
        }

        // Clic droit -> arrêt du timer
        if (Input.GetMouseButtonDown(1) && MeshCreation != null)
        {
            StopCoroutine(MeshCreation);
            MeshCreation = null;
        }

        // Clic mollette -> toggle cloth
        if (Input.GetMouseButtonUp(2))
        {
            for (int i = 0; i < rubans.Count; i++)
                ToggleCloth(rubans[i]);
        }

        // Clic barre espace -> MoveMesh
        if (Input.GetKeyDown(KeyCode.Space) && ModeCloth == false) 
        {
            if (MeshDeplacement == null)
            {
                MeshDeplacement = StartCoroutine(MoveMesh(rubans[rubans.Count - 1]));
            }

            else
            {
                StopCoroutine(MeshDeplacement);
                MeshDeplacement = null;
            }
        }
    }
}

