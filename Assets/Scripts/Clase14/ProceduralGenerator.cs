// Archivo: ProceduralGenerator.cs
using UnityEngine;
public class ProceduralGenerator : MonoBehaviour
{
    [Header("Referencias")]
    public HumanValidationPanel validationPanel;
    [System.Serializable]

    public class GeneratedElement
    {
        public int seed;
        public string type;
        public bool hasExit;
        public Vector3 position;
    }
    
    private string[] tipos = { "Habitacion", "Item", "Dialogo" };
    
    // PARTE 1: genera datos, los imprime en consola y los envia al panel (aun no instancia)
    public void GenerateContent()
    {
        GeneratedElement elemento = new GeneratedElement();
        elemento.seed = Random.Range(0, 99999);
        elemento.type = tipos[Random.Range(0, tipos.Length)];
        elemento.hasExit = Random.value > 0.4f;
        elemento.position = new Vector3(Random.Range(-5f, 5f), 0.5f, Random.Range(-5f, 5f));
        Debug.Log($"[ProceduralGenerator] Semilla={elemento.seed} Tipo={elemento.type} Salida={elemento.hasExit} Pos={elemento.position}");
        // MEJORA: regla minima automatica antes de la validacion humana
        if (!ValidateConstraints(elemento))
        {
        Debug.Log("[ProceduralGenerator] Auto-rechazado por ValidateConstraints, generando otro...");
        GenerateContent();
        return;
        }
        validationPanel.ShowForValidation(elemento);
    }
    
    // MEJORA: reduce carga de revision manual sin eliminar validacion humana
    private bool ValidateConstraints(GeneratedElement e)
    {
        if (e.type == "Habitacion" && !e.hasExit) return false;
        return true;
    }
    
    // PARTE 3: instanciacion SOLO llamada desde ApproveContent (ver correccion PARTE 4)
    public void InstantiateElement(GeneratedElement elemento)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.transform.position = elemento.position;
        obj.name = $"{elemento.type}_{elemento.seed}";
        Debug.Log($"[ProceduralGenerator] Instanciado {obj.name} en {elemento.position}");
    }
}