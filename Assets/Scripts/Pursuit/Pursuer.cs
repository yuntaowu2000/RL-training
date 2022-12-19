using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

namespace OriginalProblem
{
    public class Pursuer : Agent
    {
        [SerializeField] Transform m_EvaderTransform;
        [SerializeField] float m_Radius = 10f;
        [SerializeField] float m_MaxSpeed = 1f;
        public float MaxSpeed => m_MaxSpeed;

        private float m_Theta;
        private Transform m_Transform;
        private Evader m_EvaderBehavior;

        public void Awake()
        {
            m_EvaderBehavior = m_EvaderTransform.GetComponent<Evader>();
            m_EvaderBehavior.OnEscaped += OnEvaderEscaped;
            m_EvaderBehavior.OnCaught += OnEvaderCaught;
            m_EvaderBehavior.OnTimeout += EndEpisode;
        }
        public void OnDrawGizmos()
        {
            if (m_Transform == null)
            {
                m_Transform = transform;
            }
            Gizmos.DrawWireSphere(m_Transform.parent.transform.position, m_Radius); // region of interest
            
            if (m_EvaderBehavior == null)
            {
                m_EvaderBehavior = m_EvaderTransform.GetComponent<Evader>();
            }
            // decision boundary = R * v_2 / v_1 = (R * evader.MaxSpeed) / (R * m_MaxSpeed)
            Gizmos.DrawWireSphere(m_Transform.parent.transform.position, m_EvaderBehavior.MaxSpeed / m_MaxSpeed); 
            Gizmos.DrawWireSphere(m_Transform.position, 1.5f); // region pursuer can catch
        }

        public override void Initialize()
        {
            m_Transform = transform;
            m_Theta = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI);
        }

        public override void OnEpisodeBegin()
        {
            m_Theta = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI);
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousActionsOut = actionsOut.ContinuousActions;
            continuousActionsOut[0] = Input.GetAxis("Horizontal");
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(new Vector2(m_Transform.localPosition.x, m_Transform.localPosition.z));
            sensor.AddObservation(new Vector2(m_EvaderTransform.localPosition.x, m_EvaderTransform.localPosition.z));
            sensor.AddObservation(Vector3.Distance(m_Transform.localPosition, m_EvaderTransform.localPosition));
            sensor.AddObservation(Vector3.Dot(m_Transform.localPosition.normalized, m_EvaderTransform.localPosition.normalized));
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var contActions = actions.ContinuousActions;
            m_Theta += Mathf.Clamp(contActions[0], -1.0f, 1.0f) * m_MaxSpeed * Time.deltaTime;
            m_Theta = m_Theta % (2 * Mathf.PI);
            m_Transform.localPosition = new Vector3(m_Radius * Mathf.Cos(m_Theta), 0.5f, m_Radius * Mathf.Sin(m_Theta));

            // pursuer wants to make the angle as small as possible
            var angle = Vector3.Dot(m_Transform.localPosition.normalized, m_EvaderTransform.localPosition.normalized);
            AddReward(angle);
        }

        public void OnEvaderEscaped(float angle)
        {
            AddReward(-1000f);
            EndEpisode();
        }

        public void OnEvaderCaught(float angle)
        {
            AddReward(1000f);
            EndEpisode();
        }
    }
}
