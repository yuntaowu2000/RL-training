using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

namespace OriginalProblem
{
    public class Evader : Agent
    {
        [SerializeField] Transform m_PursuerTransform;
        [SerializeField] float m_MaxSpeed = 1f;
        public float MaxSpeed => m_MaxSpeed;
        [SerializeField] float m_Radius = 10f;

        private Transform m_Transform;
        private float m_DecisionBoundary;
        public event Action<float> OnEscaped;
        public event Action<float> OnCaught;
        public event Action OnTimeout;
        public event Action OnDecisionBoundaryReached;
        private bool m_HasReachedDecisionBoundary = false;
        private float m_StartTime;

        public void Awake()
        {
            var pursuerBehavior = m_PursuerTransform.GetComponent<Pursuer>();
            m_DecisionBoundary = m_MaxSpeed / pursuerBehavior.MaxSpeed;
        }

        public override void Initialize()
        {
            m_Transform = transform;
            m_Transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
            m_HasReachedDecisionBoundary = false;
        }

        public override void OnEpisodeBegin()
        {
            m_Transform.localPosition = new Vector3(0.0f, 0.5f, 0.0f);
            m_HasReachedDecisionBoundary = false;
            m_StartTime = Time.time;
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            var continuousActionsOut = actionsOut.ContinuousActions;
            continuousActionsOut[0] = Input.GetAxis("Vertical");
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(new Vector2(m_Transform.localPosition.x, m_Transform.localPosition.z));
            sensor.AddObservation(new Vector2(m_PursuerTransform.localPosition.x, m_PursuerTransform.localPosition.z));
            sensor.AddObservation(Vector3.Distance(m_Transform.localPosition, m_PursuerTransform.localPosition));
            sensor.AddObservation(Vector3.Dot(m_Transform.localPosition.normalized, m_PursuerTransform.localPosition.normalized));
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var contActions = actions.ContinuousActions;
            var dist = m_MaxSpeed * Time.deltaTime;

            // base direction CE
            var theta = Mathf.Atan2(m_Transform.localPosition.z, m_Transform.localPosition.x);
            // action is u2 (control added by E)
            var vAngle = theta + Mathf.Clamp(contActions[0], -1.0f, 1.0f) * Mathf.PI;

            var deltaX = dist * Mathf.Cos(vAngle);
            var deltaZ = dist * Mathf.Sin(vAngle);
            m_Transform.localPosition = m_Transform.localPosition + new Vector3(deltaX, 0.0f, deltaZ);

            // evader wants to make the angle as large as possible
            var angle = Vector3.Dot(m_Transform.localPosition.normalized, m_PursuerTransform.localPosition.normalized);
            AddReward(-angle);

            if (m_Transform.localPosition.magnitude >= m_DecisionBoundary && !m_HasReachedDecisionBoundary)
            {
                // for inference UI only
                OnDecisionBoundaryReached?.Invoke();
                m_HasReachedDecisionBoundary = true;
            }

            if (Vector3.Distance(m_Transform.localPosition, m_PursuerTransform.localPosition) < 1.5f)
            {
                // the pursuer catches the evador
                AddReward(-1000f);
                OnCaught?.Invoke(Vector3.Angle(m_Transform.localPosition, m_PursuerTransform.localPosition));
                EndEpisode();
                return;
            }
            else if (m_Transform.localPosition.magnitude >= m_Radius)
            {
                AddReward(1000f);
                OnEscaped?.Invoke(Vector3.Angle(m_Transform.localPosition, m_PursuerTransform.localPosition));
                EndEpisode();
                return;
            }
            else if (Time.time - m_StartTime > 600)
            {
                // Timeout for 10 min without winner.
                EndEpisode();
                OnTimeout?.Invoke();
                return;
            }
        }
    }
}