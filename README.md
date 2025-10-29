**MeshGeneratorV1 :** \
situation initiale : carré \
à chaque clic gauche : nouveau carré à la suite \
Point négatif : on ne décide pas de la direction du ruban en mode game 

**ClickMesh** \
situation initiale : carré \
à chaque clic gauche : créé un nouveau carré dont le sommet "bas droit" est au niveau du pointeur de la souris. Le nouveau carré est collé au précédent.\
points négatifs : un seul côté du mesh s'affiche + il faudrait passer en continu (plus de clic de souris). 

**MeshTimer**\
situation initiale : carré\
clic gauche : début coroutine, toutes les 0.1s une nouvelle maille est créée en suivant le mouvement de la souris.\
clic droit : arrêt coroutine\
point négatif : un seul ruban -> si on arrête et reprend, on continue le ruban précédent.


**MeshTimerV1**\
situation initiale : rectangle fin\
clic gauche : début coroutine, toutes les 0.1s une nouvelle maille est créée en suivant le mouvement de la souris.\
clic droit : arrêt coroutine\
clic milieu : début d'un nouveau ruban (création rectangle fin à l'emplacement de la souris)

**MeshFrequence :** \
Idem que MeshGeneratorV1 mais les carrés se forment toutes les secondes et non à chaque clic \
Point négatif : les mêmes + il faut revoir timerMax et les intervalles

**Ruban :** \
Je reprend le script MeshGeneratorV1 de Anais avec le ruban avec des clics. Quand je double clic, le ruban devient cloth. 
Problème : le tissu cloth est comme dupliqué de l'initial, donc le cloth tombe mais il y a encore le ruban mesh en hauteur.

**MeshClothV0 :** \
Comme Ruban mais on clique sur la molette pour créer un cloth et aucun plan n'est ajouté. 

**MeshClothV1 :** \
En cliqaunt sur la molette de la souris, le ruban qui est créé avec le MeshTimer devient un cloth (le mesh renderer est enlevé et un cloth est créé). En rappuyant sur la molette, le mesh renderer réapparait et le cloth est détruit.

**MeshClothV2 :** \
clic gauche : début coroutine, toutes les 0.1s une nouvelle maille est créée en suivant le mouvement de la souris.\
clic droit : arrêt coroutine\
Si on refait clic gauche on crée un nouveau ruban\
clic milieu : Mesh -> Cloth ou Cloth -> Mesh\
Problèmes : les rubans se traversent et on peut créer des mesh quand le reste est en cloth 

**MeshClothFix :** \
Même code que MeshClothV2 + je fixe le premier vertex de chaque ruban => il se déplie dans la longueur en passant en cloth 

**MeshClothFixV1 :** \
Cette fois-ci, le premier vertex se fixe uniquement s'il est sur une sphère précise. \
Attention : il faut créer une sphère dans la scène et l'assigner au Mesh Generator (en la glissant dans la zpne "sphere" 

**MeshMove :** \
clic gauche : commence un ruban \
clic droit : arrête le ruban en cours \
clic molette : mesh <-> cloth \
clic barre espace : le dernier ruban suit la osition de la souris (ou s'arrête selon ce qu'il faisait avant)

**Ruban_test_main :** \

meme code que le ruban / cloth mais pour la VR

**Select** \
clic touche C : créer ruban \
clic droit souris : arrêter ruban \
clic milieu : mode cloth ON/OFF \
clic gauche sur ruban  : le selectionne et l'affiche en jaune \
clic espace : déplacer le ruban sélectionné\

**Cut :** \
clic gauche : début d'un ruban \
clic droit : arrêter le ruban \
clic molette : mesh <-> cloth \
clic barre espace : déplacer ruban \
clic c : couper le ruban (il faut être assez proche) 

**CoudreV0 :** \
clic C : commencer ruban \
clic droit : finir ruban \
clic gauche : sélectionner un ruban \
clic espace : déplacer le ruban sélectionner \
clic molette : cloth <-> mesh \
clic P : regarder si des rubans sont proches -> colorie en rouge si oui \
clic I : coudre deux rubans proches \
clic O : décider de ne pas coudre (peut-être pas nécessaire)

**DemoV1 :** \
clic R : commencer ruban \
clic droit : arrêter ruban \
clic gauche : sélectionner un ruban \
clic espace : déplacer le ruban sélectionner \
clic molette : cloth <-> mesh \
clic P : mode couture activé \
-> pour coudre (si 2 rubans sont rouges) : sélectionner celui de droite puis \
clic I : coudre deux rubans proches \
clic O : décider de ne pas coudre (peut-être pas nécessaire) \
clic M : modifier un ruban

**DemoV2 :** \
clic R : commencer ruban \
clic droit : arrêter ruban \
clic gauche : sélectionner un ruban \
clic espace : déplacer le ruban sélectionner \
clic molette : cloth <-> mesh \
clic P : mode couture activé \
-> sélectionner les 2 à coudre \
clic I : coudre deux rubans proches \
clic M : modifier un ruban

**MateriauxV0 :** \
clic R : commencer ruban \
clic droit : arrêter ruban \
clic gauche : sélectionner un ruban \
clic espace : déplacer le ruban sélectionner \
clic molette : cloth <-> mesh \
clic P : mode couture activé \
-> sélectionner les 2 à coudre \
clic I : coudre deux rubans proches \
clic M : modifier un ruban \
clic O : met le matériaux soie sur le ruban sélectionné en mesh \
Il faut importer un package https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@11.0/manual/silk-shader.html

**MateriauxV1 :** \
clic R : commencer ruban \
clic droit : arrêter ruban \
clic gauche : sélectionner un ruban \
clic espace : déplacer le ruban sélectionner \
clic molette : cloth <-> mesh \
clic P : mode couture activé \
-> sélectionner les 2 à coudre \
clic I : coudre deux rubans proches \
clic M : modifier un ruban \
clic O : met le matériaux soie sur le ruban sélectionné en mesh \
clic L : met le matériaux coton sur le ruban sélectionné en mesh \
clic K : met le matériaux denim sur le ruban sélectionné en mesh \
clic T : met le matériaux laine sur le ruban sélectionné en mesh \
clic N : met le matériaux velours sur le ruban sélectionné en mesh \
clic B : met le matériaux lin sur le ruban sélectionné en mesh \
clic V : met le matériaux satin sur le ruban sélectionné en mesh \
clic J : met le matériaux nylon sur le ruban sélectionné en mesh \
Il faut importer un package https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@11.0/manual/silk-shader.html

**Importations VR :** \
meta XR core sdk \
meta xr interaction sdk \
meta xr interaction sdk essentials \
xr hands \
xr interaction toolkit \
xr plugin management

**DemoV3** \
Fonctionne avec VertexHandle pour modifier les meshs des rubans ( Attention bug : faire attention de ne pas avoir un fichier qui fait référence à vertexHandle en même temps (version précédente par exemple), si c'est le cas, commenter la ligne "handle.GetComponent<VertexHandle>().Init(this, ruban, i, original);" dans la fonction ShowVertices du fichier non utile). VertexHandle (radius = 0.2, force = 1) doit aussi être un script de l'objet mesh/meshcreator.

Scene : \
Ajouter 3 boutons (dans + , UI, Button - TextMeshPro ), les disposer dans un coin du canvas (les boutons se mettent tout seul en enfant d'un canvas), modifier le Text(TMP) (enfant de chaque bouton) en Modify, Sew, Cloth.\
Ajouter un Text (dans +, UI, Text-TextMeshPro), le laisser vide (effacer si quelque chose est écrit), le mettre au milieu du canvas. \
Glisser les boutons et le Text (objets qui viennent d'être créés) sur le code DemoV3 attaché à l'objet (mesh/meshcreator?) aux emplacements prévus. \
Pour la fonction modifier, on a besoin d'un prefab de sphère (vertexHandlePrefab) :Scale (1,1,1),  Sphere collider (radius = 1).

clic R : commencer ruban \
clic droit : arrêter ruban \
clic gauche : sélectionner un ruban \
clic espace : déplacer le ruban sélectionner \
bouton Cloth: cloth <-> mesh \
bouton Sew : mode couture activé \
-> sélectionner les 2 à coudre \
clic I : coudre deux rubans proches \
bouton Modify : modifier un ruban

**DemoV4** \
clic R : commencer ruban \
clic droit : arrêter ruban \
clic gauche : sélectionner un ruban \
clic espace : déplacer le ruban sélectionner \
bouton Cloth: cloth <-> mesh \
bouton Sew : mode couture activé \
-> sélectionner les 2 à coudre \
clic I : coudre deux rubans proches \
bouton Modify : modifier un ruban \
clic O : met le matériaux soie sur le ruban sélectionné en mesh \
clic L : met le matériaux coton sur le ruban sélectionné en mesh \
clic K : met le matériaux denim sur le ruban sélectionné en mesh \
clic T : met le matériaux laine sur le ruban sélectionné en mesh \
clic N : met le matériaux velours sur le ruban sélectionné en mesh \
clic B : met le matériaux lin sur le ruban sélectionné en mesh \
clic V : met le matériaux satin sur le ruban sélectionné en mesh \
clic J : met le matériaux nylon sur le ruban sélectionné en mesh \
Il faut importer un package https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@11.0/manual/silk-shader.html

**DemoV5** \
clic R : commencer ruban \
clic droit : arrêter ruban \
clic gauche : sélectionner un ruban \
clic espace : déplacer le ruban sélectionner \
bouton Cloth: cloth <-> mesh \
bouton Sew : mode couture activé \
-> sélectionner les 2 à coudre \
clic I : coudre deux rubans proches \
bouton Modify : modifier un ruban \
Dropdown: choix du matériau \
Il faut importer un package https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@11.0/manual/silk-shader.html
