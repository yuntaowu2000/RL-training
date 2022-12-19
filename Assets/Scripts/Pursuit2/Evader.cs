using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

namespace InverseProblem
{
    public class Evader : Agent
    {
        [SerializeField] Transform m_PursuerTransform;
        [SerializeField] float m_MaxSpeed = 1f;
        [SerializeField] float m_Radius = 10f;

        private float m_Theta;
        private Transform m_Transform;

        public void Awake()
        {
            var pursuerBehavior = m_PursuerTransform.GetComponent<Pursuer>();
            pursuerBehavior.OnCaught += OnEvaderCaught;
            pursuerBehavior.OnTimeout += OnSurvive;
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
            sensor.AddObservation(new Vector2(-m_PursuerTransform.localPosition.x, m_PursuerTransform.localPosition.z));
            sensor.AddObservation(Vector3.Distance(m_Transform.localPosition, m_PursuerTransform.localPosition));
            sensor.AddObservation(Vector3.Dot(m_Transform.localPosition.normalized, m_PursuerTransform.localPosition.normalized));
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            var contActions = actions.ContinuousActions;
            m_Theta += Mathf.Clamp(contActions[0], -1.0f, 1.0f) * m_MaxSpeed * Time.deltaTime;
            m_Theta = m_Theta % (2 * Mathf.PI);
            m_Transform.localPosition = new Vector3(m_Radius * Mathf.Cos(m_Theta), 0.5f, m_Radius * Mathf.Sin(m_Theta));
            
            // evader wants to make the distance as large as possible
            var distance = Vector3.Distance(m_Transform.localPosition, m_PursuerTransform.localPosition);
            AddReward(distance / (2.0f * m_Radius) - 0.5f);
        }

        public void OnEvaderCaught()
        {
            AddReward(-1000f);
            EndEpisode();
        }

        public void OnSurvive(float distance, float phi)
        {
            AddReward(1000f);
            EndEpisode();
        }
    }
}
