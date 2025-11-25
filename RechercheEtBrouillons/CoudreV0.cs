using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Select : MonoBehaviour
{
    public Camera mainCamera;
    public float spawnDistance = 10f;
    public float scrollSpeed = 5f;

    Mesh mesh;
    Vector3[] verticesAct;
    Vector3[] verticesPre;
    List<Vector3> mousePosition = new List<Vector3>();
    int[] triangles;
    int verticesCount = 4;
    int trianglesCount = 2;
    List<GameObject> rubans = new List<GameObject>();

    Coroutine MeshCreation;
    Coroutine MeshDeplacement;

    public float followSpeed = 50f;

    bool modeCloth = false;
    bool coutureActive = false;

    private GameObject rubanEnCours;
    private GameObject rubanSelectionne;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    void Update()
    {
        // Contrôle de profondeur
        spawnDistance += Input.mouseScrollDelta.y * scrollSpeed;
        spawnDistance = Mathf.Clamp(spawnDistance, 1f, 100f);

        // Création d’un ruban (clic gauche)
        if (Input.GetKeyDown(KeyCode.C) && MeshCreation == null && !modeCloth)
        {
            NewMesh();
            MeshCreation = StartCoroutine(NewVertexes());
        }

        // Stopper la création (clic droit)
        if (Input.GetMouseButtonDown(1) && MeshCreation != null)
        {
            StopCoroutine(MeshCreation);
            MeshCreation = null;

            // On ajoute le collider une fois que le ruban est fini
            if (rubanEnCours != null)
            {
                if (!modeCloth)
                {
                    AjouterCollider(rubanEnCours);
                    rubanEnCours = null;
                }
            }
        }

        // Toggle Cloth (clic molette)
        if (Input.GetMouseButtonUp(2))
        {
            foreach (var r in rubans)
                ToggleCloth(r);
            StopCoroutine(MeshCreation);
            MeshCreation = null;
        }

        // Sélection d’un ruban (clic gauche sur un ruban existant)
        if (Input.GetMouseButtonUp(0))
        {
            if (rubanSelectionne != null)
            {
                Renderer rend = rubanSelectionne.GetComponent<Renderer>();
                if (rend) rend.material.color = Color.white;
            }

            rubanSelectionne = null;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (rubans.Contains(hit.collider.gameObject))
                {
                    rubanSelectionne = hit.collider.gameObject;
                    Debug.Log("Ruban sélectionné : " + rubanSelectionne.name);

                    // Change la couleur du matériau pour montrer la sélection
                    Renderer rend = rubanSelectionne.GetComponent<Renderer>();
                    if (rend) rend.material.color = Color.yellow;
                }
            }
        }

        // Clic barre espace -> MoveMesh
        if (Input.GetKeyDown(KeyCode.Space) && modeCloth == false && rubanSelectionne != null)
        {
            if (MeshDeplacement == null)
            {
                MeshDeplacement = StartCoroutine(MoveMesh(rubanSelectionne));
            }

            else
            {
                StopCoroutine(MeshDeplacement);
                MeshDeplacement = null;
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            coutureActive = !coutureActive;
            Debug.Log("Couture " + (coutureActive ? "activée" : "désactivée"));
        }

        if (coutureActive)
        {
            // Appelle ta fonction CloseRuban() pour détecter et colorer les rubans proches
            CloseRuban();

            // Si tu appuies sur I, couds les rubans proches
            if (Input.GetKeyDown(KeyCode.I))
            {
                // On va chercher la première paire proche pour coudre
                for (int i = 0; i < rubans.Count; i++)
                {
                    GameObject rubanA = rubans[i];
                    MeshFilter filterA = rubanA.GetComponent<MeshFilter>();
                    if (filterA == null) continue;
                    Mesh meshA = filterA.mesh;
                    Vector3[] verticesA = meshA.vertices;
                    if (verticesA.Length < 2) continue;

                    for (int j = 0; j < rubans.Count; j++)
                    {
                        if (i == j) continue;

                        GameObject rubanB = rubans[j];
                        MeshFilter filterB = rubanB.GetComponent<MeshFilter>();
                        if (filterB == null) continue;
                        Mesh meshB = filterB.mesh;
                        Vector3[] verticesB = meshB.vertices;
                        if (verticesB.Length < 2) continue;

                        Vector3 pointA = rubanA.transform.TransformPoint(verticesA[verticesA.Length - 2]);
                        Vector3 pointB = rubanB.transform.TransformPoint(verticesB[0]);
                        float distance = Vector3.Distance(pointA, pointB);

                        if (distance < 1.0f)
                        {
                            Debug.Log("Couture des rubans !");
                            Coudre(rubanA, rubanB);
                            return; // Coud une paire et sort pour éviter bugs
                        }
                    }
                }
            }
        }

        else
        {
            // Couture désactivée, on remet toutes les couleurs normales
            foreach (var ruban in rubans)
            {
                Renderer rend = ruban.GetComponent<Renderer>();
                if (rend) rend.material.color = Color.white;
            }
        }
    }


    void NewMesh()
    {
        GameObject newRuban = new GameObject("Ruban");
        rubanEnCours = newRuban;
        newRuban.transform.SetParent(this.transform);
        rubans.Add(newRuban);

        MeshRenderer meshrenderer = newRuban.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = newRuban.AddComponent<MeshFilter>();
        //MeshCollider meshCollider = newRuban.AddComponent<MeshCollider>();

        Mesh newMesh = new Mesh();
        meshFilter.mesh = newMesh;
        //meshCollider.sharedMesh = newMesh;
        //meshCollider.convex = true;

        meshrenderer.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        meshrenderer.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 position = ray.origin + ray.direction * spawnDistance;

        verticesPre = new Vector3[4];
        verticesPre[0] = new Vector3(position.x - 0.1f, position.y, position.z);
        verticesPre[1] = new Vector3(position.x - 0.1f, position.y + 1, position.z);
        verticesPre[2] = position;
        verticesPre[3] = new Vector3(position.x, position.y + 1, position.z);

        verticesCount = 4;
        trianglesCount = 2;
        mousePosition.Clear();

        mesh = newMesh;
        CreateShape();
        UpdateMesh();
    }

    IEnumerator NewVertexes()
    {
        while (true)
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            mousePosition.Add(ray.origin + ray.direction * spawnDistance);

            verticesCount += 2;
            trianglesCount += 2;
            CreateShape();
            UpdateMesh();
            yield return new WaitForSeconds(0.1f);
        }
    }

    void CreateShape()
    {
        verticesAct = new Vector3[verticesCount];
        for (int j = 0; j < Mathf.Min(verticesPre.Length, verticesAct.Length); j++)
            verticesAct[j] = verticesPre[j];

        if (verticesCount > 4)
        {
            int offset = 4;
            for (int n = 0; n < mousePosition.Count; n++)
            {
                int i = offset + n * 2;
                verticesAct[i] = mousePosition[n]; // future position pouce
                verticesAct[i + 1] = new Vector3(mousePosition[n].x, mousePosition[n].y + 1, mousePosition[n].z);// future position index
            }
        }

        triangles = new int[trianglesCount * 3];
        triangles[0] = 0; triangles[1] = 1; triangles[2] = 2;
        triangles[3] = 3; triangles[4] = 2; triangles[5] = 1;

        for (int i = 2; i < verticesCount - 2; i += 2)
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

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = verticesAct;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    void ToggleCloth(GameObject ruban)
    {
        Cloth cloth = ruban.GetComponent<Cloth>();
        MeshRenderer renderer = ruban.GetComponent<MeshRenderer>();
        MeshFilter filter = ruban.GetComponent<MeshFilter>();

        if (cloth == null)
        {
            Mesh mesh = filter.mesh;
            Material mat = renderer.material;

            Destroy(renderer);
            Destroy(filter);

            var smr = ruban.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.material = mat;

            cloth = ruban.AddComponent<Cloth>();
            cloth.useGravity = true;
            cloth.damping = 0.2f;
            cloth.stretchingStiffness = 0.6f;
            cloth.bendingStiffness = 0.6f;
            modeCloth = true;
            Debug.Log("Cloth activé !");
        }
        else
        {
            SkinnedMeshRenderer smr = ruban.GetComponent<SkinnedMeshRenderer>();
            Mesh mesh = smr.sharedMesh;
            Material mat = smr.material;

            Destroy(cloth);
            Destroy(smr);

            var newFilter = ruban.AddComponent<MeshFilter>();
            newFilter.mesh = mesh;
            var newRenderer = ruban.AddComponent<MeshRenderer>();
            newRenderer.material = mat;
            modeCloth = false;
            Debug.Log("Cloth désactivé !");
        }
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

    IEnumerator MoveMesh(GameObject ruban)
    {
        while (true)
        {
            Vector3 targetPosition = GetMousePosition();
            Vector3 direction = (targetPosition - ruban.transform.position);

            // Pour savoir quand s'arrêter
            if (direction.magnitude > 0.01f)
            {
                ruban.transform.position = Vector3.MoveTowards(ruban.transform.position, targetPosition, followSpeed * Time.deltaTime);
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    Vector3 GetMousePosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        return ray.origin + ray.direction * spawnDistance;
    }

    void Coudre(GameObject firstRuban, GameObject secondRuban)
    {
        // Récupérer mesh et transform
        Mesh firstMesh = firstRuban.GetComponent<MeshFilter>().mesh;
        Mesh secondMesh = secondRuban.GetComponent<MeshFilter>().mesh;

        Vector3[] firstVertices = firstMesh.vertices;
        Vector3[] secondVertices = secondMesh.vertices;

        Transform firstTransform = firstRuban.transform;
        Transform secondTransform = secondRuban.transform;

        // Convertir en positions monde
        Vector3[] firstWorld = firstVertices.Select(v => firstTransform.TransformPoint(v)).ToArray();
        Vector3[] secondWorld = secondVertices.Select(v => secondTransform.TransformPoint(v)).ToArray();

        // Aligner les rubans : on déplace le second pour coller à la fin du premier
        Vector3 endOfFirst = firstWorld[firstWorld.Length - 2]; // avant-dernier point
        Vector3 startOfSecond = secondWorld[0];
        Vector3 offset = endOfFirst - startOfSecond;

        for (int i = 0; i < secondWorld.Length; i++)
            secondWorld[i] += offset;

        // Fusionner les vertices monde
        List<Vector3> mergedWorld = new List<Vector3>();
        mergedWorld.AddRange(firstWorld);
        mergedWorld.AddRange(secondWorld);

        // Choisir un point d’origine pour le nouveau ruban (ex: premier point)
        Vector3 origin = mergedWorld[0];
        Vector3[] localVertices = mergedWorld.Select(v => v - origin).ToArray();
        List<Vector3> localVerticesList = new List<Vector3>(localVertices);

        // Créer un nouveau ruban avec ta méthode actuelle
        CreateRubanFromVerticesAndPosition(localVerticesList, origin);

        // Supprimer les anciens rubans
        rubans.Remove(firstRuban);
        rubans.Remove(secondRuban);
        Destroy(firstRuban);
        Destroy(secondRuban);
    }

    void CreateRubanFromVerticesAndPosition(List<Vector3> vertexList, Vector3 worldPosition)
    {
        if (vertexList.Count < 4) return;

        GameObject newRuban = new GameObject("Ruban");
        newRuban.transform.SetParent(this.transform);
        newRuban.transform.position = worldPosition; // Position du ruban dans le monde
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

    void CloseRuban()
    {
        // Regarder si deux rubans sont proches
        // Si oui, renvoyer leurs Mesh
        foreach (var rubanA in rubans)
        {
            MeshFilter filterA = rubanA.GetComponent<MeshFilter>();
            if (filterA == null) continue;

            Mesh meshA = filterA.mesh;
            Vector3[] verticesA = meshA.vertices;
            Renderer rendA = rubanA.GetComponent<Renderer>();

            if (verticesA.Length < 2) continue;

            foreach (var rubanB in rubans)
            {
                if (rubanA != rubanB)
                {
                    if (rubanA == rubanB) continue;

                    MeshFilter filterB = rubanB.GetComponent<MeshFilter>();
                    if (filterB == null) continue;

                    Mesh meshB = filterB.mesh;
                    Vector3[] verticesB = meshB.vertices;
                    Renderer rendB = rubanB.GetComponent<Renderer>();

                    if (verticesB.Length < 2) continue;

                    Vector3 pointA = rubanA.transform.TransformPoint(verticesA[verticesA.Length - 2]);
                    Vector3 pointB = rubanB.transform.TransformPoint(verticesB[0]);
                    float distance = Vector3.Distance(pointA, pointB);

                    Debug.Log($"Distance entre {rubanA.name} et {rubanB.name} : {distance}");

                    if (distance < 1.0f) // Seuil de proximité
                    {
                        Debug.Log($"Rubans proches");
                        rendA.material.color = Color.red;
                        rendB.material.color = Color.red;

                        // Si on veut les coudre
                        if (Input.GetKeyDown(KeyCode.I))
                        {
                            Debug.Log("Couture des rubans !");
                            Coudre(rubanA, rubanB);
                            return; // Sortir de la fonction après la couture
                        }

                        // Si on ne veut pas les coudre
                        if (Input.GetKeyUp(KeyCode.O))
                        {
                            rendA.material.color = Color.white;
                            rendB.material.color = Color.white;
                        }
                    }

                    else if (distance >= 1.0f && rendA.material.color == rendB.material.color)
                    {
                        rendA.material.color = Color.white;
                        rendB.material.color = Color.white;
                    }
                }
            }
        }
    }
}

// voir si 2 bouts de rubans proches 
// les fusionner en un seul ruban
// - créer une nouvelle mesh avec les vertex des 2 rubans
// - supprimer les 2 anciens rubans

// constamment regarder si deux rubans sont proches
// si oui, les colorer en rouge pour indiquer qu'on peut les coudre
// si l'utilisateur appuie sur X, coudre les deux rubans proches
