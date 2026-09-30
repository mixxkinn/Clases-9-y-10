using UnityEngine;
using System.Collections.Generic;
// Generador narrativo basado en plantillas con marcadores {clave}
// Probado en Unity 6000.x (Unity 6)
public class DialogueGenerator : MonoBehaviour
{
    [System.Serializable]
    public class NPC
    {
        public string nombre = "Aldric";
        public string faccion = "mercader";
        public string humor = "enojo";
        public string ubicacion = "el mercado";
        public bool esPacifista = true;
    }
    public List<NPC> npcs = new List<NPC>();
    public NPC npcActivo;
    // PARTE 1: plantillas con marcadores de sustitucion
    private string[] plantillasDialogo = new string[]
    {
        "{nombre} el {faccion} dice con {humor}: no tengo tiempo para ti, hoy no hay descuentos.",
        "{nombre} mira hacia {ubicacion} y murmura con {humor}: las cosas han cambiado por aqui.",
        "{nombre} suspira con {humor} y dice: si buscas ayuda, {ubicacion} no es el lugar."
    };
    void Start()
    {
        if (npcs.Count == 0)
            npcs.Add(new NPC());
        npcActivo = npcs[0];
        // PARTE 1: generar al menos 3 dialogos distintos
        for (int i = 0; i < plantillasDialogo.Length; i++)
            Debug.Log(GenerarDialogo(plantillasDialogo[i], npcActivo));
        // PARTE 2: generar mision y lore
        GenerarMisionYLore(npcActivo);
    }
    // PARTE 1: reemplaza {clave} usando un diccionario de contexto del NPC
    public string GenerarDialogo(string plantilla, NPC npc)
    {
        Dictionary<string, string> vars = new Dictionary<string, string>()
        {
            { "nombre", npc.nombre }, { "faccion", npc.faccion },
            { "humor", npc.humor }, { "ubicacion", npc.ubicacion }
        };
        return ReemplazarMarcadores(plantilla, vars);
    }
    private string ReemplazarMarcadores(string plantilla, Dictionary<string, string> vars)
    {
        string resultado = plantilla;
        foreach (var kv in vars)
        {
            string marcador = "{" + kv.Key + "}";
            string valor = string.IsNullOrEmpty(kv.Value) ? "desconocido" : kv.Value;
            resultado = resultado.Replace(marcador, valor);
        }
        return resultado;
    }
    // PARTE 2: mision y lore con ramas condicionales por faccion/humor
    public void GenerarMisionYLore(NPC npc)
    {
        string mision = (npc.humor == "enojo")
            ? "{nombre}: Ajuste de cuentas en {ubicacion}"
            : "{nombre}: Favor pendiente en {ubicacion}";
        string lore = (npc.faccion == "mercader")
            ? "{nombre} perdio una caravana cerca de {ubicacion} y desconfia de forasteros."
            : "{nombre} guarda un secreto sobre {ubicacion} que pocos conocen.";
        Dictionary<string, string> vars = new Dictionary<string, string>()
        {
            { "nombre", npc.nombre }, { "faccion", npc.faccion },
            { "humor", npc.humor }, { "ubicacion", npc.ubicacion }
        };
        mision = ReemplazarMarcadores(mision, vars);
        lore = ReemplazarMarcadores(lore, vars);
        // MEJORA: validar coherencia antes de mostrar; si falla, regenerar variante
        if (!ValidarCoherenciaLore(npc, mision))
        {
            mision = ReemplazarMarcadores("{nombre}: Mediacion de conflicto en {ubicacion}", vars);
        }
        Debug.Log("Mision generada: " + mision);
        Debug.Log("Lore generado: " + lore);
    }
    // MEJORA: revisa contradicciones (ej. pacifista con mision de asesinato)
    public bool ValidarCoherenciaLore(NPC npc, string textoMision)
    {
        string t = textoMision.ToLower();
        if (npc.esPacifista && (t.Contains("ajuste de cuentas") || t.Contains("asesinato")))
            return false;
        return true;
    }
}