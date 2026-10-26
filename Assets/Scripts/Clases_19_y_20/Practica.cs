// Archivo: Practica.cs
// Unity 6 (C#). Pegar en Assets/Scripts/. No hace falta crear objetos en la escena: todo se crea por código.
// Si usas Unity 2022 o anterior, cambia rb.linearVelocity por rb.velocity.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class Arranque
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<AgentePrueba>();
        new GameObject("Bronn").AddComponent<NpcBronn>();

        if (Camera.main)
        {
            Camera.main.transform.position = new Vector3(10, 12, -6);
            Camera.main.transform.LookAt(new Vector3(0, 0, 6));
        }
    }
}

// ===== PARTE 1: agente de prueba con registro de recorrido y detector de bloqueos =====

public class AgentePrueba : MonoBehaviour
{
    Rigidbody rb;
    Vector3[] ruta;
    int idx, bloqueos;
    bool desvio, fin;
    float t0, tCheck;
    Vector3 ultima;

    void Start()
    {
        var muro = GameObject.CreatePrimitive(PrimitiveType.Cube);
        muro.transform.position = new Vector3(0, 1, 6);
        muro.transform.localScale = new Vector3(6, 2, 0.5f);

        var meta = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        meta.transform.position = new Vector3(0, 0.5f, 12);
        Destroy(meta.GetComponent<Collider>());

        rb = gameObject.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        t0 = Time.time;
        Reiniciar();
    }

    void Reiniciar()
    {
        transform.position = new Vector3(0, 0.5f, 0);
        rb.linearVelocity = Vector3.zero;

        ruta = desvio
            ? new[] { new Vector3(5, 0.5f, 6), new Vector3(0, 0.5f, 12) }
            : new[] { new Vector3(0, 0.5f, 12) };

        idx = 0;
        ultima = transform.position;
        tCheck = Time.time;
    }

    void FixedUpdate()
    {
        if (fin) return;

        var d = ruta[idx] - rb.position;
        d.y = 0;

        if (d.magnitude < 0.5f)
        {
            idx++;

            if (idx >= ruta.Length)
            {
                fin = true;
                rb.linearVelocity = Vector3.zero;

                Debug.Log($"Agente: recorrido completado en {Time.time - t0:F0} s, bloqueos detectados: {bloqueos}");
            }

            return;
        }

        rb.linearVelocity = d.normalized * 4f;

        if (Time.time - tCheck > 2f)
        {
            // si casi no avanzó en 2 s, hay bloqueo
            if ((rb.position - ultima).magnitude < 0.3f)
            {
                bloqueos++;
                desvio = true;

                Debug.Log($"Bloqueo detectado en {rb.position}, episodio reiniciado");

                Reiniciar();
            }
            else
            {
                ultima = rb.position;
                tCheck = Time.time;
            }
        }
    }
}

// ===== PARTE 2: NPC conversacional (usa Ollama local y gratis: http://localhost:11434) =====

[System.Serializable]
public class Msg
{
    public string role, content;

    public Msg(string r, string c)
    {
        role = r;
        content = c;
    }
}

[System.Serializable]
class Req
{
    public string model;
    public bool stream;
    public List<Msg> messages;
}

[System.Serializable]
class Resp
{
    public Msg message;
}

public class NpcBronn : MonoBehaviour
{
    const string Url = "http://localhost:11434/api/chat", Modelo = "llama3.2";

    const string Personalidad = "Eres Brónn, herrero gruñón de una aldea medieval. Respondes en español, breve, sin salir del personaje.";

    const string Contexto = "Nivel: aldea de Piedrahierro; la forja necesita carbón y hay rumores de lobos en el bosque.";

    static readonly string[] Prohibido =
    {
        "bomba",
        "arma real",
        "hackear",
        "suicid",
        "droga",
        "ignora tus instrucciones",
        "ignora las reglas"
    };

    static readonly string[] Cortesias =
    {
        "por favor",
        "gracias",
        "buenos días",
        "buenas tardes",
        "disculpa"
    };

    List<Msg> memoria = new List<Msg>(); // campo de clase: se conserva entre turnos (corrección del error)

    string nombre = "", entrada = "", pantalla = "", respuesta;
    int confianza;
    bool ocupado; // confianza = MEJORA

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 520, 420));

        GUILayout.Label(pantalla);
        entrada = GUILayout.TextField(entrada);

        if (GUILayout.Button("Enviar") && !ocupado && entrada.Trim() != "")
        {
            var t = entrada;
            entrada = "";
            StartCoroutine(Turno(t));
        }

        GUILayout.EndArea();
    }

    void Decir(string s)
    {
        Debug.Log(s);
        pantalla += "\n" + s;
    }

    IEnumerator Turno(string texto)
    {
        ocupado = true;
        Decir("Jugador: " + texto);

        string low = texto.ToLower(), r;

        if (Prohibido.Any(p => low.Contains(p)))
        {
            r = "Eso no es asunto de la aldea. Pregunta otra cosa."; // filtro de seguridad
        }
        else
        {
            int k = low.IndexOf("me llamo ");

            if (k >= 0)
                nombre = texto.Substring(k + 9).Trim(' ', '.', '!');

            bool cortes = Cortesias.Any(c => low.Contains(c));

            if (cortes)
                confianza++; // MEJORA: +1 por cortesía

            memoria.Add(new Msg("user", texto));

            if (cortes && confianza == 3)
            {
                string n = nombre == "" ? "forastero" : nombre;
                r = "Bueno, " + n + ", pasa y te enseño la forja";
            }
            else
            {
                yield return Llamar(low);
                r = respuesta;
            }

            memoria.Add(new Msg("assistant", r));

            while (memoria.Count > 12)
                memoria.RemoveAt(0); // últimos 6 turnos (12 mensajes)
        }

        Decir("Brónn: " + r);
        ocupado = false;
    }

    IEnumerator Llamar(string low)
    {
        string tono = confianza >= 3
            ? "Ahora confías en el jugador: habla amable."
            : "Hablas seco y desconfiado.";

        string sis = Personalidad + " " + Contexto + " " + tono
            + (nombre != "" ? " El jugador se llama " + nombre + "." : "")
            + " Si piden algo fuera de la aldea, rechaza en personaje.";

        var msgs = new List<Msg> { new Msg("system", sis) };
        msgs.AddRange(memoria);

        string json = JsonUtility.ToJson(
            new Req
            {
                model = Modelo,
                stream = false,
                messages = msgs
            }
        );

        using var web = new UnityWebRequest(Url, "POST");

        web.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        web.downloadHandler = new DownloadHandlerBuffer();

        web.SetRequestHeader("Content-Type", "application/json");
        web.timeout = 20;

        yield return web.SendWebRequest();

        if (web.result == UnityWebRequest.Result.Success)
        {
            respuesta = JsonUtility.FromJson<Resp>(web.downloadHandler.text).message.content.Trim();
        }
        else if (low.Contains("llamo") && low.Contains("?") && nombre != "")
        {
            respuesta = nombre + ", ¿verdad? Ya me lo dijiste. Habla rápido, tengo trabajo."; // respaldo
        }
        else
        {
            respuesta = "Hmm... el yunque suena fuerte, no te oigo. Inténtalo luego."; // respaldo
        }
    }
}