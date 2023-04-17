using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlueGraphSamples
{
    /// <summary>
    /// Executes matching MonoBehaviourGraph events on MonoBehaviour events.
    /// </summary>
    public class UseMonoBehaviourGraph : MonoBehaviour
    {
        public MonoBehaviourGraph graph;

        private void OnEnable()
        {
            graph.OnBehaviourEnable();
        }

        private void OnDisable()
        {
            graph.OnBehaviourDisable();
        }

        void Start()
        {
            graph.OnBehaviourStart();
        }

        void Update()
        {
            graph.OnBehaviourUpdate();
        }
    }
}
