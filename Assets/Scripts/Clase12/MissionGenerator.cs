using System.Collections.Generic;
using UnityEngine;

public class MissionGenerator : MonoBehaviour
{
    // PARTE 1: Estructura de datos MissionRule
    [System.Serializable]
    public class MissionRule
    {
        public string missionType;
        public string[] contextTags;
        public float weight;
        public int minDifficulty;
        public int maxDifficulty;
    }

    public List<MissionRule> rules = new List<MissionRule>();
    private List<string> missionHistory = new List<string>();
    public string[] currentContext = { "bosque" };

    void Start()
    {
        CargarReglas();

        Debug.Log("=== PARTE 1: Reglas cargadas ===");
        foreach (var r in rules)
            Debug.Log($"Tipo: {r.missionType} | Peso: {r.weight} | Dificultad: [{r.minDifficulty}-{r.maxDifficulty}]");

        Debug.Log("=== PARTE 2 y 3: Generación de misiones ===");
        int[] dificultades = { 1, 2, 5 };
        foreach (int d in dificultades)
        {
            string mision = GenerateMission(d, currentContext);
            Debug.Log($"Dificultad solicitada {d} -> Misión generada: {mision}");
        }
    }

    void CargarReglas()
    {
        rules.Add(new MissionRule { missionType = "Explorar", contextTags = new[] { "bosque", "base_espacial" }, weight = 3f, minDifficulty = 1, maxDifficulty = 2 });
        rules.Add(new MissionRule { missionType = "Recolectar_Hierbas", contextTags = new[] { "bosque" }, weight = 2f, minDifficulty = 1, maxDifficulty = 3 });
        rules.Add(new MissionRule { missionType = "Reparar_Nave", contextTags = new[] { "base_espacial" }, weight = 2f, minDifficulty = 2, maxDifficulty = 4 });
        rules.Add(new MissionRule { missionType = "Derrotar_Jefe", contextTags = new[] { "bosque", "base_espacial" }, weight = 1f, minDifficulty = 4, maxDifficulty = 5 });
        rules.Add(new MissionRule { missionType = "Escoltar_NPC", contextTags = new[] { "bosque" }, weight = 2.5f, minDifficulty = 2, maxDifficulty = 4 });
    }

    // PARTE 2 y 3: selección ponderada + validación de contexto + MEJORA (no repetir tipo consecutivo)
    string GenerateMission(int dificultad, string[] contexto)
    {
        List<MissionRule> validas = new List<MissionRule>();
        foreach (var r in rules)
        {
            bool contextoValido = false;
            foreach (var tag in r.contextTags)
                if (System.Array.IndexOf(contexto, tag) >= 0) { contextoValido = true; break; }

            bool dificultadValida = dificultad >= r.minDifficulty && dificultad <= r.maxDifficulty;

            // MEJORA: evitar repetir el mismo tipo que la última misión generada
            bool noRepetida = missionHistory.Count == 0 || missionHistory[missionHistory.Count - 1] != r.missionType;

            if (contextoValido && dificultadValida && noRepetida)
                validas.Add(r);
        }

        if (validas.Count == 0)
        {
            Debug.LogWarning("No hay reglas válidas para este contexto/dificultad");
            return "Ninguna";
        }

        // Corrección del error del punto 4: el acumulador debe ser local (no estático)
        // para que se reinicie en cada llamada a la función.
        float pesoTotal = 0f;
        foreach (var r in validas) pesoTotal += r.weight;

        float valorAleatorio = Random.Range(0f, pesoTotal);
        float acumulado = 0f; // variable local, se reinicia en cada ejecución
        MissionRule seleccionada = validas[0];

        foreach (var r in validas)
        {
            acumulado += r.weight;
            if (valorAleatorio <= acumulado)
            {
                seleccionada = r;
                break;
            }
        }

        missionHistory.Add(seleccionada.missionType);
        return $"{seleccionada.missionType} (Dif:{dificultad}, Contexto:{string.Join(",", contexto)})";
    }
}
