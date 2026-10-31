using UnityEngine;

/// <summary>Efectos breves, separados de la música y conectados a las acciones confirmadas.</summary>
[DisallowMultipleComponent]
public sealed class SonidosJuego : MonoBehaviour
{
    private AudioSource interfaz, acciones;
    private AudioClip clic, lanzar, pique, tirar, captura, escape, recoger;
    private TensionSystem pesca;
    private CapturaResultUI resultado;
    private GameObject raiz;
    private float siguienteClic, siguienteTiron;

    private void Awake()
    {
        raiz = new GameObject("Efectos de sonido");
        raiz.transform.SetParent(transform, false);
        interfaz = CrearFuente(.40f);
        acciones = CrearFuente(.65f);
        clic = Cargar("Clic");
        lanzar = Cargar("Lanzar");
        pique = Cargar("Pique");
        tirar = Cargar("Tirar");
        captura = Cargar("Captura");
        escape = Cargar("Escape");
        recoger = Cargar("Recoger");
        pesca = GetComponent<TensionSystem>();
        resultado = GetComponent<CapturaResultUI>();
    }

    private AudioSource CrearFuente(float volumen)
    {
        var fuente = raiz.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;
        fuente.volume = volumen;
        return fuente;
    }

    private AudioClip Cargar(string nombre)
    {
        var clip = Resources.Load<AudioClip>("Audio/Efectos/" + nombre);
        if (clip == null) Debug.LogError("Falta el efecto de sonido: " + nombre, this);
        return clip;
    }

    private void OnEnable()
    {
        siguienteClic = siguienteTiron = float.NegativeInfinity;
        if (pesca != null)
        {
            pesca.LanzamientoIniciado += AlLanzar;
            pesca.PiqueDetectado += AlPique;
            pesca.PresionCambiada += AlPresionar;
        }
        if (resultado != null)
        {
            resultado.IntentoEvaluado += AlEvaluar;
            resultado.CapturaRecogida += AlRecoger;
        }
    }

    private void OnDisable()
    {
        if (pesca != null)
        {
            pesca.LanzamientoIniciado -= AlLanzar;
            pesca.PiqueDetectado -= AlPique;
            pesca.PresionCambiada -= AlPresionar;
        }
        if (resultado != null)
        {
            resultado.IntentoEvaluado -= AlEvaluar;
            resultado.CapturaRecogida -= AlRecoger;
        }
        if (interfaz != null) interfaz.Stop();
        if (acciones != null) acciones.Stop();
    }

    private void OnDestroy()
    {
        if (raiz != null) Destroy(raiz);
    }

    public static void PulsarBoton()
    {
        var gestor = FisheryManager.Instance;
        if (gestor == null) return;
        var sonidos = gestor.GetComponent<SonidosJuego>();
        if (sonidos == null || !sonidos.isActiveAndEnabled ||
            Time.unscaledTime < sonidos.siguienteClic) return;
        sonidos.siguienteClic = Time.unscaledTime + .06f;
        if (sonidos.clic != null) sonidos.interfaz.PlayOneShot(sonidos.clic);
    }

    private void Reproducir(AudioClip clip)
    {
        if (clip != null && isActiveAndEnabled) acciones.PlayOneShot(clip);
    }

    private void AlLanzar() => Reproducir(lanzar);
    private void AlPique() => Reproducir(pique);
    private void AlEvaluar(CapturaEvaluacion evaluacion) => Reproducir(evaluacion.Capturada ? captura : escape);
    private void AlRecoger(CapturaResultado recogida) => Reproducir(recoger);

    private void AlPresionar(bool presionada)
    {
        if (!presionada || !pesca.EnForcejeo || Time.unscaledTime < siguienteTiron) return;
        siguienteTiron = Time.unscaledTime + .14f;
        Reproducir(tirar);
    }
}
