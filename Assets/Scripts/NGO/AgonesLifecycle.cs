using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

namespace NetworkingLab.NGO
{
    /// <summary>
    /// Integra el ciclo de vida de Agones (Ready y Health) usando el REST sidecar.
    /// Se crea solo: no hay que arrastrarlo a ninguna escena.
    /// Solo se activa cuando el juego corre como servidor (batchmode) dentro de Agones,
    /// es decir, cuando existe la variable de entorno AGONES_SDK_HTTP_PORT.
    /// En el Editor y en los clientes no hace nada.
    /// </summary>
    public sealed class AgonesLifecycle : MonoBehaviour
    {
        private const float HealthIntervalSeconds = 2f;
        private string baseUrl;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (!Application.isBatchMode)
            {
                return;
            }

            string port = Environment.GetEnvironmentVariable("AGONES_SDK_HTTP_PORT");
            if (string.IsNullOrEmpty(port))
            {
                Debug.Log("AGONES: AGONES_SDK_HTTP_PORT no existe; se omite Ready/Health (servidor fuera de Agones).");
                return;
            }

            GameObject go = new GameObject("AgonesLifecycle");
            DontDestroyOnLoad(go);
            go.AddComponent<AgonesLifecycle>().baseUrl = $"http://localhost:{port}";
        }

        private IEnumerator Start()
        {
            // 1. Esperar a que NGO abra el socket UDP (servidor escuchando).
            while (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            {
                yield return new WaitForSeconds(0.5f);
            }

            // 2. Avisar a Agones que el servidor acepta jugadores.
            yield return Post("/ready");
            Debug.Log("AGONES: POST /ready enviado");

            // 3. Reportar salud periódicamente mientras el servidor viva.
            WaitForSeconds wait = new WaitForSeconds(HealthIntervalSeconds);
            while (true)
            {
                yield return Post("/health");
                yield return wait;
            }
        }

        private IEnumerator Post(string path)
        {
            using (UnityWebRequest request = new UnityWebRequest(baseUrl + path, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes("{}"));
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"AGONES: {path} falló: {request.error}");
                }
            }
        }
    }
}
