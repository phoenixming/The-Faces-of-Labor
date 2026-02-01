using UnityEngine;
using System;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Base class for all infrastructure entities (workstations, stations, etc.).
    ///
    /// Responsibilities:
    /// - Broadcast lifecycle events for system integration
    ///
    /// Design:
    /// - GUID-based identification inherited from ItemBufferOwner
    /// - Static events for clean decoupling between infrastructure and systems
    /// - Systems (GridSystem, TaskManager) subscribe to events in Awake
    /// </summary>
    public abstract class Infrastructure : ItemBufferOwner
    {
        public static event Action<Infrastructure> InfrastructureStarted;
        public static event Action<Guid> InfrastructureDestroyed;

        protected virtual void Awake() { }

        protected override void Start()
        {
            base.Start();
            InfrastructureStarted?.Invoke(this);
        }

        protected override void OnDestroy()
        {
            InfrastructureDestroyed?.Invoke(Id);
            base.OnDestroy();
        }
    }
}
