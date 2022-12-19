using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class CarAgent : Agent
{
    private enum States
    {
        NeedToStop,
        CanGo,
    }
    public Transform Target;

    private Rigidbody m_rigidBody;
    private CarController m_controller;
    private Transform m_AgentTransform;
    private States m_AgentState = States.NeedToStop;

    [SerializeField] private float m_SpawnMinX = 1.5f;
    [SerializeField] private float m_SpawnMaxX = 3.5f;
    [SerializeField] private float m_SpawnMinZ = -23.0f;
    [SerializeField] private float m_SpawnMaxZ = -20.0f;
    [SerializeField] private Vector3 m_DefaultRotation = Vector3.zero;

    private const string k_AgentTag = "Agent";
    private const string k_BarrierTag = "Barrier";
    private const string k_IntersectionTag = "Intersection";
    private const string k_StopLineTag = "StopLine";
    private const string k_StopRegionTag = "StopRegion";
    private const string k_TargetTag = "Target";
    private const string k_YellowLineTag = "YellowLine";

    private Dictionary<string, int> m_TagMapping = new Dictionary<string, int>()
    {
        {k_AgentTag, 1},
        {k_BarrierTag, 2},
        {k_IntersectionTag, 3},
        {k_StopLineTag, 4},
        {k_StopRegionTag, 5},
        {k_TargetTag, 6},
        {k_YellowLineTag, 7},
    };
    
    
    public override void Initialize()
    {
        m_rigidBody = GetComponent<Rigidbody>();
        m_controller = GetComponent<CarController>();
        m_AgentTransform = transform;
        ResetAgentRandom();
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("on episode begin");
        ResetAgentRandom();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
        continuousActionsOut[2] = Input.GetAxis("Jump");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        var dir = (Target.position - m_AgentTransform.position).normalized;
        var velocityDir = m_rigidBody.velocity.normalized;
        var dirSimilarity = Vector3.Dot(dir, velocityDir);

        sensor.AddObservation(Vector3.Distance(Target.position, m_AgentTransform.position));
        sensor.AddObservation(dirSimilarity);

        int hitType = 0;
        // LIDAR detection
        sensor.AddObservation(ObserveRay(Vector3.forward, 10f, out hitType));
        // for now only check the hit type in the front
        sensor.AddObservation(hitType); 
        sensor.AddObservation(ObserveRay(Vector3.right, 3f, out hitType));
        sensor.AddObservation(ObserveRay(Vector3.right + Vector3.forward, 3f, out hitType));
        sensor.AddObservation(ObserveRay(Vector3.left, 3f, out hitType));
        sensor.AddObservation(ObserveRay(Vector3.left + Vector3.forward, 3f, out hitType));
        sensor.AddObservation(ObserveRay(Vector3.back, 5f, out hitType));
        sensor.AddObservation(InStopRegion() && m_AgentState == States.NeedToStop);

        AddReward(0.01f * dirSimilarity);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var contActions = actions.ContinuousActions;
        m_controller.CurrentSteeringAngle = contActions[0];
        m_controller.CurrentAcceleration = contActions[1];
        m_controller.CurrentBrakeTorque = contActions[2];

        if (m_rigidBody.velocity.magnitude > 10.0f)
        {
            // speeding
            AddReward(-10.0f * (m_rigidBody.velocity.magnitude - 10.0f));
            EndEpisode();
        }
        else if (m_rigidBody.velocity.magnitude >= 5.0f)
        {
            AddReward(0.01f);
        }
        if (m_AgentTransform.position.y < -0.04f)
        {
            // fall out of the map
            AddReward(-100.0f);
            EndEpisode();
        }
        /** for each action received, reduce rewards 
        *   improve the efficiency of agents finding way to target.
        **/
        // AddReward(-0.01f);
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag(k_BarrierTag))
        {
            Debug.Log("barrier hit");
            AddReward(-50f);
            EndEpisode();
        }
        else if (other.gameObject.CompareTag(k_AgentTag))
        {
            Debug.Log("car hit");
            AddReward(-100f);
            EndEpisode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag(k_YellowLineTag))
        {
            Debug.Log("Yellow line crossed");
            AddReward(-100f);
        }
        else if (other.gameObject.CompareTag(k_StopLineTag) && m_AgentState == States.NeedToStop)
        {
            WaitAtStopLine();
        }
        else if (other.gameObject.CompareTag(k_TargetTag))
        {
            Debug.Log("target reached");
            AddReward(1000f);
            EndEpisode();
        }
    }

    private IEnumerator WaitAtStopLine()
    {
        var timeElapsed = 0.0f;
        while (timeElapsed < 2.0f)
        {
            timeElapsed += Time.deltaTime;
            if (m_rigidBody.velocity.magnitude <= 0.01f)
            {
                AddReward(1.0f);
            }
            else
            {
                AddReward(-2.0f);
            }
            yield return null;
        }
        m_AgentState = States.CanGo;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag(k_IntersectionTag))
        {
            if (m_AgentState == States.CanGo && m_rigidBody.velocity.magnitude < 5f)
            {
                Debug.Log("in intersection and speed is too slow, probably stopping");
                AddReward(-0.1f);
            }
            else if (m_AgentState == States.NeedToStop)
            {
                // did not stop
                Debug.Log("didn't stop");
                m_AgentState = States.CanGo;
                AddReward(-10.0f);
            }
        }
        else if (m_AgentState == States.CanGo && (other.gameObject.CompareTag(k_StopLineTag) || other.gameObject.CompareTag(k_StopRegionTag)))
        {
            // we should start moving if we can go
            if (m_controller.CurrentAcceleration >= 0.01f)
            {
                Debug.Log("we can start moving and we start");
                AddReward(1.0f);
            }
            else
            {
                Debug.Log("we can start moving but we didn't");
                AddReward(-1.0f);
            }
        }
        else if (other.gameObject.CompareTag(k_StopRegionTag))
        {
            if (m_AgentState == States.NeedToStop && m_rigidBody.velocity.magnitude < 5f)
            {
                Debug.Log("in region to stop and slow enough");
                AddReward(1.0f);
            }
        }
    }

    private void ResetAgentRandom()
    {
        m_controller.CurrentAcceleration = 0.0f;
        m_controller.CurrentBrakeTorque = 0.0f;
        m_controller.CurrentSteeringAngle = 0.0f;
        m_rigidBody.velocity = Vector3.zero;
        m_rigidBody.angularVelocity = Vector3.zero;
        m_AgentTransform.rotation = Quaternion.Euler(m_DefaultRotation);
        m_AgentTransform.position = new Vector3(Random.Range(m_SpawnMinX, m_SpawnMaxX), 
                                                0.1f, 
                                                Random.Range(m_SpawnMinZ, m_SpawnMaxZ));

        m_AgentState = States.NeedToStop;
    }

    private float ObserveRay(Vector3 direction, float rayDist, out int hitType)
    {
        var dir = m_AgentTransform.TransformDirection(direction).normalized;
        RaycastHit hit;
        if (Physics.Raycast(m_AgentTransform.position, dir, out hit, rayDist))
        {
            Debug.DrawLine(m_AgentTransform.position, m_AgentTransform.position + dir * rayDist,Color.red);
            hitType = 0;
            m_TagMapping.TryGetValue(hit.transform.gameObject.tag, out hitType);
            return hit.distance >= 0 ? hit.distance / rayDist : 1f;
        }
        else
        {
            Debug.DrawLine(m_AgentTransform.position, m_AgentTransform.position + dir * rayDist,Color.green);
            hitType = 0; // nothing hit
            return 1f;
        }
    }

    private bool InStopRegion()
    {
        var dir = m_AgentTransform.TransformDirection(Vector3.down).normalized;
        RaycastHit hit;
        Debug.DrawLine(m_AgentTransform.position, m_AgentTransform.position + dir * 3 ,Color.red);
        if (Physics.Raycast(m_AgentTransform.position, dir, out hit, 3f))
        {
            return hit.collider.gameObject.CompareTag("StopRegion");
        }
        else
        {
            return false;
        }
    }
    
}
