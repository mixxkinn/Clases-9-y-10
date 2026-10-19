// Archivo: ImitacionNPC.cs
// Unity 6 (C#). Si usas el Input System nuevo, pon Active Input Handling en "Both" (ver EJECUCION).
using System.Collections.Generic;
using Unity.MLAgents.Integrations.Match3;
using UnityEngine;
using UnityEngine.UIElements;
public class ImitacionNPC : MonoBehaviour
{
    struct Demo { public float[] o; public int a; }
    const float MaxD = 10f;
    static readonly string[] Nom = { "avanzar", "esquivar izquierda", "esquivar derecha", "detenerse"
   };
    List<Demo> demos = new List<Demo>();
    List<GameObject> obst = new List<GameObject>();
    Transform npc, agente, meta;
    bool grabando = true;
    float tGrab, tEp, tQuieto, tAleat;
    Vector3 ultPos, dirAleat;
    int baja, decis, vict, epis, dific;
    public float umbral = 0.6f; // MEJORA: umbral de confianza
    GameObject Crear(PrimitiveType t, Vector3 pos, Vector3 esc, Color c, bool col)
    {
        var g = GameObject.CreatePrimitive(t);
        g.transform.position = pos; g.transform.localScale = esc;
        g.GetComponent<Renderer>().material.color = c;
        if (!col) Destroy(g.GetComponent<Collider>());
        return g;
    }
    void Start()
    {
        Crear(PrimitiveType.Plane, new Vector3(0, 0, 10), new Vector3(5, 1, 3), Color.gray, false);
        npc = Crear(PrimitiveType.Cube, Vector3.zero, Vector3.one, Color.blue, false).transform;
        agente = Crear(PrimitiveType.Cube, Vector3.zero, Vector3.one, Color.red, false).transform;
        meta = Crear(PrimitiveType.Sphere, Vector3.zero, Vector3.one, Color.green, false).transform;
        Camera.main.transform.SetPositionAndRotation(new Vector3(0, 28, 8), Quaternion.Euler(90, 0, 0
  ));
        NuevoNivel();
    }
    // PARTE 1: observación normalizada (0 a 1), usada igual al grabar y al decidir
    float[] Obs(Vector3 p)
    {
        float R(Vector3 d) => Physics.Raycast(p, d.normalized, out var h, MaxD) ? h.distance / MaxD :
   1f;
        float g = Mathf.Clamp((meta.position.x - p.x) / MaxD, -1f, 1f) * 0.5f + 0.5f;
        return new[] { R(Vector3.forward), R(new Vector3(-1, 0, 1)), R(new Vector3(1, 0, 1)), g };
    }
    Vector3 Dir(int a) => a == 0 ? Vector3.forward : a == 1 ? new Vector3(-1, 0, 0.5f) : a == 2 ? new
   Vector3(1, 0, 0.5f) : Vector3.zero;
    void Mover(Transform t, Vector3 d)
    {
        if (d == Vector3.zero) return;
        d.Normalize();
        if (!Physics.Raycast(t.position, d, 0.7f)) t.position += d * 4f * Time.deltaTime;
    }
    // PARTE 1: acción del ejemplo grabado más cercano (vecino más cercano)
    int Decidir(float[] o, List<Demo> src, out float dist)
    {
        float mejor = float.MaxValue; int acc = 3;
        foreach (var d in src)
        {
            float s = 0;
            for (int i = 0; i < o.Length; i++) s += (o[i] - d.o[i]) * (o[i] - d.o[i]);
            if (s < mejor) { mejor = s; acc = d.a; }
        }
        dist = Mathf.Sqrt(mejor);
        return acc;
    }
    // PARTE 2: evaluación de la política (1 de cada 5 demostraciones es de prueba)
    void Evaluar()
    {
        if (demos.Count < 10) { Debug.LogWarning("Graba al menos 10 pares."); return; }
        var tr = new List<Demo>(); var te = new List<Demo>();
        for (int i = 0; i < demos.Count; i++) (i % 5 == 4 ? te : tr).Add(demos[i]);
        int[] ac = new int[3], to = new int[3]; int suma = 0;
        foreach (var t in te)
        {
            to[t.a]++;
            if (Decidir(t.o, tr, out _) == t.a) { ac[t.a]++; suma++; }
        }
        Debug.Log($"Precisión de imitación: {100f * suma / te.Count:F1}% ({suma}/{te.Count})");
        for (int i = 0; i < 3; i++) Debug.Log($"  {Nom[i]}: {ac[i]} de {to[i]}");
    }
    // PARTE 2: nivel con obstáculos según la dificultad
    void NuevoNivel()
    {
        foreach (var g in obst) Destroy(g);
        obst.Clear();
        for (int i = 0; i < 2 + dific; i++)
            obst.Add(Crear(PrimitiveType.Cube, new Vector3(Random.Range(-4f, 4f), 0.5f, Random.Range(
  4f, 13f)), new Vector3(2.5f, 1, 1), Color.yellow, true));
        npc.position = new Vector3(0, 0.5f, 0); agente.position = new Vector3(2, 0.5f, 0);
        meta.position = new Vector3(Random.Range(-3f, 3f), 0.5f, 15);
        tEp = 0; baja = 0; decis = 0;
    }
    void FinEpisodio(bool gano)
    {
        if (!grabando)
        {
            epis++; if (gano) vict++;
            Debug.Log($"Decisiones de baja confianza: {baja} de {decis}"); // MEJORA
            if (epis % 3 == 0) // PARTE 2: ajuste de dificultad según tasa de victoria
            {
                float tasa = vict / (float)epis;
                dific = tasa > 0.7f ? dific + 1 : tasa < 0.3f ? Mathf.Max(0, dific - 1) : dific;
                Debug.Log($"Tasa de victoria {tasa:P0} -> dificultad {dific}");
                epis = 0; vict = 0;
            }
        }
        NuevoNivel();
    }
    void Update()
    {
        tEp += Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            grabando = !grabando; if (!grabando) Evaluar(); NuevoNivel();
        }
        Vector3 p = npc.position;
        if (grabando)
        {
            int a = Input.GetKey(KeyCode.UpArrow) ? 0 : Input.GetKey(KeyCode.LeftArrow) ? 1 : Input.GetKey(KeyCode.RightArrow) ? 2 : -1;
            tGrab += Time.deltaTime;
            if (a >= 0)
            {
                Mover(npc, Dir(a));
                if (tGrab > 0.1f)
                {
                    tGrab = 0; var o = Obs(p); demos.Add(new Demo { o = o, a = a });
                    Debug.Log($"Demo {demos.Count}: distancia {o[0] * MaxD:F1} y acción {Nom[a]}");
                }
            }
        }
        else if (demos.Count > 0)
        {
            int a = Decidir(Obs(p), demos, out float d); decis++;
            if (d > umbral) { a = 3; baja++; } // MEJORA: baja confianza -> detenerse
            Mover(npc, Dir(a));
            if (tEp > 25f) FinEpisodio(false);
        }
        if (Vector3.Distance(npc.position, meta.position) < 1.2f) FinEpisodio(true);
        // PARTE 2: agente de prueba y detección de bloqueo
        Vector3 dir = meta.position - agente.position; dir.y = 0;
        if (tAleat > 0) { tAleat -= Time.deltaTime; dir = dirAleat; }
        Mover(agente, dir);
        if (Vector3.Distance(agente.position, meta.position) < 1.2f) agente.position = new Vector3(2,0.5f, 0);
        if (Vector3.Distance(agente.position, ultPos) > 0.05f)
        {
            ultPos = agente.position; tQuieto =0;
        }
        else if ((tQuieto += Time.deltaTime) > 3f)
        {
            Debug.Log($"BLOQUEO en {agente.position} a los {Time.time:F1} s");
            tQuieto = 0; tAleat = 1.5f; dirAleat = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        }
    }
    void OnGUI()
    {
        string m = grabando ? "GRABAR" : "IMITAR";
        GUI.Label(new Rect(10, 10, 700, 30), "Modo: " + m + " | Pares: " + demos.Count + " | Flechas = mover, Tab = cambiar");
    }
}
