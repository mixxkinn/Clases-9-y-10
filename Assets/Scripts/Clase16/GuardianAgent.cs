// Archivo: GuardianAgent.cs
// Requiere el paquete Unity ML-Agents (com.unity.ml-agents) version 2.0 o superior, en Unity 6
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class GuardianAgent : Agent
{
    public Transform goal;
    public Transform restrictedZoneCenter;
    public float restrictedZoneRadius = 3f;
    public float aversionAlRiesgo = 1.5f; // MEJORA: multiplicador de personalidad (aversion al riesgo)
    private float distanciaAnterior;

    public override void OnEpisodeBegin()
    {
        transform.localPosition = new Vector3(Random.Range(-8f, 8f), 0.5f, Random.Range(-8f, 8f));
        distanciaAnterior = Vector3.Distance(transform.localPosition, goal.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(goal.localPosition);
        sensor.AddObservation(restrictedZoneCenter.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // PARTE 1: recompensa de progreso hacia la meta
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        transform.localPosition += new Vector3(moveX, 0, moveZ) * Time.deltaTime * 3f;

        float distanciaActual = Vector3.Distance(transform.localPosition, goal.localPosition);
        float progreso = distanciaAnterior - distanciaActual;
        AddReward(progreso * 1.0f);   // positiva si se acerca, negativa si se aleja
        AddReward(-0.001f);           // penalizacion pequeña por paso, incentiva rutas eficientes
        distanciaAnterior = distanciaActual;

        // MEJORA: penalizacion gradual por cercania a la zona restringida (aversion al riesgo)
        float distZona = Vector3.Distance(transform.localPosition, restrictedZoneCenter.localPosition);
        float radioAlerta = restrictedZoneRadius * 1.5f;
        if (distZona < radioAlerta)
        {
            float cercania = (radioAlerta - distZona) / radioAlerta;
            AddReward(-cercania * 0.02f * aversionAlRiesgo);
        }

        if (distanciaActual < 1.0f)
        {
            AddReward(5.0f);
            EndEpisode();
        }

        Debug.Log($"Paso -> recompensa acumulada del episodio = {GetCumulativeReward():F3}");
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal");
        continuousActions[1] = Input.GetAxis("Vertical");
    }

    // PARTE 2: penalizacion por cruzar la zona limite etica
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("ZonaRestringida"))
        {
            AddReward(-5.0f); // penalizacion alta, ejecutada de inmediato antes de terminar el episodio
            Debug.Log("PENALIZACION APLICADA: el agente cruzo la zona restringida");
            EndEpisode();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (restrictedZoneCenter == null) return;

        // 1. Dibuja la zona restringida (radio de colisión) en ROJO
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawSphere(restrictedZoneCenter.position, restrictedZoneRadius);

        // 2. Dibuja el radio de alerta (donde empieza la aversión) en AMARILLO
        Gizmos.color = new Color(1f, 0.92f, 0.01f, 0.2f);
        float radioAlerta = restrictedZoneRadius * 1.5f;
        Gizmos.DrawSphere(restrictedZoneCenter.position, radioAlerta);
    }
}
