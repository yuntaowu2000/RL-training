using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace OriginalProblem
{
    public class InferenceUI : MonoBehaviour
    {
        [SerializeField] TMP_Text m_TextBox;
        [SerializeField] Transform m_PursuerTransform;
        [SerializeField] Transform m_EvaderTransform;

        private int m_TimesCaught = 0;
        private int m_TimesEscaped = 0;
        private List<float> m_TerminalThetas;
        private float m_AvgTerminalTheta = 0.0f;
        private List<float> m_DecisionBoundaryThetas;
        private float m_AvgDBTheta = 0.0f;

        void Start()
        {
            m_TerminalThetas = new List<float>();
            m_DecisionBoundaryThetas = new List<float>();
            if (m_EvaderTransform.TryGetComponent<Evader>(out var evader))
            {
                // original problem
                evader.OnCaught += OnEvaderCaught;
                evader.OnEscaped += OnEvaderEscaped;
                evader.OnDecisionBoundaryReached += OnDecisionBoundaryReached;
            }
        }

        void FixedUpdate()
        {
            // Players take actions on fixed updates
            float currentTheta = Vector3.Angle(m_EvaderTransform.localPosition, m_PursuerTransform.localPosition);
            string text = $"Current theta: {currentTheta:.##}\n"
                        + $"Avg term theta: {m_AvgTerminalTheta:.##}\n"
                        + $"Avg DB theta: {m_AvgDBTheta:.##}\n"
                        + $"Times caught: {m_TimesCaught}\n"
                        + $"Times escaped: {m_TimesEscaped}\n";
            m_TextBox.text = text;
        }

        private void OnEvaderCaught(float angle)
        {
            m_TimesCaught += 1;
            m_TerminalThetas.Add(angle);
            m_AvgTerminalTheta = m_TerminalThetas.Average();
        }

        private void OnEvaderEscaped(float angle)
        {
            m_TimesEscaped += 1;
            m_TerminalThetas.Add(angle);
            m_AvgTerminalTheta = m_TerminalThetas.Average();
        }

        private void OnDecisionBoundaryReached()
        {
            m_DecisionBoundaryThetas.Add(Vector3.Angle(m_EvaderTransform.localPosition, m_PursuerTransform.localPosition));
            m_AvgDBTheta = m_DecisionBoundaryThetas.Average();
        }
    }
}