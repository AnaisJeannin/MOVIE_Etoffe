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


//Script permettant la création de patrons

public class Patron : MonoBehaviour
{
    //Lien avec le script principal
    private Main manager;

    //Camera
    public Camera mainCamera;

    //Pour créer le mesh du patron
    Mesh mesh;
    List<Vector3> Vertices = new List<Vector3>();
    Vector3[] VerticesTab;
    int[] Triangles;

    public List<GameObject> patrons = new List<GameObject>();

    Coroutine PatronCreation;

    //Permet d'afficher les sommets au moment de leur création
    public GameObject vertexPrefab;
    public List<GameObject> vertexObjects = new List<GameObject>();

    public GameObject newPatron;


    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (manager == null)
            manager = FindFirstObjectByType<Main>();
    }


    void Update()
    {

        // Création d'un patron 
        if (manager.IsPinching && !manager.isDrawing && PatronCreation == null && manager.patron_clicked) //On crée le patron en pinchant
        {
            manager.isDrawing = true;
            NewMesh();
            PatronCreation = StartCoroutine(NewVertexes());
        }

        // Stopper la création
        if (manager.isDrawing && !manager.IsPinching && PatronCreation != null && !manager.modeCloth && !manager.modeModify && !manager.modeCouture) //Arrêt de la création quand  on arrête de pincher
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
            if (newPatron != null)
            {
                manager.MakeGrabbable(newPatron);
            }

        }
    }


    void NewMesh()
    {
        newPatron = new GameObject("Patron");
        newPatron.transform.SetParent(this.transform);
        patrons.Add(newPatron);

        MeshRenderer meshrenderer = newPatron.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = newPatron.AddComponent<MeshFilter>();

        Mesh newMesh = new Mesh();
        meshFilter.mesh = newMesh;

        meshrenderer.sharedMaterial = GetComponent<MeshRenderer>().sharedMaterial;
        meshrenderer.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        Vector3 position = manager.GetIndexTipPosition();

        Vertices.Add(position);
        ShowVertices(position);

        mesh = newMesh;
    }


    IEnumerator NewVertexes()
    {
        while (true)
        {
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


    public void ShowVertices(Vector3 vertex) //Afficher les sommets au moment de leur création
    {
        //Vector3 vertexPos = vertex;
        GameObject handle = Instantiate(vertexPrefab, vertex, Quaternion.identity);
        handle.transform.localScale = Vector3.one * 0.005f;
        vertexObjects.Add(handle);
    }


    Vector3 Barycentre(List<Vector3> vertices) //Placer le barycentre des points formant le contour fermé
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