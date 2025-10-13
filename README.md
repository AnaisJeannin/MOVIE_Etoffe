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
Comme Ruban mais on clique sur la molette pour créer un cloth et aucun plan n'est ajouté. \

**MeshClothV1 :** \
En cliqaunt sur la molette de la souris, le ruban qui est créé avec le MeshTimer devient un cloth (le mesh renderer est enlevé et un cloth est créé). En rappuyant sur la molette, le mesh renderer réapparait et le cloth est détruit.\

**MeshClothV2 :** \
clic gauche : début coroutine, toutes les 0.1s une nouvelle maille est créée en suivant le mouvement de la souris.\
clic droit : arrêt coroutine\
Si on refait clic gauche on crée un nouveau ruban\
clic milieu : Mesh -> Cloth ou Cloth -> Mesh\
Problèmes : les rubans se traversent et on peut créer des mesh quand le reste est en cloth 
