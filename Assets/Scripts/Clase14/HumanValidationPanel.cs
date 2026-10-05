// Archivo: HumanValidationPanel.cs
using UnityEngine;
public class HumanValidationPanel : MonoBehaviour
{
    public ProceduralGenerator generator;
    private ProceduralGenerator.GeneratedElement elementoActual;
    private bool esperandoValidacion = false;
    
    // PARTE 2: muestra el contenido generado sin instanciarlo todavia
    public void ShowForValidation(ProceduralGenerator.GeneratedElement elemento)
    {
        elementoActual = elemento;
        esperandoValidacion = true;
    }
    
    // PARTE 3: Aprobar instancia en la escena, Rechazar descarta y regenera
    public void ApproveContent()
    {
        Debug.Log("[HumanValidationPanel] Elemento APROBADO por el usuario.");
        generator.InstantiateElement(elementoActual); // unica llamada a InstantiateElement (PARTE 4)
        esperandoValidacion = false;
    }
    
    public void RejectContent()
    {
        Debug.Log("[HumanValidationPanel] Elemento RECHAZADO, generando nueva propuesta.");
        esperandoValidacion = false;
        generator.GenerateContent();
    }
    
    private void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 400, 20), "Generador Procedural con Validacion Humana");
        if (GUI.Button(new Rect(10, 40, 120, 30), "Generar"))
        {
            generator.GenerateContent();
        }
            if (esperandoValidacion)
        {
            GUI.Box(new Rect(10, 80, 380, 90), "Elemento propuesto");
            GUI.Label(new Rect(20, 105, 360, 20), $"Tipo: {elementoActual.type} | Semilla: {elementoActual.seed}");
            GUI.Label(new Rect(20, 125, 360, 20), $"Tiene salida: {elementoActual.hasExit} | Pos: {elementoActual.position}");
            if (GUI.Button(new Rect(20, 150, 100, 30), "Aprobar")) ApproveContent();
            if (GUI.Button(new Rect(130, 150, 100, 30), "Rechazar")) RejectContent();
        }
    }
}