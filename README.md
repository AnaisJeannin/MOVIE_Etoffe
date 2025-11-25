## **README - Etoffe Virtuelle**

*Héloïse Couet, Eléonore Leroy, Lise Feliers et Anaïs Jeannin*



Le projet a été mené avec l'artiste Céline Shen. Il avait pour but de permettre la création de tissus à partir de mouvements des mains. Le projet final permet de créer des rubans et des patrons (surfaces créées à partir du dessin d'un contour). Ces objets peuvent ensuite être sélectionnés, déplacés et modifiés. On peut aussi leur appliquer différents matériaux. Dans le cas des rubans, il est possible de les couper dans le sens de la largeur et de les coudre ensemble (les uns à la suite des autres : les deux extrémités les plus proches sont choisies).

Les objets créés en runtime sont des meshs. On peut décider de leur appliquer un composant cloth pour les soumettre à la gravité et leur donner les caractères physiques de tissus.  



##### **Scripts**

* ###### Main

Gère la génération des rubans: la création du mesh (avec une coroutine) et l'ajout des interactions pour les rendre grabbable. Main gère aussi les différentes fonctionnalités du projet : couper, coudre, passer en cloth et l'application de différents matériaux aux meshs créés. Le menu (boutons, slider, dropdown) est implémenté dans ce script.



Tous les autres scripts du projet sont reliés à Main pour permettre la modification des objets, leur sélection et la création de patron.



* ###### Patron

Gère la génération de patron : création d'une surface grâce au dessin de son contour. Même concept que pour la création des rubans. 



* ###### VertexHandle

Gère la modification des objets en runtime (rubans et patrons) : permet d'afficher des "handles" liées aux vertices pour pouvoir déplacer ces derniers. Il permet aussi d'appliquer une déformation gaussienne pour que les sommets proches de celui déplacé suivent le mouvement (ce n'est pas encore très au point).



* ###### ruban\_selector

Permet de sélectionner un objet (ruban ou patron) avec le collider placé sur le bas de la paume. Dans le cas du mode couture, il permet de sélectionner deux rubans en même temps. 

##### 

##### **Assets importés**

* ###### Matériau

Importation du package : https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@11.0/manual/silk-shader.html

* ###### Mannequin de couture

Importation du ficher (en format glb) disponible en open source ici : https://sketchfab.com/3d-models/tailors-mannequin-27e0ad0ae671457a848e863707b2e195

* ###### Interactions VR/Handtracking

Meta XR Core SDK, Meta XR Interaction SDK, Meta XR Interaction SDK Essentials, XR hands, XR interaction toolkit, XR plugin management.

##### 

##### **Fonctionnement du projet**



cf : vidéo du projet



Toutes les interactions se font en handtracking. Les gestes utilisés sont les gestes de base de Meta : pinch, pock et grab. 





