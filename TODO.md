# TODO - Compilation et release

## Sur l'autre PC

- [ ] Installer Visual Studio 2022 avec la charge de travail **Développement .NET Desktop**.
- [ ] Installer le **.NET Framework 4.7.2 Developer Pack / Targeting Pack**.
- [ ] Installer ou vérifier le SDK .NET requis.
- [ ] Copier le projet sur le PC.
- [ ] Vérifier que les DLL du jeu et de MelonLoader sont présentes dans `lib/`.
- [ ] Adapter le chemin du jeu dans `setup_dependencies.ps1` si nécessaire.
- [ ] Exécuter `setup_dependencies.ps1` pour récupérer les dépendances manquantes.
- [ ] Ouvrir `TheLongDriveSyncRadio.sln` dans Visual Studio.
- [ ] Sélectionner `Release` et `x64`.
- [ ] Compiler le projet.
- [ ] Vérifier que `bin/x64/Release/TheLongDriveSyncRadio.dll` a été créé.
- [ ] Tester le mod dans The Long Drive.

## Préparer la release GitHub

- [ ] Créer un fichier ZIP contenant la DLL et les fichiers nécessaires à l'installation.
- [ ] Vérifier que le ZIP ne contient pas de fichiers inutiles comme les fichiers `.pdb` ou les dossiers `obj/`.
- [ ] Créer un tag Git, par exemple `v1.0.0`.
- [ ] Créer une release GitHub à partir de ce tag.
- [ ] Ajouter le fichier ZIP comme fichier joint à la release.
- [ ] Ajouter les changements principaux dans la description de la release.
- [ ] Tester le téléchargement et l'installation depuis la release.

## Problème connu

La compilation avec `dotnet build` échoue si les assemblys de référence du **.NET Framework 4.7.2** ne sont pas installés. Installer le Developer Pack correspondant avant de relancer la compilation.
