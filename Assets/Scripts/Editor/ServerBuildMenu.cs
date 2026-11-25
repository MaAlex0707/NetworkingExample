using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace NetworkingLab.Editor
{
    /// <summary>
    /// Menú "Lab > Construir servidor Linux (NGO)".
    /// Un clic genera el Dedicated Server en la carpeta y con el nombre que espera el Dockerfile.
    /// </summary>
    public static class ServerBuildMenu
    {
        private const string Scene = "Assets/Scenes/NGOGameplay.unity";
        private const string Output = "Builds/NGO-LinuxServer/NetworkingLabNGOServer.x86_64";

        [MenuItem("Lab/Construir servidor Linux (NGO)")]
        public static void BuildNgoServer()
        {
            // Necesario para que el servidor pueda avisar a Agones (Ready/Health) por HTTP local.
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { Scene },
                locationPathName = Output,
                target = BuildTarget.StandaloneLinux64,
                subtarget = (int)StandaloneBuildSubtarget.Server,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"SERVIDOR LISTO: {Output}");
                EditorUtility.RevealInFinder(Output);
                EditorUtility.DisplayDialog("Servidor generado",
                    "Build listo en Builds/NGO-LinuxServer.\n\nAhora ejecuta desplegar.ps1 en PowerShell.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Build falló",
                    "Revisa la consola. Si dice que falta Linux Dedicated Server Build Support, instálalo desde Unity Hub > Installs > Add modules.",
                    "OK");
            }
        }
    }
}
