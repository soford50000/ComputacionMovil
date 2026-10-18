using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>Smoke test en Play Mode con capturas y restauración de las preferencias del jugador.</summary>
[InitializeOnLoad]
public static class ValidarMenusExpedicion
{
    private const string Sesion = "Expedicion.QA";
    [Serializable] private class Preferencia { public string clave; public bool existe; public int valor; }
    [Serializable] private class Respaldo { public List<Preferencia> valores = new List<Preferencia>(); }
    private static double limite, siguiente;
    private static int paso, codigo;
    private static bool terminando;
    private static TensionSystem pesca;
    private static CapturaResultado captura;
    private static int saldoAntes;
    private static string errorRegistrado;
    private static int saldoAntesDeViajar;

    static ValidarMenusExpedicion()
    {
        if (SessionState.GetBool(Sesion, false)) Registrar();
    }

    public static void Ejecutar()
    {
        if (!Application.isBatchMode) throw new InvalidOperationException("Ejecutar en batchmode con el Editor del proyecto cerrado.");
        var respaldo = new Respaldo();
        var claves = new List<string> { "RoadToAmazonas.SaldoOro.v1" };
        claves.AddRange(Resources.Load<CatalogoCebos>("CatalogoCebos").cebos.Select(c => "RoadToAmazonas.CebosComprados.v1." + c.name));
        claves.AddRange(Resources.Load<CatalogoCanas>("CatalogoCanas").canas.Select(c => "RoadToAmazonas.CanasCompradas.v1." + c.name));
        foreach (var guid in AssetDatabase.FindAssets("t:PezData"))
        {
            var pez = AssetDatabase.LoadAssetAtPath<PezData>(AssetDatabase.GUIDToAssetPath(guid));
            claves.Add("RoadToAmazonas.Catalogo.v1." + Uri.EscapeDataString(pez.name));
        }
        foreach (string clave in claves.Distinct())
            respaldo.valores.Add(new Preferencia { clave = clave, existe = PlayerPrefs.HasKey(clave), valor = PlayerPrefs.GetInt(clave) });
        SessionState.SetString(Sesion + ".respaldo", JsonUtility.ToJson(respaldo));
        Directory.CreateDirectory("Logs/ExpedicionQA");
        File.WriteAllText("Logs/ExpedicionQA/preferencias-respaldo.json", JsonUtility.ToJson(respaldo, true));
        foreach (string clave in claves) PlayerPrefs.DeleteKey(clave);
        PlayerPrefs.SetInt("RoadToAmazonas.SaldoOro.v1", 100000);
        PlayerPrefs.Save();
        SessionState.SetBool(Sesion, true);
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        Registrar();
        EditorApplication.EnterPlaymode();
    }

    private static void Registrar()
    {
        limite = EditorApplication.timeSinceStartup + 150;
        EditorApplication.update -= Verificar;
        EditorApplication.update += Verificar;
        EditorApplication.playModeStateChanged -= CambioModo;
        EditorApplication.playModeStateChanged += CambioModo;
        Application.logMessageReceived -= RegistrarError;
        Application.logMessageReceived += RegistrarError;
    }

    private static void Verificar()
    {
        if (terminando) return;
        try
        {
            if (!string.IsNullOrEmpty(errorRegistrado)) throw new Exception("Error de ejecución: " + errorRegistrado);
            if (EditorApplication.timeSinceStartup > limite) throw new Exception("Se agotó el tiempo de validación.");
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < siguiente) return;
            pesca = UnityEngine.Object.FindAnyObjectByType<TensionSystem>();
            if (pesca == null || EconomiaManager.Instance == null) return;
            var cebos = Resources.Load<CatalogoCebos>("CatalogoCebos").cebos;
            var menu = pesca.GetComponent<CeboSelectorUI>();
            var resultado = pesca.GetComponent<CapturaResultUI>();
            var pez = FisheryManager.Instance.ZonaInicial.especiesDeEstaZona[0];
            switch (paso++)
            {
                case 0:
                    var fuente = Resources.Load<Font>("Fonts/Rye-Regular");
                    Exigir(fuente != null, "Falta la fuente Rye.");
                    Exigir("áéíóúüñÁÉÍÓÚÜÑ¿¡0123456789×$".All(fuente.HasCharacter), "Rye no cubre los caracteres de la interfaz.");
                    menu.Abrir();
                    break;
                case 1:
                    var puntero = new PointerEventData(EventSystem.current) { position = new Vector2(40, Screen.height - 40) };
                    var impactos = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(puntero, impactos);
                    Exigir(impactos.Count > 0 && impactos[0].gameObject.GetComponentInParent<EntradaExpedicion>() != null,
                        "Un acceso del HUD sigue por encima del modal.");
                    Capturar("01-carnadas", 1280, 720);
                    Pulsar("Carnada " + cebos.OrderByDescending(c => c.especiesFavorecidas.Length).First().nombre);
                    var lista = pesca.GetComponentsInChildren<ScrollRect>().Single(s => s.name == "Lista de especies favorecidas");
                    Canvas.ForceUpdateCanvases();
                    lista.verticalNormalizedPosition = 0;
                    Canvas.ForceUpdateCanvases();
                    Exigir(lista.content.anchoredPosition.y > 0, "La lista larga de especies no se puede desplazar.");
                    lista.verticalNormalizedPosition = 1;
                    Capturar("02-carnadas-lista-larga", 1280, 720);
                    Capturar("03-carnadas-vertical", 720, 1280);
                    Pulsar("Carnada " + cebos[0].nombre);
                    saldoAntes = EconomiaManager.Instance.Saldo;
                    Pulsar("Equipar carnada");
                    Exigir(pesca.CeboEquipado == cebos[0] && !menu.EstaAbierto, "No equipa/cierra la carnada.");
                    Exigir(EconomiaManager.Instance.Saldo == saldoAntes - cebos[0].precio, "Compra de carnada incorrecta.");
                    menu.Abrir();
                    Pulsar("Sin carnada");
                    Pulsar("Equipar carnada");
                    Exigir(pesca.CeboEquipado == null, "No retira la carnada.");
                    Pulsar("Abrir equipo");
                    break;
                case 2:
                    Capturar("04-canas", 1280, 720);
                    Capturar("05-canas-vertical", 720, 1280);
                    var cana = Resources.Load<CatalogoCanas>("CatalogoCanas").canas.Last();
                    Pulsar("Opción " + cana.nombreCana);
                    Exigir(pesca.CanaEquipada == cana, "No equipa la caña.");
                    Pulsar("Abrir catálogo");
                    break;
                case 3:
                    Capturar("06-coleccion", 1280, 720);
                    Capturar("07-coleccion-vertical", 720, 1280);
                    var coleccion = pesca.GetComponentsInChildren<ScrollRect>().Single(s => s.name == "Desplazamiento catálogo");
                    Canvas.ForceUpdateCanvases();
                    coleccion.verticalNormalizedPosition = 1;
                    coleccion.OnScroll(new PointerEventData(EventSystem.current) { scrollDelta = new Vector2(0, -4) });
                    Exigir(coleccion.content.anchoredPosition.y > 0, "La decoración bloquea el desplazamiento del catálogo.");
                    coleccion.verticalNormalizedPosition = 0;
                    Capturar("13-coleccion-final", 1280, 720);
                    Capturar("14-coleccion-final-vertical", 720, 1280);
                    coleccion.verticalNormalizedPosition = 1;
                    Pulsar("Cerrar");
                    Exigir(resultado.ResolverIntentoCaptura(pez, pesca.CanaEquipada, 1, out _), "La captura válida falló.");
                    captura = resultado.ResultadoActual;
                    Exigir(captura.NuevaEspecie, "No se marca el primer descubrimiento.");
                    saldoAntes = EconomiaManager.Instance.Saldo;
                    break;
                case 4:
                    Capturar("08-captura", 1280, 720);
                    Capturar("09-captura-vertical", 720, 1280);
                    Pulsar("Boton", "Recoger");
                    Exigir(resultado.ResultadoActual == null, "No recoge la captura.");
                    Exigir(EconomiaManager.Instance.Saldo == saldoAntes + captura.Oro, "Recompensa incorrecta.");
                    resultado.Mostrar(captura);
                    Exigir(resultado.ResultadoActual == null, "Se puede reclamar una captura dos veces.");
                    Exigir(resultado.ResolverIntentoCaptura(pez, pesca.CanaEquipada, 1, out _), "Falló la captura repetida.");
                    Exigir(!resultado.ResultadoActual.NuevaEspecie, "Una captura repetida muestra descubrimiento.");
                    break;
                case 5:
                    Pulsar("Boton", "Recoger");
                    Pulsar("Abrir catálogo");
                    break;
                case 6:
                    Capturar("15-coleccion-descubierta", 1280, 720);
                    Pulsar("Especie " + pez.name);
                    break;
                case 7:
                    Capturar("10-ficha", 1280, 720);
                    Capturar("11-ficha-vertical", 720, 1280);
                    Pulsar("Volver a cuadrícula");
                    Pulsar("Cerrar");
                    menu.Abrir();
                    menu.Cerrar();
                    menu.Abrir();
                    break;
                case 8:
                    var grupo = pesca.GetComponentsInChildren<EntradaExpedicion>().Single().GetComponent<CanvasGroup>();
                    Exigir(grupo.alpha > .99f, "El fundido no finaliza al reabrir.");
                    menu.Cerrar();
                    Capturar("12-hud", 1280, 720);
                    saldoAntesDeViajar = EconomiaManager.Instance.Saldo;
                    pesca.GetComponent<ZonaSelectorUI>().Abrir();
                    break;
                case 9:
                    Capturar("16-mapa", 1280, 720);
                    Capturar("17-mapa-vertical", 720, 1280);
                    Pulsar("Destino San Andrés");
                    Exigir(FisheryManager.Instance.ZonaActual == FisheryManager.Instance.ZonaSanAndres,
                        "El mapa no viaja a San Andrés.");
                    var pendientes = FisheryManager.Instance.ZonaSanAndres.especiesDeEstaZona;
                    Exigir(pendientes.Length == 15 && pendientes.Distinct().Count() == 15 &&
                        pendientes.All(p => p != null && p.foto != null && p.caracteristicasPendientes &&
                            !FisheryManager.Instance.ZonaInicial.especiesDeEstaZona.Contains(p)),
                        "San Andrés no tiene las quince especies pendientes con imágenes propias.");
                    Exigir(!FisheryManager.Instance.TieneEspecies && FisheryManager.Instance.GenerarPezAleatorio() == null,
                        "San Andrés permite capturar especies pendientes.");
                    pesca.IniciarLanzamiento();
                    Exigir(pesca.PuedeCambiarCana, "La zona sin especies permite iniciar una captura.");
                    break;
                case 10:
                    Capturar("18-san-andres", 1280, 720);
                    Capturar("19-san-andres-vertical", 720, 1280);
                    var agua = pesca.GetComponentsInChildren<RawImage>().Single(r => r.name == "Agua animada");
                    Exigir(agua.texture == FisheryManager.Instance.ZonaActual.fondoZona.texture,
                        "El fondo no corresponde a San Andrés.");
                    Exigir(agua.material.shader.name == "Pesca/MarCaribe" && agua.material.shader.isSupported,
                        "San Andrés no usa su shader marino.");
                    Exigir(Mathf.Approximately(agua.material.GetFloat("_Intensity"), 2.6f) &&
                        Mathf.Approximately(agua.material.GetFloat("_Waterline"), .478f) &&
                        Mathf.Approximately(agua.material.GetFloat("_GlintStrength"), .188f),
                        "San Andrés no aplica los valores de agua solicitados.");
                    var ajustesAgua = new SerializedObject(pesca.GetComponent<FondoAguaUI>());
                    Exigir(Mathf.Approximately(ajustesAgua.FindProperty("velocidadMar").floatValue, 1.52f) &&
                        Mathf.Approximately(ajustesAgua.FindProperty("velocidad").floatValue, 1.03f),
                        "Las velocidades de agua no están guardadas en la escena.");
                    Pulsar("Abrir catálogo");
                    break;
                case 11:
                    Capturar("20-coleccion-san-andres", 1280, 720);
                    Exigir(pesca.GetComponentsInChildren<Text>().Any(t => t.text == "Peces de San Andrés"),
                        "El catálogo conserva el nombre de Amazonas.");
                    Exigir(pesca.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("Especie ")) == 15,
                        "El catálogo de San Andrés no muestra sus quince especies.");
                    Pulsar("Cerrar");
                    pesca.GetComponent<ZonaSelectorUI>().Abrir();
                    Pulsar("Destino Amazonas");
                    Exigir(FisheryManager.Instance.ZonaActual == FisheryManager.Instance.ZonaInicial &&
                        FisheryManager.Instance.TieneEspecies, "No restaura la pesca de Amazonas.");
                    Exigir(pesca.GetComponentsInChildren<RawImage>().Single(r => r.name == "Agua animada")
                        .material.shader.name == "Pesca/AguaTranquila", "Amazonas no restaura su shader de agua.");
                    var aguaAmazonas = pesca.GetComponentsInChildren<RawImage>().Single(r => r.name == "Agua animada");
                    Exigir(Mathf.Approximately(aguaAmazonas.material.GetFloat("_Intensity"), 3.51f) &&
                        Mathf.Approximately(aguaAmazonas.material.GetFloat("_Waterline"), .433f),
                        "Amazonas no recupera sus ajustes de agua al volver.");
                    Exigir(EconomiaManager.Instance.Saldo == saldoAntesDeViajar, "Viajar cambia el saldo.");
                    var dinero = pesca.GetComponentsInChildren<Text>().Single(t => t.name == "Cantidad de oro");
                    Exigir(dinero.fontSize == 22 && !dinero.resizeTextForBestFit, "El saldo reduce la tipografía.");
                    pesca.IniciarLanzamiento();
                    Exigir(!FisheryManager.Instance.CambiarZona(FisheryManager.Instance.ZonaSanAndres),
                        "Se puede cambiar de zona mientras se espera el pique.");
                    break;
                case 12:
                    Exigir(!pesca.GetComponentsInChildren<Button>().Any(b => b.name == "Abrir mapa" ||
                        b.name == "Saldo de oro" || b.name == "Abrir equipo" || b.name == "Abrir carnadas"),
                        "El HUD de preparación sigue visible durante la pesca.");
                    pesca.NotificarPique();
                    Exigir(!FisheryManager.Instance.CambiarZona(FisheryManager.Instance.ZonaSanAndres),
                        "Se puede viajar durante el forcejeo.");
                    Debug.Log("ZONAS QA OK: mapa, viaje ida y vuelta, fondo correcto, quince especies pendientes con imágenes, captura pendiente bloqueada, saldo y tipografía conservados, bloqueo de viaje durante pesca.");
                    Debug.Log("EXPEDICION QA OK: compra/equipado de carnada y caña, retirada, catálogo con vegetación y scroll, ficha, primer descubrimiento, captura repetida, recompensa única, reapertura y capturas horizontales y verticales.");
                    Terminar(0);
                    break;
            }
            siguiente = EditorApplication.timeSinceStartup + .65;
        }
        catch (Exception e) { Debug.LogException(e); Terminar(1); }
    }

    private static void Pulsar(string nombre, string padre = null)
    {
        var boton = pesca.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == nombre && (padre == null || b.transform.parent.name == padre));
        Exigir(boton != null && boton.interactable, "Botón no disponible: " + nombre + " / " + padre);
        boton.onClick.Invoke();
    }

    private static void Capturar(string nombre, int ancho, int alto)
    {
        var fuente = Resources.Load<Font>("Fonts/Rye-Regular");
        var textos = UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
        Exigir(textos.Length > 0 && textos.All(t => t.font == fuente), "Hay textos de la interfaz que no usan Rye.");
        var camara = Camera.main;
        var destino = new RenderTexture(ancho, alto, 24);
        var anterior = RenderTexture.active;
        var texturaAnterior = camara.targetTexture;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>().Where(c => c.isRootCanvas).ToArray();
        var modos = canvases.Select(c => c.renderMode).ToArray();
        var camaras = canvases.Select(c => c.worldCamera).ToArray();
        var distancias = canvases.Select(c => c.planeDistance).ToArray();
        Texture2D imagen = null;
        try
        {
            camara.targetTexture = destino;
            foreach (var canvas in canvases)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camara;
                canvas.planeDistance = 10;
            }
            // Unity 6 actualiza CanvasScaler en preWillRenderCanvases. Una segunda
            // pasada estabiliza las anclas adaptables después del cambio de resolución.
            Canvas.ForceUpdateCanvases();
            foreach (var canvas in canvases) ((RectTransform)canvas.transform).ForceUpdateRectTransforms();
            Canvas.ForceUpdateCanvases();
            if (nombre.Contains("coleccion"))
            {
                var coleccion = pesca.GetComponentsInChildren<ScrollRect>().Single(s => s.name == "Desplazamiento catálogo");
                // Aplicar el extremo después de recalcular columnas y altura para esta resolución.
                coleccion.verticalNormalizedPosition = nombre.Contains("final") ? 0 : 1;
                Canvas.ForceUpdateCanvases();
                VerificarContrasteCatalogo(coleccion);
                var ramas = coleccion.transform.parent.GetComponentsInChildren<RawImage>();
                Exigir(ramas.Length > 0 && ramas.All(r => r.texture != null), "Faltan imágenes de la vegetación.");
                Exigir(ramas.Select(r => r.texture).Distinct().Count() == 4, "El catálogo no alterna las cuatro ramas.");
                var esquinas = new Vector3[4];
                coleccion.viewport.GetWorldCorners(esquinas);
                for (int x = 1; x <= 3; x++)
                for (int y = 1; y <= 3; y++)
                {
                    Vector3 punto = esquinas[0] + (esquinas[3] - esquinas[0]) * (x / 4f) + (esquinas[1] - esquinas[0]) * (y / 4f);
                    var puntero = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(camara, punto) };
                    var impactos = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(puntero, impactos);
                    Exigir(impactos.Count > 0 && impactos.All(r => r.gameObject.GetComponent<RawImage>() == null), "Una rama intercepta los clics o el scroll.");
                }
            }
            camara.Render();
            RenderTexture.active = destino;
            imagen = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            imagen.Apply();
            File.WriteAllBytes("Logs/ExpedicionQA/" + nombre + ".png", imagen.EncodeToPNG());
        }
        finally
        {
            for (int i = 0; i < canvases.Length; i++)
            {
                canvases[i].renderMode = modos[i]; canvases[i].worldCamera = camaras[i]; canvases[i].planeDistance = distancias[i];
            }
            camara.targetTexture = texturaAnterior;
            RenderTexture.active = anterior;
            Canvas.ForceUpdateCanvases();
            if (imagen != null) UnityEngine.Object.DestroyImmediate(imagen);
            destino.Release(); UnityEngine.Object.DestroyImmediate(destino);
        }
    }

    private static void VerificarContrasteCatalogo(ScrollRect coleccion)
    {
        var panel = coleccion.transform.parent;
        var madera = panel.GetComponent<Image>().sprite.texture;
        float fondoMasOscuro = madera.GetPixels32().Min(c => Luminancia(c));
        float minimo = float.MaxValue;
        foreach (var texto in panel.GetComponentsInChildren<Text>())
        {
            var boton = texto.GetComponentInParent<Button>();
            if (boton == null)
            {
                minimo = Mathf.Min(minimo, Contraste(texto.color, fondoMasOscuro));
                continue;
            }
            var imagenFondo = boton.targetGraphic as Image;
            var estados = boton.interactable
                ? new[] { boton.colors.normalColor, boton.colors.highlightedColor, boton.colors.selectedColor, boton.colors.pressedColor }
                : new[] { boton.colors.disabledColor };
            foreach (var estado in estados)
            {
                // La X de cierre es texto sobre el panel, sin una imagen de fondo.
                float contraste = imagenFondo != null
                    ? Contraste(texto.color, Luminancia(imagenFondo.color * estado))
                    : Contraste(texto.color * estado, fondoMasOscuro);
                minimo = Mathf.Min(minimo, contraste);
            }
        }
        Exigir(minimo >= 4.5f, "Contraste insuficiente en el catálogo: " + minimo.ToString("F2") + ":1");
        Debug.Log("NOGAL QA: contraste mínimo de textos " + minimo.ToString("F2") + ":1, incluida madera con vetas/sombra y estados de botones.");
    }

    private static float Luminancia(Color color)
    {
        return .2126f * Lineal(color.r) + .7152f * Lineal(color.g) + .0722f * Lineal(color.b);
    }
    private static float Lineal(float valor)
    {
        valor = Mathf.Clamp01(valor);
        return valor <= .04045f ? valor / 12.92f : Mathf.Pow((valor + .055f) / 1.055f, 2.4f);
    }
    private static float Contraste(Color texto, float fondo)
    {
        float tinta = Luminancia(texto);
        return (Mathf.Max(tinta, fondo) + .05f) / (Mathf.Min(tinta, fondo) + .05f);
    }

    private static void Exigir(bool valor, string mensaje) { if (!valor) throw new Exception(mensaje); }
    private static void RegistrarError(string mensaje, string pila, LogType tipo)
    {
        if (tipo == LogType.Error || tipo == LogType.Exception || tipo == LogType.Assert) errorRegistrado = mensaje;
    }
    private static void Terminar(int resultado)
    {
        codigo = resultado;
        SessionState.SetInt(Sesion + ".codigo", resultado);
        terminando = true;
        EditorApplication.update -= Verificar;
        EditorApplication.ExitPlaymode();
    }

    private static void CambioModo(PlayModeStateChange estado)
    {
        if (estado != PlayModeStateChange.EnteredEditMode) return;
        var respaldo = JsonUtility.FromJson<Respaldo>(SessionState.GetString(Sesion + ".respaldo", ""));
        foreach (var dato in respaldo.valores)
        {
            if (dato.existe) PlayerPrefs.SetInt(dato.clave, dato.valor);
            else PlayerPrefs.DeleteKey(dato.clave);
        }
        PlayerPrefs.Save();
        SessionState.SetBool(Sesion, false);
        Debug.Log("EXPEDICION QA: preferencias originales restauradas.");
        EditorApplication.Exit(SessionState.GetInt(Sesion + ".codigo", 1));
    }
}
