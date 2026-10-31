using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Valida la música en una escena vacía sin tocar el progreso del jugador.</summary>
[InitializeOnLoad]
public static class ValidarMusicaZona
{
    private const string Sesion = "MusicaZona.QA";
    [Serializable] private class EscenaGuardada
    {
        public string ruta;
        public bool cargada;
        public bool activa;
    }
    [Serializable] private class EscenasGuardadas { public EscenaGuardada[] escenas; }
    private static double limite, siguiente;
    private static int paso;
    private static FisheryManager gestor;
    private static AudioSource fuente;
    private static float tiempoAnterior;
    private static string error;

    static ValidarMusicaZona()
    {
        if (SessionState.GetBool(Sesion, false)) Registrar();
    }

    public static void Ejecutar()
    {
        if (!Application.isBatchMode)
            throw new InvalidOperationException("Ejecutar en batchmode con el Editor del proyecto cerrado.");
        var zona = AssetDatabase.LoadAssetAtPath<ZonaData>("Assets/Scripts/Zonas/Amazonas.asset");
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Bruma_del_Caqueta_65BPM_LOOP.wav");
        Exigir(clip != null && clip.length > 0 && zona.musicaAmbiente == clip,
            "La canción no está importada o asignada a Amazonas.");
        Exigir(clip.loadType == AudioClipLoadType.Streaming, "La música debe usar streaming.");
        Debug.Log("MUSICA QA: clip importado, duración=" + clip.length + " s, canales=" + clip.channels);
        var sanAndres = Resources.Load<ZonaData>("Zonas/SanAndres");
        var clipCaribe = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Brisa_de_San_Andres_104BPM_LOOP.wav");
        Exigir(clipCaribe != null && sanAndres != null && sanAndres.musicaAmbiente == clipCaribe,
            "La canción de San Andrés no está importada o asignada.");
        Exigir(clipCaribe.length >= 120 && clipCaribe.length <= 180 && clipCaribe.channels == 2,
            "San Andrés debe tener una canción estéreo de 2 a 3 minutos.");
        Exigir(clipCaribe.loadType == AudioClipLoadType.Streaming, "San Andrés debe usar streaming.");
        Debug.Log("MUSICA QA: San Andrés importado, duración=" + clipCaribe.length + " s, canales=" + clipCaribe.channels);
        var originales = EditorSceneManager.GetSceneManagerSetup();
        var respaldo = new EscenasGuardadas
        {
            escenas = Array.ConvertAll(originales, escena => new EscenaGuardada
            {
                ruta = escena.path, cargada = escena.isLoaded, activa = escena.isActive
            })
        };
        SessionState.SetString(Sesion + ".escenas", JsonUtility.ToJson(respaldo));
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Sesion, true);
        Registrar();
        EditorApplication.EnterPlaymode();
    }

    private static void Registrar()
    {
        limite = EditorApplication.timeSinceStartup + 60;
        EditorApplication.update -= Verificar;
        EditorApplication.update += Verificar;
        EditorApplication.playModeStateChanged -= CambioModo;
        EditorApplication.playModeStateChanged += CambioModo;
        Application.logMessageReceived -= RegistrarError;
        Application.logMessageReceived += RegistrarError;
    }

    private static void Verificar()
    {
        try
        {
            if (error != null) throw new Exception(error);
            if (EditorApplication.timeSinceStartup > limite) throw new Exception("Tiempo de validación agotado.");
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
            switch (paso++)
            {
                case 0:
                    Application.runInBackground = true;
                    new GameObject("Oyente QA", typeof(AudioListener));
                    var objeto = new GameObject("Gestor QA");
                    objeto.SetActive(false);
                    gestor = objeto.AddComponent<FisheryManager>();
                    var datos = new SerializedObject(gestor);
                    datos.FindProperty("zonaAmazonas").objectReferenceValue =
                        AssetDatabase.LoadAssetAtPath<ZonaData>("Assets/Scripts/Zonas/Amazonas.asset");
                    datos.ApplyModifiedPropertiesWithoutUndo();
                    objeto.SetActive(true);
                    fuente = gestor.GetComponent<AudioSource>();
                    siguiente = EditorApplication.timeSinceStartup + 2;
                    break;
                case 1:
                    Exigir(fuente != null && fuente.isPlaying && fuente.loop && fuente.spatialBlend == 0f,
                        "Amazonas no reproduce música 2D en bucle.");
                    Exigir(fuente.clip == gestor.ZonaInicial.musicaAmbiente, "Clip inicial incorrecto.");
                    tiempoAnterior = fuente.time;
                    Exigir(tiempoAnterior > 0f, "La reproducción no avanza.");
                    Exigir(gestor.CambiarZona(gestor.ZonaInicial) && fuente.time >= tiempoAnterior,
                        "Volver a seleccionar Amazonas reinicia la música.");
                    Exigir(!gestor.CambiarZona(null) && fuente.isPlaying, "Un destino inválido interrumpe la música.");
                    Exigir(gestor.CambiarZona(gestor.ZonaSanAndres), "No se pudo viajar a San Andrés.");
                    Exigir(fuente.clip == gestor.ZonaSanAndres.musicaAmbiente && fuente.clip != gestor.ZonaInicial.musicaAmbiente,
                        "No se asignó la canción del Caribe al viajar.");
                    siguiente = EditorApplication.timeSinceStartup + 2;
                    break;
                case 2:
                    Exigir(fuente.isPlaying && fuente.time > 0f, "La canción de San Andrés no avanza.");
                    tiempoAnterior = fuente.time;
                    Exigir(gestor.CambiarZona(gestor.ZonaSanAndres) && fuente.time >= tiempoAnterior,
                        "Seleccionar San Andrés de nuevo reinicia la canción.");
                    fuente.time = fuente.clip.length - 0.5f;
                    siguiente = EditorApplication.timeSinceStartup + 2;
                    break;
                case 3:
                    Exigir(fuente.isPlaying && fuente.time < 10f, "La canción de San Andrés no se repite en bucle.");
                    Exigir(gestor.CambiarZona(gestor.ZonaInicial), "No se pudo volver a Amazonas.");
                    Exigir(fuente.clip == gestor.ZonaInicial.musicaAmbiente, "No se restauró la canción de Amazonas.");
                    siguiente = EditorApplication.timeSinceStartup + 2;
                    break;
                case 4:
                    Exigir(fuente.isPlaying && fuente.time > 0f, "La música no vuelve al regresar a Amazonas.");
                    var duplicado = new GameObject("Gestor duplicado QA");
                    duplicado.AddComponent<FisheryManager>();
                    siguiente = EditorApplication.timeSinceStartup + 1;
                    break;
                case 5:
                    Exigir(Array.FindAll(UnityEngine.Object.FindObjectsByType<AudioSource>(), audio => audio.loop).Length == 1,
                        "Hay reproductores de música duplicados.");
                    Exigir(fuente.isPlaying, "El gestor duplicado interrumpió la música.");
                    // Comprueba el salto al principio del bucle sin esperar toda la canción.
                    fuente.time = fuente.clip.length - 0.5f;
                    siguiente = EditorApplication.timeSinceStartup + 2;
                    break;
                case 6:
                    Exigir(fuente.isPlaying && fuente.time < 10f, "El clip no vuelve al inicio al terminar.");
                    Debug.Log("MUSICA QA OK: reproducción y bucle de Amazonas y San Andrés, selección repetida, destino inválido, viaje de ida/vuelta y ausencia de duplicados.");
                    Terminar(0);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Terminar(1);
        }
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }

    private static void RegistrarError(string mensaje, string pila, LogType tipo)
    {
        if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) error = mensaje;
    }

    private static void Terminar(int codigo)
    {
        SessionState.SetInt(Sesion + ".codigo", codigo);
        EditorApplication.update -= Verificar;
        EditorApplication.ExitPlaymode();
    }

    private static void CambioModo(PlayModeStateChange estado)
    {
        if (estado != PlayModeStateChange.EnteredEditMode) return;
        SessionState.SetBool(Sesion, false);
        int codigo = SessionState.GetInt(Sesion + ".codigo", 1);
        try
        {
            var respaldo = JsonUtility.FromJson<EscenasGuardadas>(SessionState.GetString(Sesion + ".escenas", "{}"));
            var escenas = respaldo?.escenas;
            // Una escena temporal sin ruta no se puede reabrir en la siguiente sesión.
            if (escenas != null && escenas.Length > 0 &&
                Array.TrueForAll(escenas, escena => !string.IsNullOrEmpty(escena.ruta)))
            {
                EditorSceneManager.RestoreSceneManagerSetup(Array.ConvertAll(escenas, escena => new SceneSetup
                {
                    path = escena.ruta, isLoaded = escena.cargada, isActive = escena.activa
                }));
            }
            else
            {
                EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
            }
            Debug.Log("MUSICA QA: escenas de trabajo restauradas antes de cerrar el Editor.");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            codigo = 1;
        }
        finally
        {
            SessionState.EraseString(Sesion + ".escenas");
            EditorApplication.Exit(codigo);
        }
    }
}
