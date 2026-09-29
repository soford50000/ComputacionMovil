using UnityEngine;

[CreateAssetMenu(fileName = "NuevaZona", menuName = "Pesca/ZonaData")]
public class ZonaData : ScriptableObject
{
    [Header("identificacion")]
    public string nombreZona;
    [TextArea]
    public string descripcion;

   [Header("Visual")] 
   public Sprite fondoZona;
   public Sprite IconoMenu;

    [Header("Audio")]
    public AudioClip musicaAmbiente;

    [Header ("Especies disponibles")]
    public PezData[] especiesDeEstaZona; 


}
