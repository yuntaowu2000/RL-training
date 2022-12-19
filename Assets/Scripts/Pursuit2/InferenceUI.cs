using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

namespace InverseProblem
{
    public class InferenceUI : MonoBehaviour
    {
        [SerializeField] TMP_Text m_TextBox;
        [SerializeField] Transform m_PursuerTransform;
        private int m_TimesCaught = 0;
        private int m_TimesSurvived = 0;

        private List<float> m_PursuerSpeeds;
        private List<float> m_AvgPursuerSpeeds;
        private float m_MeanAvgPursuerSpeed = 0.0f;

        private List<float> m_PursuerDirections;
        private List<float> m_AvgPursuerDirections;
        private float m_MeanAvgPursuerDirection = 0.0f;
        
        private List<float> m_TerminalDistances;
        private float m_MeanTerminalDistance = 0.0f;

        private List<float> m_TerminalPhis;
        private float m_MeanTerminalPhi = 0.0f;

        // Start is called before the first frame update
        void Start()
        {
            m_PursuerSpeeds = new List<float>();
            m_PursuerDirections = new List<float>();
            m_AvgPursuerSpeeds = new List<float>();
            m_AvgPursuerDirections = new List<float>();
            m_TerminalDistances = new List<float>();
            m_TerminalPhis = new List<float>();

            if (m_PursuerTransform.TryGetComponent<Pursuer>(out var pursuer))
            {
                pursuer.OnCaught += OnCaught;
                pursuer.OnTimeout += OnTimeout;
                pursuer.OnStatusUpdate += OnStatusUpdate;
            }
        }

        private void OnStatusUpdate(List<float> values)
        {
            var speed = values[0];
            var direction = values[1] / Mathf.PI * 180.0f;
            var distance = values[2];
            var phi = values[3];
            m_PursuerSpeeds.Add(speed);
            m_PursuerDirections.Add(direction);
            string text = $"Current Pursuer speed: {speed:.##}\n"
                        + $"Current Pursuer direction: {direction:.##}\n"
                        + $"Current distance: {distance:.##}\n"
                        + $"Current phi: {phi:.##}\n"
                        + $"Times Caught: {m_TimesCaught}\n"
                        + $"Times Survived: {m_TimesSurvived}\n"
                        + $"Avg Pursuer speed: {m_MeanAvgPursuerSpeed:.##}\n"
                        + $"Avg Pursuer direction: {m_MeanAvgPursuerDirection:.##}\n"
                        + "When not caught:\n"
                        + $"Avg terminal distance: {m_MeanTerminalDistance:.##}\n"
                        + $"Avg terminal phi: {m_MeanTerminalPhi:.##}\n";
            m_TextBox.text = text;
        }

        private void OnCaught()
        {
            CalculateAvgVelocityAndDirections();
            m_TimesCaught += 1;
        }

        private void OnTimeout(float distance, float phi)
        {
            CalculateAvgVelocityAndDirections();
            m_TimesSurvived += 1;
            m_TerminalDistances.Add(distance);
            m_TerminalPhis.Add(phi);
            m_MeanTerminalDistance = m_TerminalDistances.Average();
            m_MeanTerminalPhi = m_TerminalPhis.Average();
        }

        private void CalculateAvgVelocityAndDirections()
        {
            m_AvgPursuerSpeeds.Add(m_PursuerSpeeds.Average());
            m_AvgPursuerDirections.Add(m_PursuerDirections.Average());
            m_MeanAvgPursuerSpeed = m_AvgPursuerSpeeds.Average();
            m_MeanAvgPursuerDirection = m_AvgPursuerDirections.Average();

            // reset for each episode
            m_PursuerSpeeds = new List<float>();
            m_PursuerDirections = new List<float>();
        }
    }
}
