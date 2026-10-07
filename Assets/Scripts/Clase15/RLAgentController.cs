// Archivo: RLAgentController.cs
// Unity 6 + ML-Agents (com.unity.ml-agents 2.x/3.x). Rigidbody usa "linearVelocity" (API Unity 6).
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RLAgentController : Agent
{
    public Transform target;
    public float moveSpeed = 5f;
    public float areaHalfSize = 5f;
    public float maxEpisodeTime = 10f;

    private Rigidbody rb;
    private float elapsedTime;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
    }

    // PARTE 1: inicio de episodio, posiciones aleatorias del agente y el objetivo
    public override void OnEpisodeBegin()
    {
        elapsedTime = 0f;
        rb.linearVelocity = Vector3.zero;
        transform.localPosition = new Vector3(Random.Range(-areaHalfSize, areaHalfSize), 0.5f, Random.Range(-areaHalfSize, areaHalfSize));
        target.localPosition = new Vector3(Random.Range(-areaHalfSize, areaHalfSize), 0.5f, Random.Range(-areaHalfSize, areaHalfSize));
    }

    // PARTE 1: observaciones -> posicion relativa (X,Z) + velocidad (X,Z) = 4 floats
    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 relPos = target.localPosition - transform.localPosition;
        sensor.AddObservation(relPos.x);
        sensor.AddObservation(relPos.z);
        sensor.AddObservation(rb.linearVelocity.x);
        sensor.AddObservation(rb.linearVelocity.z);
        // Space Size en Behavior Parameters debe ser 4
    }

    // PARTE 2: acciones (mover en X y Z) y funcion de recompensa
    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];
        Vector3 move = new Vector3(moveX, 0f, moveZ) * moveSpeed * Time.deltaTime;
        rb.MovePosition(transform.position + move);

        float distance = Vector3.Distance(transform.localPosition, target.localPosition);
        AddReward(-distance * 0.001f); // premia acercarse al objetivo

        // MEJORA: penalizacion por paso para incentivar rutas mas cortas
        //El cambio fue meterlo dentro de un IF para evitar dividir entre 0
        if (MaxStep > 0)
        {
            AddReward(-1f / MaxStep);
        }

        elapsedTime += Time.deltaTime;

        if (Mathf.Abs(transform.localPosition.x) > areaHalfSize || Mathf.Abs(transform.localPosition.z) > areaHalfSize)
        {
            AddReward(-1f);
            Debug.Log("Episode ended, reward: " + GetCumulativeReward());
            EndEpisode();
        }
        else if (elapsedTime >= maxEpisodeTime)
        {
            AddReward(-1f);
            Debug.Log("Episode ended, reward: " + GetCumulativeReward());
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        c[0] = Input.GetAxis("Horizontal");
        c[1] = Input.GetAxis("Vertical");
    }

    // PARTE 2: eventos de recompensa por colision (meta)
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == target)
        {
            SetReward(1f);
            Debug.Log("Episode ended, reward: " + GetCumulativeReward());
            EndEpisode();
        }
    }
}
