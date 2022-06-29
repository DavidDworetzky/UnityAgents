using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class RabbitAgent : Agent
{
    //RigidBody reference
    private RigidBody body;

    // Reference to original starting position
    private Vector3 startPosition;

    //Orientation of RabbitAgent
    public Transform Target;

    public float forceMultiplier = 10;

    public float targetReward = 1;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<RigidBody>();
        startPosition = transform.position;
    }

    private void ResetPosition()
    {
        this.transform.localPosition = startPosition;
    }

    private void MoveTargetRandomPosition()
    {
        Target.localPosition = new Vector3(Random.value * 8 - 4, 0.5f, Random.value * 8 - 4);
    }

    //Override for OnEpisodeBegin (training the agent.)
    public override void OnEpisodeBegin()
    {
        // reset the momentum if the agent falls
        if (this.transform.localPosition.y < 0)
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
        else if (this.transform.localPosition.y < 0)
        {
            // Fell off platform
            EndEpisode();
        }
    }
}
