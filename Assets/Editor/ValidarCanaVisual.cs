using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Prueba de integración opcional: -batchmode -executeMethod ValidarCanaVisual.Ejecutar.</summary>
[InitializeOnLoad]
public static class ValidarCanaVisual
{
    private const string Clave = "Pesca.ValidarCana";
    private static int paso;
    private static double siguiente, limite;
    private static float cargaMinima, cargaMaxima;
    private static Vector2 baseMuestra, puntaMuestra;
    private static bool hayMuestra;
    private static float recorridoPunta;

    static ValidarCanaVisual()
    {
        if (SessionState.GetBool(Clave, false))
        {
            limite = EditorApplication.timeSinceStartup + 60;
            EditorApplication.update += Actualizar;
        }
    }

    public static void Ejecutar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        SessionState.SetBool(Clave, true);
        paso = -6;
        hayMuestra = false;
        recorridoPunta = 0f;
        cargaMinima = float.PositiveInfinity;
        cargaMaxima = float.NegativeInfinity;
        siguiente = 0;
        limite = EditorApplication.timeSinceStartup + 60;
        EditorApplication.update -= Actualizar;
        EditorApplication.update += Actualizar;
        EditorApplication.EnterPlaymode();
    }

    private static void Actualizar()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > limite) throw new Exception("Tiempo de validación agotado.");
            if (!EditorApplication.isPlaying) return;
            var visual = UnityEngine.Object.FindAnyObjectByType<CanaVisual>();
            var pesca = UnityEngine.Object.FindAnyObjectByType<TensionSystem>();
            if (visual == null || pesca == null) return;
            if (paso == 3 && EditorApplication.timeSinceStartup > siguiente - 0.5)
            {
                cargaMinima = Mathf.Min(cargaMinima, visual.TensionVisual);
                cargaMaxima = Mathf.Max(cargaMaxima, visual.TensionVisual);
                var modelo = UnityEngine.Object.FindAnyObjectByType<CanaGrafico>();
                if (!hayMuestra)
                {
                    baseMuestra = modelo.Punto(0.6f);
                    puntaMuestra = modelo.Punto(1f);
                    hayMuestra = true;
                }
                Exigir(Vector2.Distance(baseMuestra, modelo.Punto(0.6f)) < 0.01f, "La parte baja se mueve durante los tirones.");
                recorridoPunta = Mathf.Max(recorridoPunta, Vector2.Distance(puntaMuestra, modelo.Punto(1f)));
                Exigir(recorridoPunta < modelo.rectTransform.rect.height * 0.008f, "La punta oscila demasiado.");
            }
            if (EditorApplication.timeSinceStartup < siguiente) return;
            if (paso == -6)
            {
                var daiwa = AssetDatabase.LoadAssetAtPath<CanaData>("Assets/Data/Canas/Cana_DaiwaDSK25BF662M.asset");
                Exigir(daiwa != null && daiwa.puestoPotenciaPrevisto == 3 && daiwa.estadisticasPendientes,
                    "Falta la Daiwa o su posición como tercera más potente.");
                PulsarBoton(pesca, "Abrir equipo");
                PulsarBoton(pesca, "Opción Daiwa DSK25-B/F662M");
                Exigir(pesca.CanaEquipada == daiwa, "El selector no equipa Daiwa.");
            }
            else if (paso == -5)
            {
                var modelo = UnityEngine.Object.FindAnyObjectByType<CanaGrafico>();
                Exigir(modelo.ModeloActual == ModeloCana.DaiwaDSK25BF662M, "No se muestra el modelo Daiwa.");
                CompararFisicas(modelo);
                Capturar("daiwa-reposo");
                modelo.Configurar(new Vector2(0.74f, -0.12f), new Vector2(0.5f, 0.81f), 1f, 0.32f, 0.8f, -0.002f, 1f);
                Capturar("daiwa-flexion");
            }
            else if (paso == -4)
            {
                var fx = AssetDatabase.LoadAssetAtPath<CanaData>("Assets/Data/Canas/Cana_ShimanoFx.asset");
                Exigir(fx != null && fx.modeloVisual == ModeloCana.ShimanoFx && fx.estadisticasPendientes,
                    "Falta la identidad Shimano Fx.");
                PulsarBoton(pesca, "Abrir equipo");
                PulsarBoton(pesca, "Opción Shimano Fx");
                Exigir(pesca.CanaEquipada == fx, "El selector no equipa Shimano Fx.");
            }
            else if (paso == -3)
            {
                var modelo = UnityEngine.Object.FindAnyObjectByType<CanaGrafico>();
                Exigir(modelo.ModeloActual == ModeloCana.ShimanoFx, "No se muestra el modelo Fx.");
                CompararFisicas(modelo);
                Capturar("fx-reposo");
                modelo.Configurar(new Vector2(0.74f, -0.12f), new Vector2(0.5f, 0.81f), 1f, 0.32f, 0.8f, -0.002f, 1f);
                Capturar("fx-flexion");
                PulsarBoton(pesca, "Abrir equipo");
                PulsarBoton(pesca, "Opción Abu Garcia Vengeance");
            }
            else if (paso == -2)
            {
                var shimano = AssetDatabase.LoadAssetAtPath<CanaData>("Assets/Data/Canas/Cana_ShimanoColtSniperLimited100H.asset");
                Exigir(shimano != null && shimano.puestoPotenciaPrevisto == 1 && shimano.estadisticasPendientes,
                    "Falta la identidad o la posición prevista de Shimano.");
                Capturar("abu-reposo");
                PulsarBoton(pesca, "Abrir equipo");
                int opciones = 0;
                foreach (var boton in pesca.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                {
                    if (!boton.name.StartsWith("Opción ")) continue;
                    opciones++;
                    Exigir(boton.name == "Opción Daiwa DSK25-B/F662M" || boton.name == "Opción Shimano Fx" || boton.name == "Opción Abu Garcia Vengeance" || boton.name == "Opción " + shimano.nombreCana,
                        "El equipo contiene una caña retirada.");
                }
                Exigir(opciones == 4, "Equipamiento debe mostrar Fx, Daiwa, Abu Garcia y Colt Sniper.");
                Capturar("selector");
                PulsarBoton(pesca, "Opción " + shimano.nombreCana);
                Exigir(pesca.CanaEquipada == shimano, "El selector no equipa Shimano en reposo.");
            }
            else if (paso == -1)
            {
                var modelo = UnityEngine.Object.FindAnyObjectByType<CanaGrafico>();
                Exigir(modelo.ModeloActual == ModeloCana.ShimanoColtSniperLimited100H,
                    "Equipar Shimano no cambia el modelo visible.");
                bool nombreVisible = false;
                foreach (var texto in pesca.GetComponentsInChildren<UnityEngine.UI.Text>())
                    if (texto.name == "Caña equipada" && texto.text.Contains(pesca.CanaEquipada.nombreCana)) nombreVisible = true;
                Exigir(nombreVisible, "El HUD no muestra el nombre de Shimano.");
                CompararFisicas(modelo);
                Capturar("shimano-reposo");
            }
            else if (paso == 0)
            {
                Exigir(visual.TensionVisual == 0f, "La caña debe empezar recta.");
                Capturar("reposo");
                pesca.IniciarLanzamiento();
                Exigir(!pesca.EnForcejeo, "La espera no debe activar el forcejeo.");
                pesca.NotificarPique();
                Exigir(pesca.EnForcejeo, "El pique no activó el forcejeo.");
                visual.EstablecerTension(1f);
            }
            else if (paso == 1)
            {
                Exigir(visual.TensionVisual > 0.9f, "No se alcanzó la flexión bajo carga.");
                Capturar("tension");
                visual.EstablecerTension(0f);
            }
            else if (paso == 2)
            {
                Exigir(Mathf.Abs(visual.TensionVisual) < 0.02f, "No se relajó al retirar la carga.");
                visual.UsarTironesSimulados();
            }
            else if (paso == 3)
            {
                Exigir(cargaMinima > 0.65f, "La caña pierde la carga sostenida durante la pelea.");
                Exigir(cargaMaxima - cargaMinima > 0.002f, "No hay variaciones de carga en el combate.");
                Exigir(recorridoPunta > 0.01f, "La punta no presenta microsacudidas.");
                Capturar("combate");
                pesca.SetZonaActiva(false);
            }
            else
            {
                Exigir(Mathf.Abs(visual.TensionVisual) < 0.02f, "No volvió al reposo al terminar.");
                Capturar("recuperada");
                Debug.Log("CANA QA OK: selección de las cuatro cañas, incluida Daiwa; físicas compartidas, pose estable, microsacudidas y recuperación. Capturas en Logs/CanaQA.");
                Terminar(0);
                return;
            }
            paso++;
            siguiente = EditorApplication.timeSinceStartup + 1.3;
        }
        catch (Exception e) { Debug.LogException(e); Terminar(1); }
    }

    private static void PulsarBoton(TensionSystem pesca, string nombre)
    {
        foreach (var boton in pesca.GetComponentsInChildren<UnityEngine.UI.Button>(true))
        {
            if (boton.name != nombre) continue;
            boton.onClick.Invoke();
            return;
        }
        throw new Exception("No se encontró el botón: " + nombre);
    }

    private static void CompararFisicas(CanaGrafico modelo)
    {
        // Misma curva con carga y vibración para ambas apariencias.
        var referencia = new Vector2[97];
        ModeloCana original = modelo.ModeloActual;
        modelo.Configurar(new Vector2(0.74f, -0.12f), new Vector2(0.5f, 0.81f), 1f, 0.32f, 0.8f, -0.002f);
        modelo.EstablecerModelo(ModeloCana.AbuGarciaVengeance);
        for (int i = 0; i <= 96; i++) referencia[i] = modelo.Punto(i / 96f);
        foreach (ModeloCana variante in Enum.GetValues(typeof(ModeloCana)))
        {
            modelo.EstablecerModelo(variante);
            for (int i = 0; i <= 96; i++)
                Exigir(Vector2.Distance(referencia[i], modelo.Punto(i / 96f)) < 0.0001f, "Las físicas varían al cambiar de modelo.");
        }
        modelo.EstablecerModelo(original);
        modelo.Configurar(new Vector2(0.74f, -0.12f), new Vector2(0.5f, 0.81f), 1f, 0.32f, 0f);
    }

    private static void Capturar(string nombre)
    {
        Camera camara = Camera.main;
        Exigir(camara != null, "Falta cámara principal.");
        var rt = new RenderTexture(1280, 720, 24);
        RenderTexture anterior = RenderTexture.active;
        RenderTexture destinoAnterior = camara.targetTexture;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>();
        var modos = new RenderMode[canvases.Length];
        var camaras = new Camera[canvases.Length];
        var distancias = new float[canvases.Length];
        Texture2D imagen = null;
        try
        {
            camara.targetTexture = rt;
            for (int i = 0; i < canvases.Length; i++)
            {
                modos[i] = canvases[i].renderMode;
                camaras[i] = canvases[i].worldCamera;
                distancias[i] = canvases[i].planeDistance;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = camara;
                canvases[i].planeDistance = 10;
            }
            Canvas.ForceUpdateCanvases();
            var grafico = UnityEngine.Object.FindAnyObjectByType<CanaGrafico>();
            Exigir(!grafico.raycastTarget, "La caña bloquea los controles.");
            if (nombre == "tension")
            {
                Rect r = grafico.rectTransform.rect;
                Exigir(grafico.Punto(1).y < grafico.Punto(0.95f).y, "La punta no se curva hacia abajo.");
                Exigir(grafico.SalidaSedal.y < grafico.Punto(1).y, "La anilla terminal no está debajo de la caña.");
                Exigir(grafico.EntradaAgua.x < grafico.SalidaSedal.x &&
                    grafico.EntradaAgua.y < grafico.SalidaSedal.y, "El sedal no cae hacia abajo y a la izquierda.");
                Exigir(grafico.Punto(1).x < grafico.Punto(0.265f).x &&
                    grafico.Punto(1).x < grafico.Punto(0.95f).x, "El arco no apunta hacia la izquierda.");
                float longitud = 0;
                for (int i = 1; i <= 96; i++)
                    longitud += Vector2.Distance(grafico.Punto((i - 1f) / 96), grafico.Punto(i / 96f));
                float recta = Vector2.Scale(r.size, new Vector2(-0.24f, 0.93f)).magnitude;
                Exigir(Mathf.Abs(longitud / recta - 1f) < 0.015f, "La caña se estira al flexionarse.");
            }
            var malla = grafico.canvasRenderer.GetMesh();
            Exigir(malla != null, "La caña no generó una malla.");
            Exigir(malla.vertexCount > 100 && malla.vertexCount < 65000, "Malla ausente o demasiado grande.");
            foreach (Vector3 v in malla.vertices)
                Exigir(!float.IsNaN(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.x) && !float.IsInfinity(v.y), "Vértice inválido.");
            camara.Render();
            RenderTexture.active = rt;
            imagen = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            imagen.Apply();
            Directory.CreateDirectory("Logs/CanaQA");
            File.WriteAllBytes("Logs/CanaQA/" + nombre + ".png", imagen.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = modos[i];
                canvases[i].worldCamera = camaras[i];
                canvases[i].planeDistance = distancias[i];
            }
            camara.targetTexture = destinoAnterior;
            RenderTexture.active = anterior;
            if (imagen != null) UnityEngine.Object.DestroyImmediate(imagen);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }

    private static void Exigir(bool condicion, string mensaje) { if (!condicion) throw new Exception(mensaje); }
    private static void Terminar(int codigo)
    {
        SessionState.SetBool(Clave, false);
        EditorApplication.update -= Actualizar;
        EditorApplication.Exit(codigo);
    }
}
