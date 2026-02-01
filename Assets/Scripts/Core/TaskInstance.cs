using UnityEngine;
using System;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Runtime instance of a task being managed by TaskManager.
    /// Created when a TaskDefinition is instantiated for execution.
    ///
    /// Responsibilities:
    /// - Track task state (Pending, Ready, Claimed, Executing)
    /// - Hold reference to TaskDefinition for parameters
    /// - Track assigned workstation and NPC
    ///
    /// Design:
    /// - Created by TaskManager when adding tasks
    /// - Immutable definition reference, mutable runtime state
    /// - State transitions: Pending → Ready → Claimed → Executing
    /// </summary>
    public class TaskInstance
    {
        public readonly TaskDefinition Definition;
        public readonly Guid Id;

        public TaskType Type => Definition.Type;
        public int RepeatCount = 0;

        public TaskState State { get; private set; }
        public WorkStation WorkStation { get; private set; }
        public bool HasWorkStation => WorkStation != null;

        public ItemBufferOwner PickupEntity { get; private set; }
        public ItemBufferOwner DropoffEntity { get; private set; }
        public bool HasPickupEntity => PickupEntity != null;
        public bool HasDropoffEntity => DropoffEntity != null;

        public TaskInstance(TaskDefinition definition, int repeatCount = 0)
        {
            Definition = definition;
            Id = Guid.NewGuid();
            RepeatCount = repeatCount;
            State = TaskState.Pending;
            WorkStation = null;
        }

        public void TransitionTo(TaskState newState)
        {
            State = newState;
            Debug.Log($"Task {Id} transitioned to state {State}");
        }

        public TaskInstance Repeat()
        {
            if (RepeatCount > 0)
            {
                return new TaskInstance(Definition, RepeatCount - 1)
                {
                    WorkStation = this.WorkStation,
                    PickupEntity = this.PickupEntity,
                    DropoffEntity = this.DropoffEntity
                };
            } else {
                return null;
            }
        }

        public void AssignWorkStation(WorkStation station)
        {
            WorkStation = station;
        }

        public void ClearWorkStation()
        {
            WorkStation = null;
        }

        public void AssignPickupEntity(ItemBufferOwner entity)
        {
            PickupEntity = entity;
        }

        public void AssignDropoffEntity(ItemBufferOwner entity)
        {
            DropoffEntity = entity;
        }

        public void ClearDeliveryEntities()
        {
            PickupEntity = null;
            DropoffEntity = null;
        }
    }
}
