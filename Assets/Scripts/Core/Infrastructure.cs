using UnityEngine;
using System;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Base class for all infrastructure entities (workstations, stations, etc.).
    ///
    /// Responsibilities:
    /// - Provide GUID-based identification for event-based registration
    /// - Broadcast lifecycle events for system integration
    ///
    /// Design:
    /// - GUID generated in constructor for unique identification
    /// - Static events for clean decoupling between infrastructure and systems
    /// - Systems (GridSystem, TaskManager) subscribe to events in Awake
    /// </summary>
    public abstract class Infrastructure : MonoBehaviour
    {
        [Tooltip("Unique identifier for event-based registration.")]
        public readonly Guid Id = Guid.NewGuid();

        public static event Action<Infrastructure> InfrastructureStarted;
        public static event Action<Guid> InfrastructureDestroyed;

        protected virtual void Awake() { }

        protected virtual void Start()
        {
            InfrastructureStarted?.Invoke(this);
        }

        protected virtual void OnDestroy()
        {
            InfrastructureDestroyed?.Invoke(Id);
        }
    }
}
