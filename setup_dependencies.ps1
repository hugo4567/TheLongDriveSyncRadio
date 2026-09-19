$gamePath = "C:\Program Files (x86)\Steam\steamapps\common\The Long Drive"
$libPath = ".\lib"

# Créer le dossier lib s'il n'existe pas
if (!(Test-Path $libPath)) {
    New-Item -ItemType Directory -Path $libPath
}

# Liste des DLLs à copier
$dlls = @(
    "MelonLoader\net35\0Harmony.dll",
    "MelonLoader\net35\MelonLoader.dll",
    "TheLongDrive_Data\Managed\Assembly-CSharp.dll",
    "TheLongDrive_Data\Managed\Assembly-CSharp-firstpass.dll",
    "TheLongDrive_Data\Managed\UnityEngine.dll",
    "TheLongDrive_Data\Managed\UnityEngine.CoreModule.dll",
    "TheLongDrive_Data\Managed\UnityEngine.IMGUIModule.dll",
    "TheLongDrive_Data\Managed\UnityEngine.UI.dll",
    "TheLongDrive_Data\Managed\UnityEngine.TextRenderingModule.dll",
    "TheLongDrive_Data\Plugins\Steamworks.NET.dll"
)

# Copier chaque DLL
foreach ($dll in $dlls) {
    $sourcePath = Join-Path $gamePath $dll
    $destPath = Join-Path $libPath (Split-Path $dll -Leaf)
    
    if (Test-Path $sourcePath) {
        Copy-Item $sourcePath $destPath -Force
        Write-Host "Copié : $dll"
    } else {
        Write-Host "Erreur : $dll non trouvé dans $sourcePath"
    }
}

Write-Host "`nConfiguration terminée. Vous pouvez maintenant compiler le mod."