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

public class ModifyVertexHandle : MonoBehaviour
{
    //Lien avec script principal
    public Main manager;

    private GameObject objet;

    private int vertexIndex;
    private Grabbable grabbable;


    public float radius = 0.02f;
    public float force = 1.0f;

    public Mesh originalMesh;

    public MeshFilter meshFilter;
    private Vector3 lastPosition;

    private bool isGrabbed = false;


    void Start()
    {

        if (manager == null)
            manager = FindFirstObjectByType<Main>();

    }

    public void Init(GameObject objet, int vertexIndex, Mesh original)
    {
        this.objet = objet;
        this.vertexIndex = vertexIndex;
        originalMesh = original;
        meshFilter = objet.GetComponent<MeshFilter>();
        meshFilter.mesh = Instantiate(originalMesh);

        grabbable = GetComponent<Grabbable>();

        if (grabbable != null)
        {
            grabbable.WhenPointerEventRaised += OnGrabEvent;
        }
        lastPosition = transform.position;

    }

    private void OnGrabEvent(PointerEvent evt)
    {
        if (evt.Type == PointerEventType.Select)
        {
            isGrabbed = true;
            lastPosition = transform.position;
            Debug.Log($"Vertex {vertexIndex} grabbed !");
        }
        else if (evt.Type == PointerEventType.Unselect)
        {
            isGrabbed = false;
            Debug.Log($"Vertex {vertexIndex} released !");
        }
    }


    void Update()
    {
        if (isGrabbed)
        {
            Vector3 newPos = transform.position;
            Vector3 delta = newPos - lastPosition;
            if (delta.sqrMagnitude > 0.000001f)
            {
                SmoothDeform(vertexIndex, delta);
                lastPosition = newPos;
            }
            
        }
    }


    void SmoothDeform(int selectedIndex, Vector3 delta)
    {
        originalMesh = meshFilter.mesh;
        Vector3[] vertices = originalMesh.vertices;
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
            vertices[i] += delta * falloff * force;
        }

        originalMesh.vertices = vertices;
        originalMesh.RecalculateNormals();
        meshFilter.mesh = originalMesh;

        manager.UpdateHandlesPositions(objet, vertices);
    }

    float GaussFalloff(float distance, float radius)
    {
        return Mathf.Exp(-(distance * distance) / (2 * radius * radius));
    }

    public void MakeVertexGrabbable(GameObject vertex)
    {
        // 1. Rigidbody
        Rigidbody rb = vertex.GetComponent<Rigidbody>();
        if (rb == null)
            rb = vertex.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.isKinematic = true; // on veut pouvoir le déplacer

        // 2. Collider
        Collider col = vertex.GetComponent<Collider>();
        if (col == null)
        {
            col = vertex.AddComponent<SphereCollider>();
            ((SphereCollider)col).radius = 0.005f;
        }

        // 3. Grabbable (Oculus SDK)
        var grabbable = vertex.GetComponent<Grabbable>();
        if (grabbable == null)
        {
            grabbable = vertex.AddComponent<Grabbable>();
            grabbable.InjectOptionalRigidbody(rb);
        }

        // 4. GrabInteractable (controller)
        var grabInteractable = vertex.GetComponent<GrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = vertex.AddComponent<GrabInteractable>();
            grabInteractable.InjectRigidbody(rb);
            grabInteractable.InjectOptionalPointableElement(grabbable);
        }

        // 5. HandGrabInteractable (optional, for hand tracking)
        var handGrabInteractable = vertex.GetComponent<HandGrabInteractable>();
        if (handGrabInteractable == null)
        {
            handGrabInteractable = vertex.AddComponent<HandGrabInteractable>();
            handGrabInteractable.InjectRigidbody(rb);
            handGrabInteractable.InjectOptionalPointableElement(grabbable);
        }
    }
}
