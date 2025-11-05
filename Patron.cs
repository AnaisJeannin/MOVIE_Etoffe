using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using System.Linq;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;

public class Patron : MonoBehaviour
{
    private MeshTimer manager;

    //Camera
    public Camera mainCamera;

    Mesh mesh;
    List<Vector3> Vertices = new List<Vector3>();
    Vector3[] VerticesTab;
    int[] Triangles;
    // List<Vector3> mousePosition = new List<Vector3>();

    GameObject patronEnCours;
    public List<GameObject> patrons = new List<GameObject>();

    Coroutine PatronCreation;

    public GameObject vertexPrefab;
    public List<GameObject> vertexObjects = new List<GameObject>();

    public GameObject newPatron;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (manager == null)
            manager = FindFirstObjectByType<MeshTimer>();
    }

    // Update is called once per frame
    void Update()
    {

        // Création d'un patron 
        if (manager.IsPinching && !manager.isDrawing && PatronCreation == null && manager.patron_clicked)
        {
            manager.isDrawing = true;
            NewMesh();
            PatronCreation = StartCoroutine(NewVertexes());
        }

        // Stopper la création
        if (manager.isDrawing && !manager.IsPinching && PatronCreation != null && !manager.modeCloth && !manager.modeModify && !manager.coutureActive)
        {
            manager.isDrawing = false;
            StopCoroutine(PatronCreation);
            CreateShape();
            UpdateMesh();

            PatronCreation = null;
            Vertices.Clear();

            foreach (var vertex in vertexObjects)
                Destroy(vertex);
            vertexObjects.Clear();

            // On ajoute le collider une fois que le ruban est fini
            if (patronEnCours != null)
            {
                if (!manager.modeCloth)
                {
                    manager.AjouterCollider(patronEnCours);
                    patronEnCours = null;
                }

                Rigidbody rb = newPatron.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = newPatron.AddComponent<Rigidbody>();
                }
                rb.useGravity = false;
                rb.isKinematic = true;

                // Ensure we have a collider (CreatePrimitive gives one already)
                MeshCollider col = newPatron.GetComponent<MeshCollider>();
                if (col == null)
                {
                    col = newPatron.AddComponent<MeshCollider>();
                    col.convex = true;
                }

                var grabbable = newPatron.AddComponent<Grabbable>();
                grabbable.InjectOptionalRigidbody(rb);

                // 2. GrabInteractable (for controllers)
                var grabInteractable = newPatron.AddComponent<GrabInteractable>();
                // assign required fields (check in your SDK inspector to confirm)
                grabInteractable.InjectRigidbody(rb);
                grabInteractable.InjectOptionalPointableElement(grabbable);

                // 3. HandGrabInteractable (for hand tracking)
                var handGrabInteractable = newPatron.AddComponent<HandGrabInteractable>();
                handGrabInteractable.InjectRigidbody(rb);
                handGrabInteractable.InjectOptionalPointableElement(grabbable);

                Debug.Log("Runtime grabbable newPatron created and configured.");
            }

        }
    }

    void NewMesh()
    {
        newPatron = new GameObject("Patron");
        patronEnCours = newPatron;
        newPatron.transform.SetParent(this.transform);
        patrons.Add(newPatron);

        MeshRenderer meshrenderer = newPatron.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = newPatron.AddComponent<MeshFilter>();

        Mesh newMesh = new Mesh();
        meshFilter.mesh = newMesh;

        meshrenderer.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        meshrenderer.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        // Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 position = manager.GetIndexTipPosition();

        Vertices.Add(position);
        ShowVertices(position);

        mesh = newMesh;
        //CreateShape();
        //UpdateMesh();
    }

    IEnumerator NewVertexes()
    {
        while (true)
        {
            // Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3 pos = manager.GetIndexTipPosition();

            if (Vector3.Distance(Vertices[Vertices.Count - 1], pos) > 0.01f)
            {
                Vertices.Add(pos);
                ShowVertices(pos);
            }
            yield return new WaitForSeconds(0.01f);
        }
    }

    void CreateShape()
    {
        Vertices.Add(Barycentre(Vertices));
        VerticesTab = Vertices.ToArray();

        Triangles = new int[(VerticesTab.Length - 1) * 3];

        for (int i = 0; i < VerticesTab.Length - 2; i += 1)
        {
            int tri = i * 3;
            Triangles[tri + 0] = i + 1;
            Triangles[tri + 1] = i;
            Triangles[tri + 2] = VerticesTab.Length - 1;
        }

        Triangles[(VerticesTab.Length - 2) * 3 + 0] = VerticesTab.Length - 2;
        Triangles[(VerticesTab.Length - 2) * 3 + 1] = 0;
        Triangles[(VerticesTab.Length - 2) * 3 + 2] = VerticesTab.Length - 1;
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = VerticesTab;
        mesh.triangles = Triangles;
        mesh.RecalculateNormals();
    }

    public void ShowVertices(Vector3 vertex)
    {
        //Vector3 vertexPos = transform.TransformPoint(vertex);
        Vector3 vertexPos = vertex;
        GameObject handle = Instantiate(vertexPrefab, vertexPos, Quaternion.identity);
        handle.transform.localScale = Vector3.one * 0.05f;
        vertexObjects.Add(handle);
    }

    // Barycentre
    Vector3 Barycentre(List<Vector3> vertices)
    {
        // Calcul pour chaque coordonnées
        float sumX = 0f;
        float sumY = 0f;
        float sumZ = 0f;
        foreach (var vertex in vertices)
        {
            sumX += vertex.x;
            sumY += vertex.y;
            sumZ += vertex.z;
        }

        float centerX = sumX / vertices.Count;
        float centerY = sumY / vertices.Count;
        float centerZ = sumZ / vertices.Count;

        Vector3 barycentre = new Vector3(centerX, centerY, centerZ);

        return barycentre;
    }
}
