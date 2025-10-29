using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SpeedTree.Importer;
using UnityEngine.EventSystems;

public class DemoV5 : MonoBehaviour
{
    //Camera
    public Camera mainCamera;
    public float spawnDistance = 10f;
    public float scrollSpeed = 5f;

    //Tout ce qui concerne la création des meshs rubans
    Mesh mesh;
    Vector3[] verticesAct;
    Vector3[] verticesPre;
    List<Vector3> mousePosition = new List<Vector3>();
    int[] triangles;
    int verticesCount = 4;
    int trianglesCount = 2;
    List<GameObject> rubans = new List<GameObject>();

    //Coroutines pour la création et le déplacement des meshs
    Coroutine MeshCreation;
    Coroutine MeshDeplacement;

    public TextMeshProUGUI messageText;

    //Vitesse Déplacements du ruban
    public float followSpeed = 50f;

    bool modeCloth = false;
    bool coutureActive = false;
    bool modeModify = false;

    private GameObject rubanEnCours;
    private GameObject rubanSelectionne;

    private GameObject rubanSelectionne1;
    private GameObject rubanSelectionne2;

    //Modification des vertices
    public GameObject vertexHandlePrefab;
    private List<GameObject> vertexHandles = new List<GameObject>();

    //Création de bouttons
    public Button Cloth_button, Modify_button, Sew_button;
    public bool modify_clicked = false, sew_clicked = false, cloth_clicked = false;

    // Matériaux pour les rubans
    public Material[] materials;
    public Material White;
    public Material Soie;
    public Material Coton;
    public Material Denim;
    public Material Laine;
    public Material Velours;
    public Material Lin;
    public Material Satin;
    public Material Nylon;

    public TMP_Dropdown materialDropdown;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        Cloth_button.onClick.AddListener(ClothButton);
        Modify_button.onClick.AddListener(ModifyButton);
        Sew_button.onClick.AddListener(SewButton);

        materials = new Material[]{ White, Soie, Coton, Denim, Laine, Velours, Lin, Satin, Nylon};

        materialDropdown = GameObject.Find("materialDropdown").GetComponent<TMP_Dropdown>();
        materialDropdown.ClearOptions();
        materialDropdown.AddOptions(new List<string> {"Materiau par defaut","Soie", "Coton", "Denim", "Laine", "Velours", "Lin", "Satin", "Nylon"});
        materialDropdown.onValueChanged.AddListener(delegate { AppliquerMateriau(rubanSelectionne, materialDropdown.value); });
        materialDropdown.value = 0;
        materialDropdown.onValueChanged.Invoke(0);

    }

    // Update is called once per frame
    void Update()
    {
        // Contrôle de profondeur
        spawnDistance += Input.mouseScrollDelta.y * scrollSpeed;
        spawnDistance = Mathf.Clamp(spawnDistance, 1f, 100f);

        // Création d'un ruban (clic R)
        if (Input.GetKeyDown(KeyCode.R) && MeshCreation == null && !modeCloth && !coutureActive && !modeModify)
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

        // Sélection d'un ruban (clic gauche sur un ruban existant)
        if (Input.GetMouseButtonUp(0) && !coutureActive)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

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

                    // Change la couleur du mat�riau pour montrer la sélection
                    Renderer rend = rubanSelectionne.GetComponent<Renderer>();
                    if (rend) rend.material.color = Color.yellow;
                }
            }

            if (modeModify)
            {
                if (rubanSelectionne != null)
                {
                    StopCoroutine(ShowTemporaryMessage("Selectionnez un ruban à modifier !"));
                    Mesh rubanMesh = rubanSelectionne.GetComponent<MeshFilter>().mesh;
                    Vector3[] vertices = rubanMesh.vertices;
                    if (vertexHandles.Count == 0)
                        ShowVertices(rubanSelectionne);
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

        // Clic C -> Cut
        if (Input.GetKeyDown(KeyCode.C))
        {
            Cut();
            Debug.Log("Cut executed");
        }

        //Couture
        if (coutureActive)
        {
            modeCloth = false;
            if (rubanSelectionne != rubanSelectionne1 && rubanSelectionne != rubanSelectionne2)
            {
                Renderer rend = rubanSelectionne.GetComponent<Renderer>();
                if (rend) rend.material.color = Color.white;
                rubanSelectionne = null;
            }

            if (Input.GetMouseButtonUp(0))
            {
                if (rubanSelectionne1 == null)
                {
                    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        if (rubans.Contains(hit.collider.gameObject))
                        {
                            rubanSelectionne1 = hit.collider.gameObject;
                            rubanSelectionne = rubanSelectionne1; // Pour le déplacement

                            Renderer rend1 = rubanSelectionne1.GetComponent<Renderer>();
                            if (rend1) rend1.material.color = Color.yellow;

                            Debug.Log("Ruban 1 sélectionné : " + rubanSelectionne1.name);
                        }
                    }
                }

                else if (rubanSelectionne1 != null)
                {
                    Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        if (rubans.Contains(hit.collider.gameObject))
                        {
                            // Si le ruban est différent du premier on le sélectionne en ruban 2
                            if (hit.collider.gameObject != rubanSelectionne1)
                            {
                                if (rubanSelectionne2 != null)
                                {
                                    // Remet la couleur normale si un ruban 2 était déjà sélectionné
                                    Renderer rend2O = rubanSelectionne2.GetComponent<Renderer>();
                                    if (rend2O) rend2O.material.color = Color.white;
                                }

                                rubanSelectionne2 = hit.collider.gameObject;
                                rubanSelectionne = rubanSelectionne2; // Pour le déplacement

                                Renderer rend2 = rubanSelectionne2.GetComponent<Renderer>();
                                if (rend2) rend2.material.color = Color.yellow;

                                Debug.Log("Ruban 2 sélectionné : " + rubanSelectionne2.name);
                                Debug.Log("On peut coudre");
                            }

                            // Si le ruban est le même que le premier, on le déselctionne
                            else if (hit.collider.gameObject == rubanSelectionne1)
                            {
                                Renderer rend1 = rubanSelectionne1.GetComponent<Renderer>();
                                if (rend1) rend1.material.color = Color.white;

                                rubanSelectionne1 = null;

                                Debug.Log("Ruban 1 désélectionné");
                            }
                        }
                    }
                }
            }

            if (rubanSelectionne1 != null && rubanSelectionne2 != null)
            {
                // Si tu appuies sur I, couds les rubans proches
                if (Input.GetKeyDown(KeyCode.I))
                {
                    GameObject rubanA = rubanSelectionne1;
                    MeshFilter filterA = rubanA.GetComponent<MeshFilter>();

                    Mesh meshA = filterA.mesh;
                    Vector3[] verticesA = meshA.vertices;

                    GameObject rubanB = rubanSelectionne2;
                    MeshFilter filterB = rubanB.GetComponent<MeshFilter>();

                    Mesh meshB = filterB.mesh;
                    Vector3[] verticesB = meshB.vertices;

                    Vector3 pointA1 = rubanA.transform.TransformPoint(verticesA[verticesA.Length - 2]);
                    Vector3 pointB1 = rubanB.transform.TransformPoint(verticesB[0]);
                    Vector3 pointB2 = rubanB.transform.TransformPoint(verticesB[verticesB.Length - 2]);
                    Vector3 pointA2 = rubanA.transform.TransformPoint(verticesA[0]);

                    float distance1 = Vector3.Distance(pointA1, pointB1);
                    float distance2 = Vector3.Distance(pointB2, pointA2);

                    if (distance1 < distance2)
                    {
                        Debug.Log("Couture des rubans !");
                        Coudre(rubanA, rubanB);
                        return; // Coud une paire et sort pour éviter bugs
                    }

                    else if (distance2 <= distance1)
                    {
                        Debug.Log("Couture des rubans !");
                        Coudre(rubanB, rubanA);
                        return; // Coud une paire et sort pour éviter bugs
                    }
                }
            }
        }

        else
        {
            rubanSelectionne1 = null;
            rubanSelectionne2 = null;

            // Couture désactivée, on remet toutes les couleurs normales
            foreach (var ruban in rubans)
            {
                Renderer rend = ruban.GetComponent<Renderer>();
                if (rend) rend.material.color = Color.white;
            }

            if (rubanSelectionne != null)
            {
                Renderer rend = rubanSelectionne.GetComponent<Renderer>();
                if (rend) rend.material.color = Color.yellow;
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

        Mesh newMesh = new Mesh();
        meshFilter.mesh = newMesh;

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
            Vector3 pos = ray.origin + ray.direction * spawnDistance;

            if (Vector3.Distance(verticesPre[verticesPre.Length - 2], pos) > 0.01f)
            {
                mousePosition.Add(pos);
                verticesCount += 2;
                trianglesCount += 2;
                CreateShape();
                UpdateMesh();
            }
            yield return new WaitForSeconds(0.01f);
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

        Debug.Log($"MeshCollider ajouté : {ruban.name}");
    }
    IEnumerator MoveMesh(GameObject ruban)
    {
        while (true)
        {
            Vector3 targetPosition = GetMousePosition();
            Vector3 direction = (targetPosition - ruban.transform.position);

            // Pour savoir quand s'arr�ter
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
            modeCloth = true;

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
            modeCloth = false;
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
        rubans.Remove(hitRuban);
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

    //Couture des rubans
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

        // Créer le ruban directement avec les vertices monde
        CreateRubanFromWorldVertices(mergedWorld);

        // Supprimer les anciens rubans
        rubans.Remove(firstRuban);
        rubans.Remove(secondRuban);
        Destroy(firstRuban);
        Destroy(secondRuban);
    }

    void CreateRubanFromWorldVertices(List<Vector3> worldVertices)
    {
        if (worldVertices.Count < 4) return;

        GameObject newRuban = new GameObject("Ruban");
        newRuban.transform.SetParent(this.transform);
        rubans.Add(newRuban);

        // Créer mesh directement dans l’espace monde
        MeshRenderer meshrenderer = newRuban.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = newRuban.AddComponent<MeshFilter>();
        meshrenderer.material = GetComponent<MeshRenderer>().material;
        meshrenderer.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        Mesh newMesh = new Mesh();
        Vector3[] localVerts = new Vector3[worldVertices.Count];
        // Convertir les vertices monde en coordonnées locales du nouveau ruban
        for (int i = 0; i < worldVertices.Count; i++)
            localVerts[i] = newRuban.transform.InverseTransformPoint(worldVertices[i]);

        int[] tris = new int[(localVerts.Length - 2) * 3];
        for (int i = 0; i < (localVerts.Length / 2) - 1; i++)
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

        newMesh.vertices = localVerts;
        newMesh.triangles = tris;
        newMesh.RecalculateNormals();
        meshFilter.mesh = newMesh;

        AjouterCollider(newRuban);
    }

    void AppliquerMateriau(GameObject ruban, int index)
    {
        if (modeCloth) return; // Pas de changement de matériau en mode cloth

        Material mat = materials[index];

        if (ruban == null || mat == null) return;

        MeshRenderer rend = ruban.GetComponent<MeshRenderer>();
        SkinnedMeshRenderer skinned = ruban.GetComponent<SkinnedMeshRenderer>();

        if (rend != null)
            rend.material = mat;
        else if (skinned != null)
        {
            skinned.material = mat;

            // Forcer la mise à jour du Cloth
            var cloth = ruban.GetComponent<Cloth>();
            if (cloth != null)
            {
                skinned.enabled = false;
                skinned.enabled = true;
            }

        }

        else
        {
            Debug.LogWarning($"Aucun renderer trouvé sur {ruban.name}");
        }

        rend.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
    }

    //Modification des vertices d'un ruban
    public void ShowVertices(GameObject ruban)
    {
        MeshFilter filter = ruban.GetComponent<MeshFilter>();
        Mesh original = filter.sharedMesh;

        Mesh cloned = new Mesh();
        cloned.name = "clone";
        cloned.vertices = original.vertices;
        cloned.triangles = original.triangles;
        cloned.normals = original.normals;
        filter.mesh = cloned;

        vertexHandles.Clear();
        for (int i = 0; i < cloned.vertices.Length; i++)
        {
            Vector3 vertexPos = ruban.transform.TransformPoint(cloned.vertices[i]);
            GameObject handle = Instantiate(vertexHandlePrefab, vertexPos, Quaternion.identity);
            handle.transform.localScale = Vector3.one * 0.05f;
            handle.GetComponent<VertexHandle>().Init(this, ruban, i, original);
            vertexHandles.Add(handle);
        }

    }

    public void UpdateHandlesPositions(GameObject ruban, Vector3[] vertices)
    {
        if (vertexHandles.Count == 0) return;

        for (int i = 0; i < vertices.Length && i < vertexHandles.Count; i++)
        {
            Vector3 Pos = ruban.transform.TransformPoint(vertices[i]);
            vertexHandles[i].transform.position = Pos;
        }
    }
    public void DeleteVerticesShowed()
    {
        foreach (var handle in vertexHandles)
            Destroy(handle);
        vertexHandles.Clear();
    }

    //Fonctionnement des boutons
    public void ClothButton()
    {
        cloth_clicked = !cloth_clicked;
        modeCloth = cloth_clicked;
        Debug.Log("Mode Cloth " + (cloth_clicked ? "activé" : "désactivé"));

        if (modify_clicked)
        {
            modify_clicked = false;
            modeModify = false;
            DeleteVerticesShowed();
            Modify_button.GetComponent<Image>().color = Color.white;
        }

        if (sew_clicked)
        {
            sew_clicked = false;
            coutureActive = false;
            Sew_button.GetComponent<Image>().color = Color.white;
        }

        Cloth_button.GetComponent<Image>().color = cloth_clicked ? Color.green : Color.white;

        foreach (var r in rubans)
            ToggleCloth(r);

        if (MeshCreation != null)
        {
            StopCoroutine(MeshCreation);
            MeshCreation = null;
        }
    }

    public void ModifyButton()
    {
        modify_clicked = !modify_clicked;
        modeModify = modify_clicked;
        Debug.Log("Mode Modification " + (modify_clicked ? "activé" : "désactivé"));

        if (modeCloth)
        {
            cloth_clicked = false;
            modeCloth = false;

            foreach (var r in rubans)
                ToggleCloth(r);

            Cloth_button.GetComponent<Image>().color = Color.white;
        }

        if (sew_clicked)
        {
            sew_clicked = false;
            coutureActive = false;
            Sew_button.GetComponent<Image>().color = Color.white;
        }

        Modify_button.GetComponent<Image>().color = modify_clicked ? Color.green : Color.white;

        if (modeModify)
        {
            if (rubanSelectionne == null)
            {
                StartCoroutine(ShowTemporaryMessage("Selectionnez un ruban à modifier !"));
            }
        }

        if (!modeModify)
            DeleteVerticesShowed();
    }

    public void SewButton()
    {
        sew_clicked = !sew_clicked;
        coutureActive = sew_clicked;
        Debug.Log("Mode Couture " + (sew_clicked ? "activé" : "désactivé"));

        if (modeCloth)
        {
            cloth_clicked = false;
            modeCloth = false;

            foreach (var r in rubans)
                ToggleCloth(r);

            Cloth_button.GetComponent<Image>().color = Color.white;
        }

        if (modify_clicked)
        {
            modify_clicked = false;
            modeModify = false;
            DeleteVerticesShowed();
            Modify_button.GetComponent<Image>().color = Color.white;
        }

        Sew_button.GetComponent<Image>().color = sew_clicked ? Color.green : Color.white;
    }

    //Coroutine pour afficher un message temporaire
    IEnumerator ShowTemporaryMessage(string msg, float duration = 3f)
    {
        messageText.text = msg;
        yield return new WaitForSeconds(duration);
        messageText.text = "";
    }
}
