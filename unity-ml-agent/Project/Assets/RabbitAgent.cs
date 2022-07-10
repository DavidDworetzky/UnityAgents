using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RabbitAgent : Agent
{
    private const bool ResetActorOnEpisode = true;
    private readonly Vector3 spawnPosition = new Vector3(0, MAP_ELEVATION, 15);
    private readonly Vector3 carrotSpawnPosition = new Vector3(0, MAP_ELEVATION, 18);

    private const float fallingFloor = -100f;
    private const string lakeName = "Lake";

    private const float MAP_ELEVATION = 6f;
    private Rigidbody body;

    private bool isCollidedWater = false;

    // Reference to original starting position
    private Vector3 startPosition;

    //Orientation of RabbitAgent
    public Transform Target;

    public float forceMultiplier = 10;

    public float targetReward = 1;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
        startPosition = transform.position;
    }

    private void ResetPosition()
    {
        this.transform.localPosition = spawnPosition;
    }

    private void MoveTargetRandomPosition()
    {
        Target.localPosition = new Vector3(carrotSpawnPosition.x + (Random.value - 0.5f) * 6, MAP_ELEVATION, carrotSpawnPosition.z + (Random.value - 0.5f) * 9);
    }

    //Override for OnEpisodeBegin (training the agent.)
    public override void OnEpisodeBegin()
    {
        // reset the momentum if the agent falls
        if (this.transform.localPosition.y < fallingFloor || ResetActorOnEpisode)
        {
            this.body.angularVelocity = Vector3.zero;
            this.body.velocity = Vector3.zero;
            this.ResetPosition();
        }
        this.MoveTargetRandomPosition();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //target and agent positions
        sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.transform.localPosition);

        //agent velocity
        sensor.AddObservation(this.body.velocity.x);
        sensor.AddObservation(this.body.velocity.z);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Actions, size = 2
        Vector3 controlSignal = Vector3.zero;
        controlSignal.x = actionBuffers.ContinuousActions[0];
        controlSignal.z = actionBuffers.ContinuousActions[1];
        body.AddForce(controlSignal * forceMultiplier);

        // Rewards
        float distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);

        // Reached target
        if (distanceToTarget < 1.42f)
        {
            SetReward(targetReward);
            EndEpisode();
        }
        else if (this.transform.localPosition.y < fallingFloor)
        {
            // Fell off platform
            SetReward(-0.5f);
            EndEpisode();
        }
        else if (isCollidedWater)
        {
            isCollidedWater = false;
            SetReward(-0.5f);
            EndEpisode();
        }
    }

    void OnCollisionEnter(Collision hit)
    {
        if (hit.transform.gameObject.name.Contains(lakeName))
        {
            isCollidedWater = true;
        }
    }

    //keyboard controls for rabbit agent to override behavior.
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
    
}
