using NUnit.Framework.Internal;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class VertexHandle : MonoBehaviour
{
    private DemoV0 manager;
    private GameObject ruban;
    private int vertexIndex;
    private Camera cam;
    private bool isDragging = false;
    private float distanceToCamera;

    public float radius = 10f;
    public float force = 1.0f;

    Mesh originalMesh;
    Mesh clonedMesh;
    MeshFilter meshFilter;
    public void Init(DemoV0 manager, GameObject ruban, int vertexIndex, Mesh original)
    {
        this.manager = manager;
        this.ruban = ruban;
        this.vertexIndex = vertexIndex;
        cam = manager.mainCamera;

        meshFilter = ruban.GetComponent<MeshFilter>();
        originalMesh = original;

        clonedMesh = meshFilter.mesh;
    }

    void OnMouseDown()
    {
        isDragging = true;
        distanceToCamera = Vector3.Distance(transform.position, cam.transform.position);
    }

    void OnMouseUp()
    {
        isDragging = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (isDragging)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            Vector3 newPos = ray.origin + ray.direction * distanceToCamera;
            Vector3 delta = newPos - transform.position;

            transform.position = newPos;
            //manager.DeplacerVertex(ruban, vertexIndex, newPos);
            SmoothDeform(vertexIndex, delta);
        }

        if (Input.GetKeyDown(KeyCode.U))
            Reset();
    }

    public void Reset()
    {
        if (clonedMesh != null && originalMesh != null) 
        {
            clonedMesh.vertices = originalMesh.vertices; 
            clonedMesh.triangles = originalMesh.triangles;
            clonedMesh.normals = originalMesh.normals;
            meshFilter.mesh = clonedMesh; 

            manager.UpdateHandlesPositions(ruban, clonedMesh.vertices);
        }
    }
    

    void SmoothDeform(int selectedIndex, Vector3 delta)
    {
        clonedMesh = meshFilter.mesh;
        Vector3[] vertices = clonedMesh.vertices;

        Vector3 targetVertexPos = vertices[selectedIndex];
        float sqrRadius = radius * radius;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 currentVertexPos = vertices[i];
            float sqrMagnitude = (currentVertexPos - targetVertexPos).sqrMagnitude;

            if (sqrMagnitude > sqrRadius)
                continue;

            float distance = Mathf.Sqrt(sqrMagnitude);
            float falloff = GaussFalloff(distance, radius);

            // applique le déplacement atténué
            vertices[i] += delta * falloff * force;
        }

        clonedMesh.vertices = vertices;
        clonedMesh.RecalculateNormals();
        meshFilter.mesh = clonedMesh;
        manager.UpdateHandlesPositions(ruban, vertices);
    }

    float GaussFalloff(float distance, float radius)
    {
        return Mathf.Exp(-(distance * distance) / (2 * radius * radius));
    }
}

