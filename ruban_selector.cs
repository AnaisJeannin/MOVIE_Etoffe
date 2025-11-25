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

//Script permettant de selectionner un objet (ruban ou patron) ou deux si modeCouture
public class HandSelector : MonoBehaviour
{
    //Lien avec les autres scripts
    public Main manager;
    public Patron patronManager;

    void Start()
    {
        if (manager == null)
            manager = FindFirstObjectByType<Main>();

        if (patronManager == null)
            patronManager = FindFirstObjectByType<Patron>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!manager.rubans.Contains(other.gameObject) && !patronManager.patrons.Contains(other.gameObject)) return;

        if (manager.modeCouture) //Gère la selection de deux rubans pour la couture : rubanSelectionne1 et rubanSelectionne2
        {
            manager.modeCloth = false;
            if (manager.objetSelectionne != manager.rubanSelectionne1 && manager.objetSelectionne != manager.rubanSelectionne2)
            {
                Renderer rend = manager.objetSelectionne.GetComponent<Renderer>();
                if (rend) rend.material.color = Color.white;
                manager.objetSelectionne = null;
            }

            if (manager.rubanSelectionne1 == null)
            {
                manager.rubanSelectionne1 = other.gameObject;
                manager.objetSelectionne = manager.rubanSelectionne1; // Pour le déplacement

                Renderer rend1 = manager.rubanSelectionne1.GetComponent<Renderer>();
                if (rend1) rend1.material.color = Color.yellow;

                Debug.Log("Ruban 1 sélectionné par collision : " + manager.rubanSelectionne1.name);
            }

            else if (manager.rubanSelectionne1 != null)
            {
                if (other.gameObject != manager.rubanSelectionne1)
                {
                    if (manager.rubanSelectionne2 != null)
                    {
                        // Remet la couleur normale si un ruban 2 était déjà sélectionné
                        Renderer rend2O = manager.rubanSelectionne2.GetComponent<Renderer>();
                        if (rend2O) rend2O.material.color = Color.white;
                    }

                    manager.rubanSelectionne2 = other.gameObject;
                    manager.objetSelectionne = manager.rubanSelectionne2; // Pour le déplacement

                    Renderer rend2 = manager.rubanSelectionne2.GetComponent<Renderer>();
                    if (rend2) rend2.material.color = Color.yellow;

                    Debug.Log("Ruban 2 sélectionné par collision : " + manager.rubanSelectionne2.name);
                    Debug.Log("On peut coudre");
                }

                else if (other.gameObject == manager.rubanSelectionne1)
                {
                    Renderer rend1 = manager.rubanSelectionne1.GetComponent<Renderer>();
                    if (rend1) rend1.material.color = Color.white;

                    manager.rubanSelectionne1 = null;
                    Debug.Log("Ruban 1 désélectionné par collision");
                }
            }
        }

        else //Gère la sélection d'un objet : patron ou ruban 
        {
            manager.rubanSelectionne1 = null;
            manager.rubanSelectionne2 = null;

            // Couture désactivée, on remet toutes les couleurs normales
            foreach (var ruban in manager.rubans)
            {
                Renderer rend = ruban.GetComponent<Renderer>();
                if (rend) rend.material.color = Color.white;
            }

            foreach (var patron in patronManager.patrons)
            {
                Renderer rend = patron.GetComponent<Renderer>();
                if (rend) rend.material.color = Color.white;
            }

            if (manager.objetSelectionne != null)
            {
                Renderer rend = manager.objetSelectionne.GetComponent<Renderer>();
                if (rend) rend.material.color = Color.yellow;
            }


            // Toggle : si c'était déjà sélectionné, on désélectionne
            if (manager.objetSelectionne == other.gameObject)
            {
                DeselectObjet(manager.objetSelectionne);
                manager.objetSelectionne = null;
                Debug.Log("Objet désélectionné : " + other.gameObject.name);
            }
            else
            {
                // Deselect précédent
                if (manager.objetSelectionne != null)
                    DeselectObjet(manager.objetSelectionne);

                // Sélectionner nouveau ruban
                manager.objetSelectionne = other.gameObject;
                SelectObjet(manager.objetSelectionne);
                Debug.Log("Objet sélectionné par collision : " + manager.objetSelectionne.name);

                if (manager.modeModify && manager.vertexHandles.Count == 0)
                {
                    manager.objetModify = manager.objetSelectionne;
                    manager.ToggleRubanGrabbable(manager.objetModify, false);
                    manager.ShowVertices(manager.objetModify);
                } 
            }
        }
    }

    private void SelectObjet(GameObject objet)
    {
        Renderer rend = objet.GetComponent<Renderer>();
        if (rend) rend.material.color = Color.yellow;
    }

    private void DeselectObjet(GameObject objet)
    {
        Renderer rend = objet.GetComponent<Renderer>();
        if (rend) rend.material.color = Color.white;
    }
}