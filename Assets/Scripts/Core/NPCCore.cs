using UnityEngine;
using System;

namespace FacesOfLabor.Core
{
    public class NPCCore : MonoBehaviour
    {
        public enum NPCState
        {
            Idle,
            MovingToJob,
            ExecutingJob,
            MovingToDelivery,
            Delivering,
            Eating,
            Dead
        }

        // public JobInstance CurrentJob;
        public MaskType CurrentMask;
        // public ItemType CarriedItem;
        public NPCState State;
        public bool IsDead;
        public bool IsIdle;
        public float Hunger;
        public float MaxHunger;

        public event Action<NPCCore> OnDied;
        // public event Action<NPCCore, JobInstance> OnJobClaimed;
        // public event Action<NPCCore, JobInstance> OnJobCompleted;

        protected virtual void Awake() { }
        protected virtual void Start() { }
        protected virtual void Update() { }

        // public void SetPosition(GridCoordinate coordinate) { }
        public void SetMask(MaskType mask) { }
    }
}
