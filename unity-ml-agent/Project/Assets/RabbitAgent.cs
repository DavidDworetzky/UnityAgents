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
    private readonly Vector3 wolfSpawnPosition = new Vector3(0, MAP_ELEVATION - 10, -50);

    private const float fallingFloor = -100f;
    private const string lakeName = "Lake";

    private const int tideDelay = 20;

    private const float MAP_ELEVATION = 6f;
    private Rigidbody body;

    private bool isCollidedWater = false;

    // Reference to original starting position
    private Vector3 startPosition;

    //Orientation of RabbitAgent
    public Transform Target;

    public GameObject Avoid1;

    public GameObject Avoid2;

    public Transform Wolf;

    public bool Avoid1TriggersEnd;

    public bool Avoid2TriggersEnd;

    //Public setting determing if we are the primary agent (scene agent to trigger scene behaviors)

    public bool IsPrimaryAgent;

    public float forceMultiplier = 10;

    public float targetReward = 1;

    private int tideCounter = 0;
    private bool tideToggled = false;

    private GameObject[] TideObjects;

    // Start is called before the first frame update
    void Start()
    {
        body = GetComponent<Rigidbody>();
        startPosition = transform.position;
        if (IsPrimaryAgent)
        {
            TideObjects = GameObject.FindGameObjectsWithTag("Tide");
            InvokeRepeating("IncrementTide", 1.0f, 1.0f);
        }
    }

    private void ResetPosition()
    {
        this.transform.localPosition = spawnPosition;
    }

    private void MoveTargetRandomPosition()
    {
        Target.localPosition = new Vector3(carrotSpawnPosition.x + (Random.value - 0.5f) * 6, MAP_ELEVATION, carrotSpawnPosition.z + (Random.value - 0.5f) * 9);
    }

    private void MoveWolfRandomPosition()
    {
        Wolf.localPosition = new Vector3(wolfSpawnPosition.x + (Random.value - 0.5f) * 6, MAP_ELEVATION - 5, wolfSpawnPosition.z + (Random.value - 0.5f) * 9 );
    }

    private void ToggleTideObjects(bool toggle)
    {
        tideToggled = toggle;
        Debug.Log($"Toggling tide objects active to: {toggle}");
        var objects = TideObjects;
        foreach (var t in objects)
        {
            t.SetActive(toggle);
        }
    }

    void IncrementTide()
    {
        tideCounter++;
        if(tideCounter > tideDelay)
        {
            //tide comes in, and comes out.
            tideCounter = 0;
            ToggleTideObjects(!tideToggled);
        }
        Debug.Log($"tide counter: {tideCounter}");
    }

    //Override for OnEpisodeBegin (training the agent.)
    public override void OnEpisodeBegin()
    {
        if (IsPrimaryAgent)
        {
            //tide is reset to start.
            tideCounter = 0;
            ToggleTideObjects(false);
            // reset the momentum if the agent falls
            if (this.transform.localPosition.y < fallingFloor || ResetActorOnEpisode)
            {
                this.body.angularVelocity = Vector3.zero;
                this.body.velocity = Vector3.zero;
                this.ResetPosition();
            }
            this.MoveTargetRandomPosition();
            this.MoveWolfRandomPosition();
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //target and agent positions
        sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(this.transform.localPosition);

        //agent velocity
        sensor.AddObservation(this.body.velocity.x);
        sensor.AddObservation(this.body.velocity.z);

        //avoid 1 and 2 positions
        sensor.AddObservation(Avoid1.transform.localPosition);
        sensor.AddObservation(Avoid2.transform.localPosition);
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
        float distanceToAvoid1 = Vector3.Distance(this.transform.localPosition, Avoid1.transform.localPosition);
        float distancetoAvoid2 = Vector3.Distance(this.transform.localPosition, Avoid2.transform.localPosition);


        //update reward functions and end the episode if we are the primary agent.
        if (IsPrimaryAgent)
        {
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
            else if (distanceToAvoid1 < 2.0f && Avoid1TriggersEnd)
            {
                SetReward(-0.5f);
                EndEpisode();
            }
            else if (distancetoAvoid2 < 2.0f && Avoid2TriggersEnd)
            {
                SetReward(-0.5f);
                EndEpisode();
            }
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
