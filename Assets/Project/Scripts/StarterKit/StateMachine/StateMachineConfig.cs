using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarterKit.StateMachine
{
    /// <summary>Cấu hình chuyển state hợp lệ, dùng để validate trong Editor.</summary>
    [CreateAssetMenu(fileName = "StateMachineConfig", menuName = "ScriptableObjects/StateMachineConfig")]
    public class StateMachineConfig : ScriptableObject
    {
        [Serializable]
        public class Transition
        {
            public string from;
            public string to;
            [Tooltip("Ghi chú điều kiện chuyển, chỉ để tài liệu hoá.")]
            public string condition;
        }

        public List<Transition> transitions = new List<Transition>();

        public bool IsAllowed(string from, string to)
        {
            for (int i = 0; i < transitions.Count; i++)
                if (transitions[i].from == from && transitions[i].to == to) return true;
            return false;
        }
    }
}
