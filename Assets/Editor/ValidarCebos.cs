using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Integración reproducible: -batchmode -executeMethod ValidarCebos.Ejecutar.</summary>
[InitializeOnLoad]
public static class ValidarCebos
{
    private const string Clave = "Pesca.ValidarCebos";
    private static double limite;
    static ValidarCebos()
    {
        if (SessionState.GetBool(Clave, false)) Registrar();
    }

    public static void Ejecutar()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        SessionState.SetBool(Clave, true);
        Registrar();
        EditorApplication.EnterPlaymode();
    }

    public static void EjecutarSenuelos()
    {
        SessionState.SetBool("Pesca.ValidarSenuelos", true);
        Ejecutar();
    }

    private static void ProbarSenuelos(TensionSystem pesca)
    {
        var gestor = FisheryManager.Instance;
        var zona = gestor.ZonaActual;
        var menu = pesca.GetComponent<CeboSelectorUI>();
        var senuelos = Resources.Load<CatalogoCebos>("CatalogoSenuelos").cebos;
        string clave = "RoadToAmazonas.SenueloEquipado.v1";
        string anterior = PlayerPrefs.GetString(clave, "");
        bool existia = PlayerPrefs.HasKey(clave);
        string compra = "RoadToAmazonas.CebosUsos.v2." + senuelos[1].name;
        int propiedadAnterior = PlayerPrefs.GetInt(compra, -1);
        var economia = EconomiaManager.Instance;
        int saldoAnterior = economia.Saldo;
        try
        {
            gestor.CambiarZona(gestor.ZonaInicial);
            var carnada = pesca.CeboEquipado;
            PlayerPrefs.DeleteKey(clave);
            Exigir(gestor.CambiarZona(gestor.ZonaSanAndres), "No se pudo viajar al mar.");
            Exigir(senuelos.Length == 3 && pesca.CeboEquipado == senuelos[0], "Falta el señuelo inicial.");
            menu.Abrir();
            float anteriorFactor = 1f;
            foreach (var s in senuelos)
            {
                Pulsar(pesca, "Señuelo " + s.nombre);
                Exigir(s.esSenuelo && s.icono != null && s.multiplicadorAparicion > anteriorFactor &&
                    s.especiesFavorecidas.Length == 3 && s.especiesFavorecidas.All(p =>
                        gestor.ZonaActual.especiesDeEstaZona.Contains(p) && s.MultiplicadorPara(p) == s.multiplicadorAparicion),
                    "Datos incorrectos para " + s.name);
                anteriorFactor = s.multiplicadorAparicion;
            }
            menu.Cerrar();
            Exigir(!pesca.EquiparCebo(Resources.Load<CatalogoCebos>("CatalogoCebos").cebos[0]), "Se admitió carnada en el mar.");
            pesca.IniciarLanzamiento();
            Exigir(!pesca.PuedeCambiarCebo && pesca.CeboEquipado == senuelos[0] && menu.TieneUsos(senuelos[0]), "El lanzamiento perdió el señuelo.");
            pesca.SendMessage("VolverAListo");
            PlayerPrefs.DeleteKey(compra);
            if (economia.Saldo < senuelos[1].precio)
                economia.AgregarOro(senuelos[1].precio - economia.Saldo);
            int saldoCompra = economia.Saldo;
            menu.Abrir();
            Pulsar(pesca, "Señuelo " + senuelos[1].nombre);
            Pulsar(pesca, "Equipar carnada");
            Exigir(pesca.CeboEquipado == senuelos[1] && economia.Saldo == saldoCompra - senuelos[1].precio,
                "La compra del Mepps no lo equipó al precio previsto.");
            pesca.IniciarLanzamiento();
            Exigir(!pesca.PuedeCambiarCebo && pesca.CeboEquipado == senuelos[1] && menu.TieneUsos(senuelos[1]),
                "El señuelo comprado se agotó al lanzar.");
            pesca.SendMessage("VolverAListo");
            Exigir(gestor.CambiarZona(gestor.ZonaInicial) && pesca.CeboEquipado == carnada &&
                !pesca.EquiparCebo(senuelos[0]), "La vuelta al río no restauró la carnada.");
            Exigir(gestor.CambiarZona(gestor.ZonaSanAndres) && pesca.CeboEquipado == senuelos[1],
                "El regreso a San Andrés perdió el señuelo comprado.");
            Debug.Log("SEÑUELOS QA OK: catálogo, imágenes, afinidades, compra, reutilización y cambio de zona.");
        }
        finally
        {
            gestor.CambiarZona(gestor.ZonaInicial);
            if (propiedadAnterior < 0) PlayerPrefs.DeleteKey(compra);
            else PlayerPrefs.SetInt(compra, propiedadAnterior);
            if (economia.Saldo < saldoAnterior) economia.AgregarOro(saldoAnterior - economia.Saldo);
            else if (economia.Saldo > saldoAnterior) economia.IntentarGastarOro(economia.Saldo - saldoAnterior);
            if (existia) PlayerPrefs.SetString(clave, anterior);
            else PlayerPrefs.DeleteKey(clave);
            gestor.CambiarZona(zona);
            PlayerPrefs.Save();
        }
    }

    private static void Registrar()
    {
        limite = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update -= Verificar;
        EditorApplication.update += Verificar;
    }

    private static void Verificar()
    {
        try
        {
            if (EditorApplication.timeSinceStartup > limite) throw new Exception("Tiempo de prueba agotado.");
            if (!EditorApplication.isPlaying) return;
            var pesca = UnityEngine.Object.FindAnyObjectByType<TensionSystem>();
            if (pesca == null || pesca.GetComponent<CeboSelectorUI>() == null) return;
            EditorApplication.update -= Verificar;
            if (SessionState.GetBool("Pesca.ValidarSenuelos", false))
            {
                SessionState.SetBool("Pesca.ValidarSenuelos", false);
                ProbarSenuelos(pesca);
                Terminar(0);
                return;
            }
            var catalogo = Resources.Load<CatalogoCebos>("CatalogoCebos");
            Exigir(catalogo != null && catalogo.cebos.Length == 8, "Deben existir ocho carnadas.");
            Exigir(catalogo.cebos.All(c => c.icono != null), "Las ocho carnadas deben tener su imagen asignada.");
            var cubiertos = new HashSet<PezData>();
            foreach (var cebo in catalogo.cebos)
            {
                Exigir(cebo != null && cebo.multiplicadorAparicion == 2f, "Cebo inválido.");
                Exigir(cebo.especiesFavorecidas.Length > 0 && cebo.especiesFavorecidas.All(p => p != null), "Referencias de peces inválidas: " + cebo.name + " índices " + string.Join(",", cebo.especiesFavorecidas.Select((p, i) => p == null ? i.ToString() : "-")));
                foreach (var pez in cebo.especiesFavorecidas) cubiertos.Add(pez);
            }
            Exigir(cubiertos.Count == 25, "El catálogo debe cubrir los 25 peces.");
            ProbarSorteo(catalogo);
            var menu = pesca.GetComponent<CeboSelectorUI>();
            menu.Abrir();
            pesca.IniciarLanzamiento();
            Exigir(pesca.PuedeCambiarCebo, "El menú permite lanzar por detrás de sus controles.");
            Pulsar(pesca, "Carnada Lombriz");
            Capturar("selector", 1280, 720);
            Pulsar(pesca, "Equipar carnada");
            Exigir(pesca.CeboEquipado == catalogo.cebos[0] && !menu.EstaAbierto, "El botón no equipa la lombriz.");
            menu.Abrir();
            Pulsar(pesca, "Carnada Pez de carnada entero");
            Capturar("lista-depredadores", 1280, 720);
            Capturar("vertical", 720, 1280);
            Pulsar(pesca, "Carnada Trozos de pescado");
            Capturar("trozos", 1280, 720);
            Pulsar(pesca, "Carnada Engodo vegetal de fondo");
            Capturar("engodo", 1280, 720);
            Pulsar(pesca, "Sin carnada");
            Pulsar(pesca, "Equipar carnada");
            Exigir(pesca.CeboEquipado == null, "No se puede quitar el cebo.");
            pesca.EquiparCebo(catalogo.cebos[3]);
            Exigir(pesca.GetComponentsInChildren<Text>().Any(t => t.name == "Carnada equipada" && t.text.Contains("Frutos")), "El HUD no actualiza la carnada equipada.");
            Capturar("hud", 1280, 720);
            pesca.IniciarLanzamiento();
            Exigir(!pesca.PuedeCambiarCebo, "No se inició el lanzamiento.");
            Exigir(!pesca.EquiparCebo(null) && pesca.CeboEquipado == catalogo.cebos[3], "Se puede cambiar el cebo durante la espera.");
            menu.Abrir();
            Exigir(!menu.EstaAbierto, "Se puede abrir el selector durante la espera.");
            pesca.NotificarPique();
            Exigir(pesca.EnForcejeo && !pesca.EquiparCebo(catalogo.cebos[0]), "Se puede cambiar el cebo en combate.");
            pesca.SendMessage("VolverAListo");
            Exigir(pesca.EquiparCebo(null), "No se puede cambiar el cebo tras terminar.");
            Debug.Log($"CEBOS QA OK: 8 carnadas, {catalogo.cebos.Count(c => c.icono != null)} imágenes, 25 peces, 180000 sorteos, selector, bloqueo y retirada.");
            Terminar(0);
        }
        catch (Exception e) { Debug.LogException(e); Terminar(1); }
    }

    private static void ProbarSorteo(CatalogoCebos catalogo)
    {
        var gestor = FisheryManager.Instance;
        var peces = gestor.ZonaActual.especiesDeEstaZona.Where(p => p != null).Distinct().ToArray();
        var datos = peces.Select(p => new Vector4(p.pesoMinimo, p.pesoMaximo, p.fuerzaPez, p.longitudMaxima)).ToArray();
        float[] bases = { gestor.pesoComun, gestor.pesoPocoComun, gestor.pesoRaro, gestor.pesoEpico, gestor.pesoLegendario };
        var estadoRandom = UnityEngine.Random.state;
        bool log = Debug.unityLogger.logEnabled;
        try
        {
            Debug.unityLogger.logEnabled = false;
            UnityEngine.Random.InitState(13728);
            foreach (var cebo in new CeboData[] { null }.Concat(catalogo.cebos))
            {
                var cuentas = peces.ToDictionary(p => p, p => 0);
                var pesos = peces.ToDictionary(p => p, p => bases[(int)p.rareza] * (cebo != null && cebo.especiesFavorecidas.Contains(p) ? 2f : 1f));
                float total = pesos.Values.Sum();
                for (int i = 0; i < 20000; i++) cuentas[gestor.GenerarPezAleatorio(cebo: cebo)]++;
                foreach (var pez in peces)
                    Exigir(Mathf.Abs(cuentas[pez] / 20000f - pesos[pez] / total) < .012f, "Distribución incorrecta: " + pez.nombreEspecie);
            }
            for (int i = 0; i < peces.Length; i++)
                Exigir(datos[i] == new Vector4(peces[i].pesoMinimo, peces[i].pesoMaximo, peces[i].fuerzaPez, peces[i].longitudMaxima), "El cebo cambió las medidas o dificultad.");
        }
        finally { Debug.unityLogger.logEnabled = log; UnityEngine.Random.state = estadoRandom; }
    }

    private static void Pulsar(TensionSystem pesca, string nombre)
    {
        var boton = pesca.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == nombre);
        Exigir(boton != null && boton.interactable, "Botón no disponible: " + nombre);
        boton.onClick.Invoke();
    }

    private static void Capturar(string nombre, int ancho, int alto)
    {
        var camara = Camera.main;
        Exigir(camara != null, "Falta cámara para QA.");
        var destino = new RenderTexture(ancho, alto, 24);
        var activoAnterior = RenderTexture.active;
        var destinoAnterior = camara.targetTexture;
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>();
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
            Canvas.ForceUpdateCanvases();
            camara.Render();
            RenderTexture.active = destino;
            imagen = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            imagen.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            imagen.Apply();
            Directory.CreateDirectory("Logs/CebosQA");
            File.WriteAllBytes("Logs/CebosQA/" + nombre + ".png", imagen.EncodeToPNG());
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
            RenderTexture.active = activoAnterior;
            if (imagen != null) UnityEngine.Object.DestroyImmediate(imagen);
            destino.Release();
            UnityEngine.Object.DestroyImmediate(destino);
        }
    }

    private static void Exigir(bool valor, string mensaje) { if (!valor) throw new Exception(mensaje); }
    private static void Terminar(int codigo)
    {
        SessionState.SetBool(Clave, false);
        EditorApplication.update -= Verificar;
        EditorApplication.Exit(codigo);
    }
}
