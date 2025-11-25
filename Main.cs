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

//Script principal : création ruban, grab, gère les boutons, fonction couper, fonction coudre

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Main : MonoBehaviour
{
    //Lien avec les autres scripts
    public Patron patronManager;
    public ModifyVertexHandle vertexManager;

    Mesh mesh;

    public GameObject mannequin;

    [Header("Hand Tracking")]
    public Transform indexTransform;

    public OVRHand hand;    // permet de détecter le geste de la main
    public OVRSkeleton skeleton;    // permet d'avoir ccès aux positions des doigts
    public OVRHand.HandFinger fingerToTrack1 = OVRHand.HandFinger.Index;    // fingerToTrack = ici on suit l'index

    public GameObject grabPrefab;

    private Dictionary<OVRSkeleton.BoneId, OVRBone> bones = new();

    [Header("Camera")]
    public Camera mainCamera;

    //Concerne la création des rubans
    public event Action<Mesh> MeshCreated;

    Vector3[] verticesAct;
    Vector3[] verticesPre;
    public bool isDrawing = false; 
    List<Vector3> indexPosition = new List<Vector3>();  //permet de suivre les positions successives de l'index

    private List<(Vector3 pos, Quaternion rot)> fingerTrack = new();

    int[] triangles;
    int verticesCount = 4;
    int trianglesCount = 2;

    public List<GameObject> rubans = new List<GameObject>();
    Coroutine MeshCreation;
    public GameObject newRuban;


    public bool modeCloth = false;
    public bool modeCouture = false;
    public bool modeModify = false;

    public GameObject objetSelectionne;

    //Sélectionner rubans pour les coudre
    public GameObject rubanSelectionne1;
    public GameObject rubanSelectionne2;

    //Modification des vertices
    public Mesh original;
    public GameObject vertexHandlePrefab;
    public List<GameObject> vertexHandles = new List<GameObject>();
    public GameObject objetModify;

    //Création de bouttons
    public Button Cloth_button, Modify_button, Sew_button, Patron_button, Delete_button, Cut_button;
    public bool modify_clicked = false, sew_clicked = false, cloth_clicked = false, patron_clicked = false, cut_clicked = false;

    public TMP_Dropdown materialDropdown;

    [Header("Réglages curseur")]
    public Slider widthSlider;
    public float width = 0.02f;

    public TextMeshProUGUI messageText;

    //Eviter le double trigger
    private float lastToggleTime = 0f;
    private float ToggleCooldown = 0.3f;

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


    //Positions de l'index et du pouce

    public Vector3 GetIndexTipPosition()  // OVRSkeleton pour récupérer la position 3D précise du bout de l'index
    {
        if (skeleton == null || !skeleton.IsDataValid || !skeleton.IsDataHighConfidence)
            return Vector3.zero;    // positions du squelette valides et fiables ?

        foreach (var bone in skeleton.Bones)   //parcourt la liste des bones
        {
            if (bone.Id == OVRSkeleton.BoneId.Hand_Index3)
                return bone.Transform.position;                 // return bone.Transform; sans le position 
        }

        return Vector3.zero;    // donne vecteur 0 si pas trouvé
    }

    public Quaternion GetIndexTipRotation()
    {
        if (skeleton == null) return Quaternion.identity;

        var IndexBone = skeleton.Bones.FirstOrDefault(b => b.Id == OVRSkeleton.BoneId.Hand_Index3);
        return IndexBone != null ? IndexBone.Transform.rotation : Quaternion.identity;
    }

    Vector3 GetThumbTipPosition()  // OVRSkeleton pour récupérer la position 3D précise du bout du pouce
    {
        if (skeleton == null || !skeleton.IsDataValid || !skeleton.IsDataHighConfidence)
            return Vector3.zero;    // positions du squelette valides et fiables ?

        foreach (var bone in skeleton.Bones)   //parcourt la liste des bones
        {
            if (bone.Id == OVRSkeleton.BoneId.Hand_ThumbTip)
                return bone.Transform.position;                // idem que pour index
        }

        return Vector3.zero;    // donne vecteur 0 si pas trouvé
    }

    Vector3 pinchPosition()
    {
        Vector3 indexPosition = GetIndexTipPosition();
        Vector3 thumbPosition = GetThumbTipPosition();
        return (indexPosition + thumbPosition) / 2f;
    }

    public Vector3 pinchDirection()
    {
        return pinchPosition().normalized;

    }

    public bool IsPinching
    {
        get
        {
            if (hand == null) return false;
            return hand.GetFingerIsPinching(fingerToTrack1);
        }
    }


    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (patronManager == null)
            patronManager = FindFirstObjectByType<Patron>();

        if (vertexManager == null)
            vertexManager = FindFirstObjectByType<ModifyVertexHandle>();

        if (hand != null && skeleton == null)
        {
            skeleton = hand.GetComponent<OVRSkeleton>();    // si on ne trouve pas le squelette on essaie de le récupérer via composant de la main
        }

        //Initialisation des boutons
        Cloth_button.onClick.AddListener(ClothButton);
        Modify_button.onClick.AddListener(ModifyButton);
        Sew_button.onClick.AddListener(SewButton);
        Patron_button.onClick.AddListener(PatronButton);
        Delete_button.onClick.AddListener(DeleteButton);
        Cut_button.onClick.AddListener(CutButton);

        materials = new Material[] { White, Soie, Coton, Denim, Laine, Velours, Lin, Satin, Nylon };

        //Initialisation du dropdown
        materialDropdown = GameObject.Find("materialDropdown").GetComponent<TMP_Dropdown>();
        materialDropdown.ClearOptions();
        materialDropdown.AddOptions(new List<string> { "Materiau par defaut", "Soie", "Coton", "Denim", "Laine", "Velours", "Lin", "Satin", "Nylon" });
        materialDropdown.onValueChanged.AddListener(delegate { AppliquerMateriau(objetSelectionne, materialDropdown.value); });
        materialDropdown.value = 0;
        materialDropdown.onValueChanged.Invoke(0);

        //Initialisation du slider (largeur ruban)
        if (widthSlider != null)
        {
            widthSlider.onValueChanged.AddListener(UpdateWidthFromSlider);
            widthSlider.value = width;
        }

    }


    void UpdateWidthFromSlider(float newWidth)  //Gère la largeur du ruban
    {
        width = newWidth;
        Debug.Log($"Largeur du ruban : {width}");
    }


    void Update()
    {
        if (hand == null || skeleton == null)
            return;  // main et squelettes non dispos 

        ///////// Appel fonctions ////////

        if (!isDrawing && IsPinching && !modeCloth && !modeModify && !modeCouture && !patron_clicked && !cut_clicked) //Lance la création du ruban (on pinche)
        {
            isDrawing = true;
            NewMesh();
            MeshCreation = StartCoroutine(NewVertexes());
            Debug.Log("Début du dessin");
        }

        else if (isDrawing && !IsPinching && !modeCloth && !modeModify && !modeCouture && !patron_clicked && !cut_clicked) //Arrêt de la création (on arrête de pincher)
        {
            StopCoroutine(MeshCreation);
            MeshCreation = null;
            isDrawing = false;
            Debug.Log("Fin du dessin");

            MakeGrabbable(newRuban); 
        }

        if (rubanSelectionne1 != null && rubanSelectionne2 != null && modeCouture)  //Couture de deux rubans
        {
            // Si tu pinches, couds les rubans sélectionnés
            if (IsPinching)
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

        if (cut_clicked) //Permet de couper un ruban 
        {
            if (IsPinching)
            {
                Debug.Log($"pinchPosition = {pinchPosition()} , pinchDirection ={pinchDirection()}");
                Debug.DrawRay(pinchPosition(), pinchDirection(), Color.red, 0.1f);
                Cut();
            }
        }

    }


    void NewMesh() //Création d'un nouveau ruban (initialisation)
    {
        newRuban = new GameObject("Ruban");
        newRuban.transform.SetParent(this.transform);
        rubans.Add(newRuban);

        MeshRenderer meshRenderer = newRuban.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = newRuban.AddComponent<MeshFilter>();

        meshRenderer.material = GetComponent<MeshRenderer>().material;
        meshRenderer.material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

        Mesh newMesh = new Mesh();
        meshFilter.mesh = newMesh;
        mesh = newMesh;

        // Première position de départ
        Vector3 fingerPos = GetIndexTipPosition();
        Quaternion handRot = GetIndexTipRotation();
        Vector3 right = handRot * Vector3.right;
        Vector3 up = handRot * Vector3.up;

        verticesPre = new Vector3[4];
        verticesPre[0] = fingerPos - right * width;
        verticesPre[1] = fingerPos - right * width + up * width;
        verticesPre[2] = fingerPos + right * width;
        verticesPre[3] = fingerPos + right * width + up * width;

        verticesCount = 4;
        trianglesCount = 2;
        indexPosition.Clear();

        CreateShape();
        UpdateMesh();
        MeshCreated?.Invoke(mesh);

        fingerTrack.Clear();
    }


    IEnumerator NewVertexes()
    {
        fingerTrack.Clear(); // remet à zéro la trace au début

        while (true)
        {
            // On récupère la position et rotation du bout de l’index
            Vector3 currentPos = GetIndexTipPosition();
            Quaternion currentRot = GetIndexTipRotation();

            if (Vector3.Distance(verticesPre[verticesPre.Length - 2], pinchPosition()) > 0.01f)
            {
                // On lisse la trajectoire pour éviter les tremblements du suivi
                Vector3 smoothedPos = Vector3.Lerp(
                    fingerTrack.Count > 0 ? fingerTrack[^1].pos : currentPos,
                    currentPos,
                    0.4f
                );

                Quaternion smoothedRot = Quaternion.Slerp(
                    fingerTrack.Count > 0 ? fingerTrack[^1].rot : currentRot,
                    currentRot,
                    0.4f
                );

                indexPosition.Add(pinchPosition());
                verticesCount += 2;
                trianglesCount += 2;

                // On enregistre cette position/rotation dans la trace
                fingerTrack.Add((smoothedPos, smoothedRot));

                // On reconstruit le ruban complet à partir de la trajectoire enregistrée
                CreateShapeWithOrientation(fingerTrack, width);
                UpdateMesh();
            }

            yield return new WaitForSeconds(0.01f); // durée entre chaque itération
        }

    }


    void CreateShape()
    {
        verticesAct = new Vector3[verticesCount];

        for (int j = 0; j < verticesPre.Length; j++)
        {
            verticesAct[j] = verticesPre[j];
        }

        triangles = new int[trianglesCount * 3];
        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 2;
        triangles[3] = 3;
        triangles[4] = 2;
        triangles[5] = 1;
    }


    void CreateShapeWithOrientation(List<(Vector3 pos, Quaternion rot)> track, float width)
    {
        if (track.Count < 2)
            return;

        verticesAct = new Vector3[track.Count * 2];
        Vector3[] normals = new Vector3[track.Count * 2];

        float currentWidth = width;

        for (int i = 0; i < track.Count; i++)
        {
            Vector3 center = track[i].pos;
            Quaternion rotation = track[i].rot;

            // Tangente = direction du mouvement du doigt
            Vector3 tangent;
            if (i == 0)
                tangent = (track[i + 1].pos - track[i].pos).normalized;
            else
                tangent = (track[i].pos - track[i - 1].pos).normalized;

            // Normale = orientation du doigt 
            Vector3 forward = rotation * Vector3.forward;

            // Correction pour éviter les inversions : recalcul de la “right”
            Vector3 right = Vector3.Cross(forward, tangent).normalized;
            Vector3 adjustedNormal = Vector3.Cross(tangent, right).normalized;

            // Appliquer le lissage entre l’ancienne normale et la nouvelle
            if (i > 0)
            {
                Vector3 prevNormal = normals[(i - 1) * 2];
                adjustedNormal = Vector3.Slerp(prevNormal, adjustedNormal, 0.5f).normalized;
            }

            // Placer les deux sommets du ruban
            verticesAct[i * 2] = center - right * width;
            verticesAct[i * 2 + 1] = center + right * width;

            // Affecter la normale à chaque vertex
            normals[i * 2] = adjustedNormal;
            normals[i * 2 + 1] = adjustedNormal;
        }

        // Création des triangles
        triangles = new int[(track.Count - 1) * 6];
        for (int i = 0; i < track.Count - 1; i++)
        {
            int vi = i * 2;
            int ti = i * 6;
            triangles[ti + 0] = vi;
            triangles[ti + 1] = vi + 1;
            triangles[ti + 2] = vi + 2;
            triangles[ti + 3] = vi + 3;
            triangles[ti + 4] = vi + 2;
            triangles[ti + 5] = vi + 1;
        }

        // Mise à jour du mesh
        UpdateMesh();
    }


    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = verticesAct;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }


    public void MakeGrabbable(GameObject gameObject)
    {
        // grab le gameObject au runtime 
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.isKinematic = true;

        // Ensure we have a collider
        MeshCollider col = gameObject.GetComponent<MeshCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<MeshCollider>();
            col.convex = true;
        }

        //1.Grabbable
        var grabbable = gameObject.AddComponent<Grabbable>();
        grabbable.InjectOptionalRigidbody(rb);

        // 2. GrabInteractable (for controllers)
        var grabInteractable = gameObject.AddComponent<GrabInteractable>();
        grabInteractable.InjectRigidbody(rb);
        grabInteractable.InjectOptionalPointableElement(grabbable);

        // 3. HandGrabInteractable (for hand tracking)
        var handGrabInteractable = gameObject.AddComponent<HandGrabInteractable>();
        handGrabInteractable.InjectRigidbody(rb);
        handGrabInteractable.InjectOptionalPointableElement(grabbable);

        Debug.Log($"Runtime grabbable {gameObject} created and configured.");
    }


    void ToggleCloth(GameObject ruban) //Donner les caractéristiques d'un cloth au mesh
    {
        if (ruban == null)
        {
            Debug.LogWarning("Ruban null !");
            return;
        }

        Debug.Log("ToggleCloth commencé pour " + ruban.name);

        // Vérifie les composants existants
        Cloth cloth = ruban.GetComponent<Cloth>();
        SkinnedMeshRenderer smr = ruban.GetComponent<SkinnedMeshRenderer>();
        MeshRenderer meshRenderer = ruban.GetComponent<MeshRenderer>();
        MeshFilter meshFilter = ruban.GetComponent<MeshFilter>();

        Debug.Log($"Components - Cloth: {cloth}, SkinnedMeshRenderer: {smr}, MeshRenderer: {meshRenderer}, MeshFilter: {meshFilter}");

        // Si Cloth n'existe pas -> on l'active
        if (cloth == null)
        {
            if (meshFilter == null || meshRenderer == null)
            {
                Debug.LogError("Impossible de convertir en Cloth : MeshFilter ou MeshRenderer manquant !");
                return;
            }

            Mesh mesh = meshFilter.sharedMesh;
            Material mat = meshRenderer.sharedMaterial;

            // Supprimer les composants non nécessaires
            DestroyImmediate(meshRenderer);
            DestroyImmediate(meshFilter);

            // Ajouter SkinnedMeshRenderer
            smr = ruban.GetComponent<SkinnedMeshRenderer>();
            if (smr == null)
                smr = ruban.AddComponent<SkinnedMeshRenderer>();
            smr.sharedMesh = mesh;
            smr.material = mat;

            // Ajouter le Cloth
            cloth = ruban.AddComponent<Cloth>();
            cloth.useGravity = true;
            cloth.worldVelocityScale = 1f;
            cloth.worldAccelerationScale = 1f;
            cloth.damping = 0.2f;
            cloth.stretchingStiffness = 0.6f;
            cloth.bendingStiffness = 0.6f;

            // Empêcher le tissu de traverser le mannequin
            List<ClothSphereColliderPair> colliderList = new List<ClothSphereColliderPair>();

            // Récupère tous les colliders du mannequin
            foreach (var c in mannequin.GetComponentsInChildren<Collider>())
            {
                // Si c’est un SphereCollider, on peut directement l’utiliser
                if (c is SphereCollider sphere)
                    colliderList.Add(new ClothSphereColliderPair(sphere));
            }

            // Paramètres de collision
            cloth.useContinuousCollision = 1.0f;        // Empêche le tissu de traverser les objets à grande vitesse
            cloth.friction = 0.4f;                      // Plus haut = glisse moins sur le mannequin
            cloth.collisionMassScale = 0.5f;            // Masse apparente du tissu lors des collisions
            cloth.randomAcceleration = Vector3.zero;

            // Applique au cloth
            cloth.sphereColliders = colliderList.ToArray();

            Debug.Log($"Colliders du mannequin assignés : {colliderList.Count}");

            // Fixer les 10% des vertex les plus hauts
            Vector3[] vertices = mesh.vertices;
            ClothSkinningCoefficient[] coefficients = new ClothSkinningCoefficient[vertices.Length];

            // Initialisation par défaut
            for (int i = 0; i < coefficients.Length; i++)
            {
                coefficients[i].maxDistance = float.MaxValue;
                coefficients[i].collisionSphereDistance = 0f;
            }

            // Calculer le nombre de vertex à fixer (10% du total, au moins 1)
            int nbFixes = Math.Max(1, (int)Math.Floor(vertices.Length * 0.10f));

            // Trouver les indices des vertex les plus hauts (10%)
            int[] topIndices = vertices
                .Select((v, i) => new { Index = i, Y = v.y })
                .OrderByDescending(v => v.Y)
                .Take(nbFixes)
                .Select(v => v.Index)
                .ToArray();

            // Fixer ces vertex
            foreach (int idx in topIndices)
            {
                coefficients[idx].maxDistance = 0f; // Ces vertex sont fixés
            }

            cloth.coefficients = coefficients;

            Debug.Log($"Cloth activé avec les {nbFixes} vertex les plus hauts fixés (≈5%) !");
            modeCloth = true;

        }
        else
        {
            // Sauvegarder mesh et material
            if (smr == null)
            {
                Debug.LogError("SkinnedMeshRenderer manquant, impossible de désactiver Cloth !");
                return;
            }

            Mesh mesh = smr.sharedMesh;
            Material mat = smr.material;

            DestroyImmediate(cloth);
            DestroyImmediate(smr);

            // Recréer MeshFilter et MeshRenderer
            meshFilter = ruban.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            meshRenderer = ruban.AddComponent<MeshRenderer>();
            meshRenderer.material = mat;

            // Ajouter MeshCollider si nécessaire
            MeshCollider collider = ruban.GetComponent<MeshCollider>();
            if (collider == null)
                collider = ruban.AddComponent<MeshCollider>();
            collider.sharedMesh = mesh;

            Debug.Log("Cloth désactivé !");
            modeCloth = false;
        }
    }


    //Modification des vertices d'un ruban
    public void ShowVertices(GameObject ruban) //Créer des poignées sur les verteces pour les rendre déplaçables
    {
        MeshFilter filter = ruban.GetComponent<MeshFilter>();
        original = filter.sharedMesh;

        vertexHandles.Clear();
        for (int i = 0; i < original.vertices.Length; i++)
        {
            Vector3 vertexPos = ruban.transform.TransformPoint(original.vertices[i]);
            GameObject handle = Instantiate(vertexHandlePrefab, vertexPos, Quaternion.identity);
            handle.transform.localScale = Vector3.one * 0.005f;
            vertexManager.MakeVertexGrabbable(handle);
            handle.GetComponent<ModifyVertexHandle>().Init(ruban, i, original);
            vertexHandles.Add(handle);
        }

    }

    public void UpdateHandlesPositions(GameObject objet, Vector3[] vertices) //Pour que les sommets autour du sommet déplacé suivent le mouvement (smooth deformation)
    {
        if (vertexHandles.Count == 0) return;

        for (int i = 0; i < vertices.Length && i < vertexHandles.Count; i++)
        {
            Vector3 Pos = objet.transform.TransformPoint(vertices[i]);
            vertexHandles[i].transform.position = Pos;
        }
    }

    public void DeleteVerticesShowed() 
    {
        foreach (var handle in vertexHandles)
            Destroy(handle);
        vertexHandles.Clear();


    }

    public void ToggleRubanGrabbable(GameObject objet, bool active)
    {
        var grabbable = objet.GetComponent<Grabbable>();
        var grabInteractable = objet.GetComponent<GrabInteractable>();
        var handGrabInteractable = objet.GetComponent<HandGrabInteractable>();

        if (!active)
        {
            if (grabbable) Destroy(grabbable);
            if(grabInteractable) Destroy(grabInteractable);
            if (handGrabInteractable) Destroy(handGrabInteractable);
        }
        else
        {
            MeshFilter mf_objet = objet.GetComponent<MeshFilter>();

            Mesh modifiedMesh = mf_objet.mesh;
            mf_objet.sharedMesh = modifiedMesh;
            MeshCollider mc = objet.GetComponent<MeshCollider>();
            if (mc != null)
            {
                mc.sharedMesh = null;
                mc.sharedMesh = modifiedMesh;
            }
            MakeGrabbable(objet);

        }
    }


    //Couture des rubans
    public void Coudre(GameObject firstRuban, GameObject secondRuban)
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


    // Fonction pour couper un ruban en deux
    void Cut()
    {
        if (objetSelectionne == null || patronManager.patrons.Contains(objetSelectionne))
        {
            Debug.LogWarning("Aucun ruban sélectionné !");
            return;
        }

        // Vérifie que le ruban a un MeshFilter valide
        MeshFilter mf = objetSelectionne.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogWarning("Ruban sélectionné invalide !");
            return;
        }

        Mesh mesh = mf.sharedMesh;
        Vector3[] vertices = mesh.vertices;

        // Convertir tous les vertices en coordonnées mondiales
        List<Vector3> worldVertices = new List<Vector3>();
        foreach (var v in vertices)
            worldVertices.Add(objetSelectionne.transform.TransformPoint(v));

        // Trouver le vertex le plus proche du pinch
        int closestIndex = -1;
        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < worldVertices.Count; i++)
        {
            float dist = Vector3.Distance(worldVertices[i], pinchPosition());
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestIndex = i;
            }
        }

        if (closestIndex == -1)
        {
            Debug.Log("Aucun vertex trouvé pour couper");
            return;
        }

        Debug.Log($"Découpe au vertex {closestIndex} (distance {closestDistance:F3})");

        // Déterminer l'index pair/impair du bas ou haut du ruban
        int lowerIndex = (closestIndex % 2 == 0) ? closestIndex : closestIndex - 1;

        // Créer les deux listes de vertices monde
        List<Vector3> firstHalfWorldVerts = new List<Vector3>();
        List<Vector3> secondHalfWorldVerts = new List<Vector3>();

        for (int i = 0; i < worldVertices.Count; i += 2)
        {
            if (i <= lowerIndex)
            {
                firstHalfWorldVerts.Add(worldVertices[i]);
                firstHalfWorldVerts.Add(worldVertices[i + 1]);
            }
            else
            {
                secondHalfWorldVerts.Add(worldVertices[i]);
                secondHalfWorldVerts.Add(worldVertices[i + 1]);
            }
        }

        // Créer les deux nouveaux rubans dans le monde
        CreateRubanFromWorldVertices(firstHalfWorldVerts);
        CreateRubanFromWorldVertices(secondHalfWorldVerts);

        // Supprime l'ancien ruban
        rubans.Remove(objetSelectionne);
        Destroy(objetSelectionne);

        Debug.Log("Ruban coupé avec succès !");
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

        MakeGrabbable(newRuban);
    }

    public void AjouterCollider(GameObject ruban)
    {
        MeshFilter mf = ruban.GetComponent<MeshFilter>();
        Mesh mesh = mf.mesh;

        MeshCollider collider = ruban.AddComponent<MeshCollider>();
        collider.sharedMesh = null;
        collider.sharedMesh = mesh;
        collider.convex = true;

        collider.enabled = false;
        collider.enabled = true;

        Debug.Log($"MeshCollider ajouté : {ruban.name}");
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


    //Coroutine pour afficher un message temporaire
    IEnumerator ShowTemporaryMessage(string msg, float duration = 3f)
    {
        messageText.text = msg;
        yield return new WaitForSeconds(duration);
        messageText.text = "";
    }

    //Fonctionnement des boutons
    public void ClothButton()
    {
        // Vérifier cooldown pour éviter double trigger
        if (Time.time - lastToggleTime < ToggleCooldown)
            return;

        lastToggleTime = Time.time;

        cloth_clicked = !cloth_clicked;
        modeCloth = cloth_clicked;
        Debug.Log("Mode Cloth " + (cloth_clicked ? "activé" : "désactivé"));

        foreach (var r in rubans)
            ToggleCloth(r);

        foreach (var p in patronManager.patrons)
            ToggleCloth(p);

        Cloth_button.GetComponent<Image>().color = cloth_clicked ? Color.green : Color.white;

        if (modify_clicked)
        {
            modify_clicked = false;
            modeModify = false;
            if (objetModify != null)
            {
                ToggleRubanGrabbable(objetModify, true);
                objetModify = null;
            }
            DeleteVerticesShowed();
            Modify_button.GetComponent<Image>().color = Color.white;
        }

        if (sew_clicked)
        {
            sew_clicked = false;
            modeCouture = false;
            Sew_button.GetComponent<Image>().color = Color.white;
            Debug.Log("change couyleur 1");

            if (rubanSelectionne1 != null)
            {
                Renderer rend1 = rubanSelectionne1.GetComponent<Renderer>();
                if (rend1) rend1.material.color = Color.white;

                rubanSelectionne1 = null;
            }
            Debug.Log("change couyleur 2");

            if (rubanSelectionne2 != null)
            {
                Renderer rend2 = rubanSelectionne2.GetComponent<Renderer>();
                if (rend2) rend2.material.color = Color.white;

                rubanSelectionne2 = null;
            }
            objetSelectionne = null;
        }

        if (MeshCreation != null)
        {
            StopCoroutine(MeshCreation);
            MeshCreation = null;
        }

        if (patron_clicked)
        {
            patron_clicked = false;
            Patron_button.GetComponent<Image>().color = Color.white;
        }

        if (cut_clicked)
        {
            cut_clicked = false;
            Cut_button.GetComponent<Image>().color = Color.white;
        }
    }

    public void ModifyButton()
    {
        // Vérifier cooldown pour éviter double trigger
        if (Time.time - lastToggleTime < ToggleCooldown)
            return;

        lastToggleTime = Time.time;

        modify_clicked = !modify_clicked;
        modeModify = modify_clicked;
        Debug.Log("Mode Modification " + (modify_clicked ? "activé" : "désactivé"));

        if (modeCloth)
        {
            cloth_clicked = false;
            modeCloth = false;

            foreach (var r in rubans)
                ToggleCloth(r);

            foreach (var p in patronManager.patrons)
                ToggleCloth(p);

            Cloth_button.GetComponent<Image>().color = Color.white;
        }

        if (sew_clicked)
        {
            sew_clicked = false;
            modeCouture = false;
            Sew_button.GetComponent<Image>().color = Color.white;

            if (rubanSelectionne1 != null)
            {
                Renderer rend1 = rubanSelectionne1.GetComponent<Renderer>();
                if (rend1) rend1.material.color = Color.white;

                rubanSelectionne1 = null;
            }
            Debug.Log("change couyleur 2");

            if (rubanSelectionne2 != null)
            {
                Renderer rend2 = rubanSelectionne2.GetComponent<Renderer>();
                if (rend2) rend2.material.color = Color.white;

                rubanSelectionne2 = null;
            }
            objetSelectionne = null;
        }

        Modify_button.GetComponent<Image>().color = modify_clicked ? Color.green : Color.white;

        if (modeModify)
        {
            objetModify = objetSelectionne;

            if (objetSelectionne == null)
            {
                StartCoroutine(ShowTemporaryMessage(""));
            }
            else
            {
                ToggleRubanGrabbable(objetModify, false);
                ShowVertices(objetModify);
            }
        }

        if (patron_clicked)
        {
            patron_clicked = false;
            Patron_button.GetComponent<Image>().color = Color.white;
        }

        if (!modeModify)
        {
            if (objetModify != null)
                ToggleRubanGrabbable(objetModify, true);
            DeleteVerticesShowed();
            objetModify = null;
        }

        if (cut_clicked)
        {
            cut_clicked = false;
            Cut_button.GetComponent<Image>().color = Color.white;
        }

    }

    public void SewButton()
    {
        // Vérifier cooldown pour éviter double trigger
        if (Time.time - lastToggleTime < ToggleCooldown)
            return;

        lastToggleTime = Time.time;

        sew_clicked = !sew_clicked;
        modeCouture = sew_clicked;
        Debug.Log("Mode Couture " + (sew_clicked ? "activé" : "désactivé"));

        if (modeCloth)
        {
            cloth_clicked = false;
            modeCloth = false;

            foreach (var r in rubans)
                ToggleCloth(r);

            foreach (var p in patronManager.patrons)
                ToggleCloth(p);

            Cloth_button.GetComponent<Image>().color = Color.white;
        }

        if (modify_clicked)
        {
            modify_clicked = false;
            modeModify = false;
            if (objetModify != null)
            {
                ToggleRubanGrabbable(objetModify, true);
                objetModify = null;
            }
            DeleteVerticesShowed();
            Modify_button.GetComponent<Image>().color = Color.white;
        }

        if (patron_clicked)
        {
            patron_clicked = false;
            Patron_button.GetComponent<Image>().color = Color.white;
        }

        if (cut_clicked)
        {
            cut_clicked = false;
            Cut_button.GetComponent<Image>().color = Color.white;
        }

        Sew_button.GetComponent<Image>().color = sew_clicked ? Color.green : Color.white;

        if (!sew_clicked)
        {
            if (rubanSelectionne1 != null)
            {
                Renderer rend1 = rubanSelectionne1.GetComponent<Renderer>();
                if (rend1) rend1.material.color = Color.white;

                rubanSelectionne1 = null;
            }
            Debug.Log("change couyleur 2");

            if (rubanSelectionne2 != null)
            {
                Renderer rend2 = rubanSelectionne2.GetComponent<Renderer>();
                if (rend2) rend2.material.color = Color.white;

                rubanSelectionne2 = null;
            }
            objetSelectionne = null;
        }
    }

    public void PatronButton()
    {
        // Vérifier cooldown pour éviter double trigger
        if (Time.time - lastToggleTime < ToggleCooldown)
            return;

        lastToggleTime = Time.time;


        patron_clicked = !patron_clicked;
        Patron_button.GetComponent<Image>().color = patron_clicked ? Color.green : Color.white;

        if (modify_clicked)
        {
            modify_clicked = false;
            modeModify = false;
            if (objetModify != null)
            {
                ToggleRubanGrabbable(objetModify, true);
                objetModify = null;
            }
            DeleteVerticesShowed();
            Modify_button.GetComponent<Image>().color = Color.white;
        }

        if (sew_clicked)
        {
            sew_clicked = false;
            modeCouture = false;
            Sew_button.GetComponent<Image>().color = Color.white;

            if (rubanSelectionne1 != null)
            {
                Renderer rend1 = rubanSelectionne1.GetComponent<Renderer>();
                if (rend1) rend1.material.color = Color.white;

                rubanSelectionne1 = null;
            }
            Debug.Log("change couyleur 2");

            if (rubanSelectionne2 != null)
            {
                Renderer rend2 = rubanSelectionne2.GetComponent<Renderer>();
                if (rend2) rend2.material.color = Color.white;

                rubanSelectionne2 = null;
            }
            objetSelectionne = null;
        }

        if (modeCloth)
        {
            cloth_clicked = false;
            modeCloth = false;

            foreach (var r in rubans)
                ToggleCloth(r);

            foreach (var p in patronManager.patrons)
                ToggleCloth(p);

            Cloth_button.GetComponent<Image>().color = Color.white;
        }

        if (cut_clicked)
        {
            cut_clicked = false;
            Cut_button.GetComponent<Image>().color = Color.white;
        }
    }

    public void DeleteButton()
    {
        // Vérifier cooldown pour éviter double trigger
        if (Time.time - lastToggleTime < ToggleCooldown)
            return;

        lastToggleTime = Time.time;

        if (objetSelectionne == null)
            return;

        else if (rubans.Contains(objetSelectionne))
        {
            rubans.Remove(objetSelectionne);
            Debug.Log("ruban supprimé");
        }

        else if (patronManager.patrons.Contains(objetSelectionne))
        {
            patronManager.patrons.Remove(objetSelectionne);
            Debug.Log("patron supprimé");
        }

        Destroy(objetSelectionne);
    }

    public void CutButton()
    {
        // Vérifier cooldown pour éviter double trigger
        if (Time.time - lastToggleTime < ToggleCooldown)
            return;

        lastToggleTime = Time.time;

        cut_clicked = !cut_clicked;
        Debug.Log("Mode Découpe " + (cut_clicked ? "activé" : "désactivé"));

        Cut_button.GetComponent<Image>().color = cut_clicked ? Color.green : Color.white;

        if (modeCloth)
        {
            cloth_clicked = false;
            modeCloth = false;

            foreach (var r in rubans)
                ToggleCloth(r);

            foreach (var p in patronManager.patrons)
                ToggleCloth(p);

            Cloth_button.GetComponent<Image>().color = Color.white;
        }

        if (modify_clicked)
        {
            modify_clicked = false;
            modeModify = false;
            if (objetModify != null)
            {
                ToggleRubanGrabbable(objetModify, true);
                objetModify = null;
            }
            DeleteVerticesShowed();
            Modify_button.GetComponent<Image>().color = Color.white;
        }

        if (sew_clicked)
        {
            sew_clicked = false;
            modeCouture = false;
            Sew_button.GetComponent<Image>().color = Color.white;

            if (rubanSelectionne1 != null)
            {
                Renderer rend1 = rubanSelectionne1.GetComponent<Renderer>();
                if (rend1) rend1.material.color = Color.white;

                rubanSelectionne1 = null;
            }
            Debug.Log("change couyleur 2");

            if (rubanSelectionne2 != null)
            {
                Renderer rend2 = rubanSelectionne2.GetComponent<Renderer>();
                if (rend2) rend2.material.color = Color.white;

                rubanSelectionne2 = null;
            }
            objetSelectionne = null;
        }

        if (patron_clicked)
        {
            patron_clicked = false;
            Patron_button.GetComponent<Image>().color = Color.white;
        }
    }
}