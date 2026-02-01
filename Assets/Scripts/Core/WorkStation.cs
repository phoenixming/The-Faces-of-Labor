using UnityEngine;
using System;
using System.Collections.Generic;

namespace FacesOfLabor.Core
{
    /// <summary>
    /// Base class for all workstations in the colony.
    /// Acts as a thin container referencing a WorkStationDefinition.
    ///
    /// Responsibilities:
    /// - Hold reference to WorkStationDefinition for physical constraints
    /// - Manage input buffer with reservation system for slots and items
    ///
    /// Design:
    /// - Scene objects are thin containers around ScriptableObject definitions
    /// - Workstations never decide WHAT happens to items — only WHETHER items may be present
    /// - See WorkStationDefinition for capacity and acceptance rules
    /// - Reservation system ensures atomicity: reserve before add/consume
    /// </summary>
    public class WorkStation : Infrastructure
    {
        [Header("Configuration")]
        [Tooltip("Workstation definition specifying physical constraints.")]
        [SerializeField] private WorkStationDefinition definition;

        [Header("Runtime State")]
        [Tooltip("Grid cell this workstation occupies.")]
        public Vector2Int GridPosition;

        [Tooltip("Input items currently in the buffer.")]
        public Queue<RealItem> InputBuffer = new Queue<RealItem>();

        public WorkStationDefinition Definition => definition;
        public WorkstationType Type => definition?.Type ?? WorkstationType.None;
        public int Capacity => definition?.InputBufferSize ?? 0;
        public bool AcceptsInput => Capacity > 0;
        public ItemPromise AcceptsPromise => definition?.AcceptsInput ?? ItemPromise.None;

        private int reservedSlots = 0;
        private int reservedItems = 0;

        public int AvailableSlots => Capacity - InputBuffer.Count - reservedSlots;
        public int AvailableItems => InputBuffer.Count - reservedItems;

        public void SetDefinition(WorkStationDefinition newDefinition)
        {
            definition = newDefinition;
        }

        #region Slot Reservations (for incoming deliveries)

        /// <summary>
        /// Reserve free slots for items being delivered to this workstation.
        /// Call before adding items. Prevents buffer overflow.
        /// </summary>
        /// <param name="quantity">Number of slots to reserve.</param>
        /// <returns>True if reservation succeeded.</returns>
        public bool TryReserveSlot(int quantity = 1)
        {
            if (quantity > AvailableSlots)
                return false;

            reservedSlots += quantity;
            return true;
        }

        /// <summary>
        /// Add items from a delivery. Must have reserved slots first.
        /// </summary>
        public void AddItem(RealItem item, int quantity = 1)
        {
            if (quantity > reservedSlots)
            {
                Debug.LogError($"Adding more items ({quantity}) than reserved slots ({reservedSlots}).");
                return;
            }

            for (int i = 0; i < quantity; i++)
                InputBuffer.Enqueue(item);

            reservedSlots -= quantity;
        }

        #endregion

        #region Item Reservations (for task consumption)

        /// <summary>
        /// Reserve existing items for a task to consume.
        /// Call before consuming items. Ensures atomicity.
        /// </summary>
        /// <param name="quantity">Number of items to reserve.</param>
        /// <returns>True if reservation succeeded.</returns>
        public bool TryReserveItem(int quantity = 1)
        {
            int available = InputBuffer.Count - reservedItems;
            if (quantity > available)
                return false;

            reservedItems += quantity;
            return true;
        }

        /// <summary>
        /// Consume reserved items for a task. Returns the consumed items.
        /// </summary>
        public RealItem ConsumeItem(int quantity = 1)
        {
            if (quantity > reservedItems)
            {
                Debug.LogError($"Consuming more items ({quantity}) than reserved ({reservedItems}).");
                return default;
            }

            var item = InputBuffer.Dequeue();
            reservedItems -= quantity;
            return item;
        }

        #endregion
    }
}
