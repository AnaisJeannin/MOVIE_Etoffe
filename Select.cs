using System.Collections;
using System.Collections.Generic;
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
        collider.convex = true;

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
}
