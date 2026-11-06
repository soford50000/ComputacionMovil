using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ValidarAguaCaribe
{
    public static void Ejecutar()
    {
        var shader = Resources.Load<Shader>("Agua/MarCaribe");
        if (shader == null || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
            throw new Exception("El shader del Caribe no compila o no es compatible.");
        var foto = Resources.Load<ZonaData>("Zonas/SanAndres").fondoZona.texture;
        var material = new Material(shader);
        const int ancho = 960, alto = 637;
        var destino = new RenderTexture(ancho, alto, 0, RenderTextureFormat.ARGB32);
        var lectura = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
        var anterior = RenderTexture.active;
        Directory.CreateDirectory("Logs/AguaCaribeQA");
        material.SetVector("_RippleUvPerPixel", new Vector4(1f / ancho, 1f / alto, 0, 0));
        Color32[] primero = null;
        try
        {
            // Ocho segundos a 24 fps, velocidad idéntica a FondoAguaUI.
            for (int frame = 0; frame < 192; frame++)
            {
                material.SetFloat("_RippleTime", frame / 24f * 1.52f);
                Graphics.Blit(foto, destino, material);
                RenderTexture.active = destino;
                lectura.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
                lectura.Apply();
                File.WriteAllBytes("Logs/AguaCaribeQA/agua-" + frame.ToString("D3") + ".png", lectura.EncodeToPNG());
                if (frame == 0) primero = lectura.GetPixels32();
                if (frame == 96)
                {
                    var actual = lectura.GetPixels32();
                    long cambioAgua = 0, cambioTierra = 0;
                    for (int y = 0; y < alto; y++)
                    for (int x = 0; x < ancho; x++)
                    {
                        int i = y * ancho + x;
                        int diferencia = Math.Abs(actual[i].r - primero[i].r) + Math.Abs(actual[i].g - primero[i].g) + Math.Abs(actual[i].b - primero[i].b);
                        if (y > alto * material.GetFloat("_Waterline")) cambioTierra += diferencia;
                        if (y < alto * .38f) cambioAgua += diferencia;
                    }
                    if (cambioTierra != 0 || cambioAgua < 10000) throw new Exception("Máscara o animación incorrecta: tierra=" + cambioTierra + ", agua=" + cambioAgua);
                    Debug.Log("AGUA CARIBE QA OK: shader compilado, región sobre el límite sin cambios (diferencia 0), movimiento en agua=" + cambioAgua);
                }
            }
        }
        finally
        {
            RenderTexture.active = anterior;
            destino.Release();
            UnityEngine.Object.DestroyImmediate(destino);
            UnityEngine.Object.DestroyImmediate(lectura);
            UnityEngine.Object.DestroyImmediate(material);
        }
        ValidarMenusExpedicion.Ejecutar();
    }
}
