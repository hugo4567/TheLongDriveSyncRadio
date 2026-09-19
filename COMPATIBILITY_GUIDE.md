# Compatibilité du Mod TheLongDriveSyncRadio

## Versions Supportées

Ce mod est maintenant compatible avec les versions suivantes de The Long Drive en **multijoueur**:

- **Build 10117180** ✓
- **Build 16536989** ✓

## Fonctionnalités Multijoueur

### ✓ Synchronisation Audio
La radio personnalisée se synchronise entre tous les joueurs de la partie multijoueur.

### ✓ Transfert de Fichiers P2P
Les fichiers audio sont transférés via P2P (peer-to-peer) entre joueurs:
- Automatique au démarrage d'une partie
- Utilise des chunks optimisés par version
- Gestion intelligente de la mémoire

### ✓ Optimisation Réseau
Les paramètres réseau sont automatiquement optimisés selon la version du jeu:

#### Build 10117180
- Timeout P2P: 60 secondes (renforcé)
- Taille chunk: 256 KB
- Délai inter-chunk: 150 ms

#### Build 16536989
- Timeout P2P: 30 secondes (standard)
- Taille chunk: 256 KB
- Délai inter-chunk: 100 ms

## Installation et Configuration

### 1. Installation du Mod
1. Placer le DLL du mod dans le dossier `Mods/` de MelonLoader
2. Lancer le jeu
3. Le mod détecte automatiquement la version du jeu

### 2. Configuration (AudioSyncConfig.xml)

Le fichier `AudioSyncConfig.xml` contient les paramètres:

```xml
<compatibility>
    <supportedGameBuildIds>
        <buildId>10117180</buildId>
        <buildId>16536989</buildId>
    </supportedGameBuildIds>
</compatibility>
```

### 3. Paramètres Réseau

Pour ajuster les paramètres réseau, modifier dans `AudioSyncConfig.xml`:

```xml
<network>
    <maxRetries>3</maxRetries>
    <sendTimeout>30</sendTimeout>
    <maxBufferSize>524288</maxBufferSize>
</network>

<audioStreaming>
    <chunkSize>262144</chunkSize>
    <chunkDelay>100</chunkDelay>
</audioStreaming>
```

## Dépannage

### Le mod ne se charge pas
- Vérifier que MelonLoader est installé correctement
- Vérifier les logs dans `MelonLoader/Logs/`
- S'assurer que votre version du jeu est l'une des versions supportées

### Les fichiers audio ne se synchronisent pas
- Vérifier la connexion réseau
- Vérifier les logs pour les erreurs P2P
- S'assurer que vous êtes dans une partie multijoueur
- Vérifier que le dossier CustomRadio est accessible en écriture

### Connexion P2P instable
- Réduire la taille des chunks dans la config
- Augmenter le délai inter-chunk
- Vérifier le pare-feu Steam

## Détection Automatique de Version

Le mod détecte automatiquement votre version de jeu au démarrage et applique:
- Les patchs Harmony appropriés
- Les paramètres réseau optimisés
- Les configurations de synchronisation adaptées

Vous verrez dans les logs:
```
[GameVersionManager] Version détectée: XXXXX
✓ Version XXXXX est supportée
✓ Compatibilité multijoueur validée pour XXXXX
```

## Logs et Débogage

Pour activer les logs détaillés, modifier dans `AudioSyncConfig.xml`:

```xml
<logging>
    <verbosity>3</verbosity>
    <logAudioSync>true</logAudioSync>
    <logStreaming>true</logStreaming>
</logging>
```

Les logs se trouvent dans:
- Windows: `MelonLoader/Logs/game_start.log`
- Linux: `~/.melonloader/Logs/game_start.log`

## Développement et Ajout de Versions

Pour ajouter une nouvelle version compatible:

1. Ajouter l'ID de build dans `AudioSyncConfig.xml`:
```xml
<supportedGameBuildIds>
    <buildId>NOUVEAU_ID</buildId>
</supportedGameBuildIds>
```

2. Créer une fonction de patch dans `GameVersionManager.cs`:
```csharp
private static void ApplyPatches_vNOUVEAU_ID()
{
    // Implémenter les patchs spécifiques
}
```

3. Ajouter la condition dans `ApplyVersionSpecificPatches()`:
```csharp
else if (currentId == "NOUVEAU_ID")
{
    ApplyPatches_vNOUVEAU_ID();
}
```

## Support

Pour les problèmes ou suggestions:
1. Vérifier les logs détaillés
2. Consulter la documentation
3. Reporter les issues avec logs complets

---

**Dernière mise à jour**: 28 mai 2026
**Versions supportées**: 10117180, 16536989
**Mode**: Multijoueur (P2P Steamworks)
