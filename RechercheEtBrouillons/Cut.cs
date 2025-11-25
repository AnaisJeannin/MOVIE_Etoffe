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
    public float followSpeed = 10f;

    int[] triangles;
    int verticesCount = 4;
    int trianglesCount = 2;
    int clicCount = 0;

    List<GameObject> rubans = new List<GameObject>();

    Coroutine MeshCreation;
    Coroutine MeshDeplacement;

    bool ModeCloth = false;

    private GameObject rubanEnCours;



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
        GameObject newRuban = new GameObject("Ruban");
        rubanEnCours = newRuban;
        newRuban.transform.SetParent(this.transform);
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
        MeshCollider oldCollider = ruban.GetComponent<MeshCollider>();

        if (cloth == null)
        {
            // Convertir en SkinnedMeshRenderer
            Mesh mesh = filter.mesh;
            Material mat = renderer.material;

            Destroy(renderer);
            Destroy(filter);
            Destroy(oldCollider);

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

            // Recréer MeshFilter, MeshRenderer et MeshCollider
            MeshFilter newFilter = ruban.AddComponent<MeshFilter>();
            newFilter.mesh = mesh;

            MeshRenderer newRenderer = ruban.AddComponent<MeshRenderer>();
            newRenderer.material = mat;

            MeshCollider newCollider = ruban.AddComponent<MeshCollider>();
            newCollider.sharedMesh = mesh;

            Debug.Log("Cloth désactivé !");
            ModeCloth = false;
        }
    }

    // Fonction pour couper un ruban en deux
    void Cut()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Mesh hitMesh;
        GameObject hitRuban;

        int closestIndex = ClosestIndexToPoint(ray, out hitMesh, out hitRuban);

        Debug.Log($"closestIndex={closestIndex}, hitMesh={(hitMesh != null)}, hitRuban={(hitRuban != null)}");

        if (closestIndex == -1 || hitMesh == null || hitRuban == null)
        {
            Debug.Log("Pas de vertex proche détecté !");
            return;
        }

        Vector3[] vertices = hitMesh.vertices;

        // Vérifie que le curseur est proche du vertex
        Vector3 worldVertex = hitRuban.transform.TransformPoint(vertices[closestIndex]);
        float distanceToCursor = Vector3.Distance(worldVertex, GetMousePosition());

        Debug.Log($"Distance au vertex: {distanceToCursor}");

        if (distanceToCursor > 4f)
        {
            Debug.Log("Vertex trop loin du curseur");
            return; // trop loin
        }
            

        // Pair = bas du ruban, Impair = haut du ruban
        int lowerIndex = (closestIndex % 2 == 0) ? closestIndex : closestIndex - 1;
        int upperIndex = lowerIndex + 1;

        // Découpe en deux rubans
        List<Vector3> firstHalf = new List<Vector3>();
        List<Vector3> secondHalf = new List<Vector3>();

        for (int i = 0; i < vertices.Length; i += 2)
        {
            if (i <= lowerIndex)
            {
                firstHalf.Add(vertices[i]);
                firstHalf.Add(vertices[i + 1]);
            }
            else
            {
                secondHalf.Add(vertices[i]);
                secondHalf.Add(vertices[i + 1]);
            }
        }

        // Crée les 2 nouveaux rubans
        CreateRubanFromVertices(firstHalf);
        CreateRubanFromVertices(secondHalf);

        // Supprime l'ancien ruban
        Destroy(hitRuban);
    }

    // Pour créer les rubans lors de la découpe
    void CreateRubanFromVertices(List<Vector3> vertexList)
    {
        if (vertexList.Count < 4) return;

        GameObject newRuban = new GameObject("Ruban");
        newRuban.transform.SetParent(this.transform);
        rubans.Add(newRuban);

        MeshRenderer meshrenderer = newRuban.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = newRuban.AddComponent<MeshFilter>();
        meshrenderer.material = GetComponent<MeshRenderer>().material;
        meshrenderer.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        Mesh newMesh = new Mesh();
        Vector3[] verts = vertexList.ToArray();
        int[] tris = new int[(verts.Length - 2) * 3];

        for (int i = 0; i < (verts.Length / 2) - 1; i++)
        {
            int vi = i * 2;
            int ti = i * 6;
            tris[ti + 0] = vi;
            tris[ti + 1] = vi + 1;
            tris[ti + 2] = vi + 2;
            tris[ti + 3] = vi + 3;
            tris[ti + 4] = vi + 2;
            tris[ti + 5] = vi + 1;
        }

        newMesh.vertices = verts;
        newMesh.triangles = tris;
        newMesh.RecalculateNormals();

        meshFilter.mesh = newMesh;

        AjouterCollider(newRuban);
    }

    void AjouterCollider(GameObject ruban)
    {
        MeshFilter mf = ruban.GetComponent<MeshFilter>();
        Mesh mesh = mf.mesh;

        MeshCollider collider = ruban.AddComponent<MeshCollider>();
        collider.sharedMesh = null;
        collider.sharedMesh = mesh;
        collider.convex = false;

        collider.enabled = false;
        collider.enabled = true;

        Debug.Log($"MeshCollider ajouté à {ruban.name}");
    }

    // Pour sélectionner le vertex le plus proche
    public int ClosestIndexToPoint(Ray ray, out Mesh hitMesh, out GameObject hitObject)
    {
        RaycastHit hit;
        hitMesh = null;
        hitObject = null;

        if (Physics.Raycast(ray, out hit))
        {
            MeshFilter mf = hit.transform.GetComponent<MeshFilter>();
            if (mf == null)
            {
                return -1; // Sécurité
            }

            hitMesh = mf.sharedMesh;
            hitObject = hit.transform.gameObject;
            Transform hitTransform = hit.transform;

            // Vérification de la validité du triangleIndex
            int triangleIndex = hit.triangleIndex;
            if (triangleIndex < 0)
            {
                Debug.LogWarning("triangleIndex négatif, probablement aucun triangle détecté.");
                hitMesh = null;
                hitObject = null;
                return -1;
            }

            int[] triangleArray = hitMesh.triangles;
            Vector3[] vertices = hitMesh.vertices;


            int triangleArrayLength = triangleArray.Length;
            int vertexArrayLength = vertices.Length;

            int triangleIndexBase = hit.triangleIndex * 3;

            if (triangleIndexBase + 2 >= triangleArrayLength)
            {
                Debug.LogWarning($"triangleIndex ({hit.triangleIndex}) hors limites du tableau triangles ({triangleArrayLength / 3} triangles).");
                hitMesh = null;
                hitObject = null;
                return -1;
            }

            // On récupère les indices de sommet pour ce triangle
            int i0 = triangleArray[triangleIndexBase + 0];
            int i1 = triangleArray[triangleIndexBase + 1];
            int i2 = triangleArray[triangleIndexBase + 2];

            // Vérifie que les indices sont valides par rapport au tableau de sommets
            if (i0 >= vertexArrayLength || i1 >= vertexArrayLength || i2 >= vertexArrayLength)
            {
                Debug.LogWarning("Un des indices de triangle dépasse la taille du tableau de vertices.");
                hitMesh = null;
                hitObject = null;
                return -1;
            }

            // OK, on peut accéder aux sommets
            Vector3 worldV0 = hitTransform.TransformPoint(vertices[i0]);
            Vector3 worldV1 = hitTransform.TransformPoint(vertices[i1]);
            Vector3 worldV2 = hitTransform.TransformPoint(vertices[i2]);

            // Trouver le plus proche
            int closestVertexIndex = i0;
            float closestDistance = Vector3.Distance(hit.point, worldV0);

            float dist1 = Vector3.Distance(hit.point, worldV1);
            if (dist1 < closestDistance)
            {
                closestVertexIndex = i1;
                closestDistance = dist1;
            }

            float dist2 = Vector3.Distance(hit.point, worldV2);
            if (dist2 < closestDistance)
            {
                closestVertexIndex = i2;
                closestDistance = dist2;
            }

            return closestVertexIndex;
        }

        return -1;
    }


    // Pour avoir accès à la position de la souris
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

        // Clic gauche -> lancement de coroutine NewVertexes
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

            // On ajoute le collider une fois que le ruban est fini
            if (rubanEnCours != null)
            {
                if (!ModeCloth)
                {
                    AjouterCollider(rubanEnCours);
                    rubanEnCours = null;
                }
            }
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

        // Clic C -> Cut
        if (Input.GetKeyDown(KeyCode.C))
        {
            Cut();
            Debug.Log("Cut executed");
        }
    }
}

// cliquer et prendre le vertex le plus proche 
// une fois que la distance entre le curseur de la souris et le vertex du bas est assez petite : couper
// pour couper :
// - créer un ruban égal à la fin de celui que l'on coupe 
// - créer un ruban égal au début du ruban que l'on coupe
// - supprimer le ruban de base


// impair : -1
// pair : +1
