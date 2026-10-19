using System.Collections;
using UnityEngine;

/// <summary>Fundido corto con tiempo real: funciona también con la partida pausada.</summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class EntradaExpedicion : MonoBehaviour
{
    public RectTransform Panel { private get; set; }
    public UnityEngine.UI.Outline Destello { private get; set; }
    private CanvasGroup grupo;

    private void OnEnable()
    {
        grupo = GetComponent<CanvasGroup>();
        StartCoroutine(Entrar());
    }

    private IEnumerator Entrar()
    {
        grupo.alpha = 0;
        for (float tiempo = 0; tiempo < .2f; tiempo += Time.unscaledDeltaTime)
        {
            float avance = Mathf.SmoothStep(0, 1, tiempo / .2f);
            grupo.alpha = avance;
            if (Panel != null) Panel.localScale = Vector3.one * Mathf.Lerp(.985f, 1, avance);
            yield return null;
        }
        grupo.alpha = 1;
        if (Panel != null) Panel.localScale = Vector3.one;
        if (Destello != null && Destello.gameObject.activeInHierarchy)
        {
            for (float tiempo = 0; tiempo < .7f; tiempo += Time.unscaledDeltaTime)
            {
                var color = EstiloExpedicion.Dorado;
                color.a = Mathf.Lerp(1, .25f, tiempo / .7f);
                Destello.effectColor = color;
                yield return null;
            }
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (grupo != null) grupo.alpha = 1;
        if (Panel != null) Panel.localScale = Vector3.one;
    }
}
