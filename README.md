# Radio Sync Mod pour The Long Drive
Ce mod synchronise automatiquement les radios personnalisées entre l’hôte et les clients dans The Long Drive. Il prend également en charge la synchronisation des titres et la diffusion audio en direct, sur la base de la dernière version multijoueur du jeu.



Il s’agit de mon propre fork de ce mod, basé sur la dernière version multijoueur, avec une amélioration majeure permettant de synchroniser les titres et de diffuser la radio en direct.
## Fonctionnalités

- Synchronisation automatique des fichiers audio (MP3, WAV, OGG)
- Support des gros fichiers (jusqu'à 500 MB)
- Interface utilisateur intuitive (touche F7)
- Gestion intelligente du cache
- Reprise des transferts en cas d'erreur
- Affichage des métadonnées des fichiers audio
- Notifications en jeu des événements

## Installation

1. Téléchargez et installez [Melonloader](https://github.com/LavaGang/MelonLoader/releases/latest/)
2. Téléchargez le mod depuis l'onglet [Releases](https://github.com/DeltaNeverUsed/TheLongDriveSyncRadio/releases/latest/)
3. Placez le fichier dll du mod dans le dossier Mods du jeu The Long Drive

## Utilisation

- Appuyez sur F7 pour ouvrir/fermer l'interface
- Les fichiers sont automatiquement synchronisés en rejoignant une partie
- Le bouton "Synchroniser maintenant" force une nouvelle synchronisation
- Les métadonnées des fichiers audio sont affichées dans l'interface
- Une barre de progression indique l'avancement des transferts
- Les notifications apparaissent en haut à droite de l'écran

## Gestion du cache

Le mod utilise un système de cache intelligent qui :
- Stocke les fichiers téléchargés pour une réutilisation future
- Nettoie automatiquement le cache si nécessaire (limite de 2 GB)
- Améliore la vitesse des transferts répétés

## Dépannage

- En cas d'erreur de transfert, le mod réessaiera automatiquement
- Les transferts peuvent être annulés si nécessaire
- Les erreurs sont enregistrées dans un fichier de log pour le dépannage
