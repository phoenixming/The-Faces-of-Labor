using UnityEngine;
using System;
using System.Collections.Generic;

namespace FacesOfLabor.Core
{
    public abstract class ItemBufferOwner : MonoBehaviour
    {
        [SerializeField] protected int capacity = 0;
        [SerializeField] protected ItemPromise acceptsPromise = ItemPromise.None;

        public readonly Guid Id = Guid.NewGuid();

        public static event Action<ItemBufferOwner> BufferOwnerStarted;
        public static event Action<Guid> BufferOwnerDestroyed;

        public Queue<RealItem> InputBuffer = new Queue<RealItem>();

        protected int reservedSlots = 0;
        protected int reservedItems = 0;

        // Whether this buffer owner is for pickup or dropoff
        // Either way, adding or removing items from the buffer owner is the same
        // This is a contract only, not an enforcement
        [SerializeField] protected bool forPickup;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("Debug")]
        [Tooltip("Enable debug logging for buffer operations.")]
        [SerializeField] protected bool debugPrintEnabled = false;
#endif

        public virtual int Capacity => capacity;
        public virtual ItemPromise AcceptsPromise => acceptsPromise;
        public virtual bool AcceptsInput => Capacity > 0;
        public bool IsForPickup => forPickup;

        public virtual int AvailableSlots => Capacity - InputBuffer.Count - reservedSlots;
        public virtual int AvailableItems => InputBuffer.Count - reservedItems;

        protected virtual void Start()
        {
            if (AcceptsPromise != ItemPromise.None) BufferOwnerStarted?.Invoke(this);
        }

        protected virtual void OnDestroy()
        {
            if (AcceptsPromise != ItemPromise.None) BufferOwnerDestroyed?.Invoke(Id);
        }

        public void RegisterWithTaskManager()
        {
            if (AcceptsPromise != ItemPromise.None) BufferOwnerStarted?.Invoke(this);
        }

        public void DeregisterFromTaskManager()
        {
            if (AcceptsPromise != ItemPromise.None) BufferOwnerDestroyed?.Invoke(Id);
        }

        protected void DebugPrintBufferState(string operation)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!debugPrintEnabled) return;
            Debug.Log($"[{name}] {operation} | Total: {InputBuffer.Count} | Reserved: {reservedItems} | Unreserved: {AvailableItems} | Incoming {reservedSlots} | FreeSlots: {AvailableSlots}");
#endif
        }

        public bool TryReserveSlot(int quantity = 1)
        {
            if (quantity > AvailableSlots)
                return false;

            reservedSlots += quantity;
            DebugPrintBufferState($"Reserved {quantity} slot(s)");
            return true;
        }

        public void ReleaseReservedSlots(int quantity = 1)
        {
            if (quantity > reservedSlots)
            {
                Debug.LogWarning($"[{name}] Releasing more slots ({quantity}) than reserved ({reservedSlots}). Clamping to {reservedSlots}.");
                quantity = reservedSlots;
            }
            reservedSlots -= quantity;
            DebugPrintBufferState($"Released {quantity} slot(s)");
        }

        public virtual void AddItem(RealItem item, int quantity = 1)
        {
            if (quantity > reservedSlots)
            {
                Debug.LogError($"[{name}] Adding more items ({quantity}) than reserved slots ({reservedSlots}).");
                return;
            }

            for (int i = 0; i < quantity; i++)
                InputBuffer.Enqueue(item);

            reservedSlots -= quantity;
            DebugPrintBufferState($"Added {quantity} item(s): {item}");
        }

        public bool TryReserveItem(int quantity = 1)
        {
            if (quantity > AvailableItems)
                return false;

            reservedItems += quantity;
            DebugPrintBufferState($"Reserved {quantity} item(s)");
            return true;
        }

        public void ReleaseReservedItems(int quantity = 1)
        {
            if (quantity > reservedItems)
            {
                Debug.LogWarning($"[{name}] Releasing more items ({quantity}) than reserved ({reservedItems}). Clamping to {reservedItems}.");
                quantity = reservedItems;
            }
            reservedItems -= quantity;
            DebugPrintBufferState($"Released {quantity} item(s)");
        }

        public virtual RealItem ConsumeItem(int quantity = 1)
        {
            if (quantity > reservedItems)
            {
                Debug.LogError($"[{name}] Consuming more items ({quantity}) than reserved ({reservedItems}).");
                return default;
            }

            if (quantity > InputBuffer.Count)
            {
                Debug.LogError($"[{name}] Consuming more items ({quantity}) than available in buffer ({InputBuffer.Count}).");
                return default;
            }

            var item = InputBuffer.Dequeue();
            reservedItems -= quantity;
            DebugPrintBufferState($"Consumed {quantity} item(s): {item}");
            return item;
        }

        public RealItem PeekItem()
        {
            if (InputBuffer.Count == 0)
                return default;

            return InputBuffer.Peek();
        }
    }
}
