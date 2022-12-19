using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

namespace InverseProblem
{
    public class Pursuer : Agent
    {
        [SerializeField] Transform m_EvaderTransform;
        [SerializeField] float m_MaxSpeed = 1f;
        [SerializeField] float m_Radius = 10f;
        public event Action OnCaught;
        public event Action<float, float> OnTimeout;
        public event Action<List<float>> OnStatusUpdate;

        private Transform m_Transform;
        private float m_StartTime;

        public void OnDrawGizmos()
        {
            if (m_Transform == null)
            {
                m_Transform = transform;
            }
            Gizmos.DrawWireSphere(m_Transform.parent.transform.position, m_Radius);
            Gizmos.DrawWireSphere(m_Transform.position, 1.5f);
        }

        public override void Initialize()
        {
            m_Transform = transform;
            m_Transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
        }

        public override void OnEpisodeBegin()
        {
            m_Transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
            m_StartTime = Time.time;
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousActionsOut = actionsOut.ContinuousActions;
            continuousActionsOut[0] = Input.GetAxis("Jump");
            continuousActionsOut[1] = Input.GetAxis("Vertical");
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
            var speed = Mathf.Clamp(contActions[0], 0.5f, 1.0f) * m_MaxSpeed;
            var dist = speed * Time.deltaTime;

            // base direction PE
            var vecPE = m_EvaderTransform.localPosition - m_Transform.localPosition;
            var theta = Mathf.Atan2(vecPE.z, vecPE.x);
            // action is u2 (control added by P)
            var actionAngle = Mathf.Clamp(contActions[1], -1.0f, 1.0f) * Mathf.PI;
            var vAngle = theta + actionAngle;

            var deltaX = dist * Mathf.Cos(vAngle);
            var deltaZ = dist * Mathf.Sin(vAngle);
            m_Transform.localPosition = m_Transform.localPosition + new Vector3(deltaX, 0.0f, deltaZ);

            // pursuer wants to make the distance as small as possible
            var distance = Vector3.Distance(m_Transform.localPosition, m_EvaderTransform.localPosition);
            AddReward(0.5f - distance / (2.0f * m_Radius));

            var vecEC = Vector3.up * 0.5f - m_EvaderTransform.localPosition;
            var vecEP = m_Transform.localPosition - m_EvaderTransform.localPosition;
            var phi = Vector3.Angle(vecEP, vecEC);
            OnStatusUpdate?.Invoke(new List<float>{speed, actionAngle, distance, phi});

            if (Vector3.Distance(m_Transform.localPosition, m_EvaderTransform.localPosition) < 1.5f)
            {
                // the pursuer catches the evador
                AddReward(1000f);
                EndEpisode();
                OnCaught?.Invoke();
                return;
            }
            else if (m_Transform.localPosition.magnitude > m_Radius)
            {
                /** out of region, but no need to decrease the reward
                * reset the position only.
                * Once we reach 15 seconds, we decrease the reward
                **/
                m_Transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
                return;
            }
            else if (Time.time - m_StartTime > 15)
            {
                // Timeout for 15 sec 
                // We didn't catch the evader in 15 sec, we lose
                OnTimeout?.Invoke(distance, phi);
                AddReward(-1000f);
                EndEpisode();
                return;
            }
        }
    }
}