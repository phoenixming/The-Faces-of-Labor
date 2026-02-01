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

        public bool TryReserveSlot(int quantity = 1)
        {
            if (quantity > AvailableSlots)
                return false;

            reservedSlots += quantity;
            return true;
        }

        public void ReleaseReservedSlots(int quantity = 1)
        {
            reservedSlots = Mathf.Max(0, reservedSlots - quantity);
        }

        public virtual void AddItem(RealItem item, int quantity = 1)
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

        public bool TryReserveItem(int quantity = 1)
        {
            if (quantity > AvailableItems)
                return false;

            reservedItems += quantity;
            return true;
        }

        public void ReleaseReservedItems(int quantity = 1)
        {
            reservedItems = Mathf.Max(0, reservedItems - quantity);
        }

        public virtual RealItem ConsumeItem(int quantity = 1)
        {
            if (quantity > reservedItems)
            {
                Debug.LogError($"Consuming more items ({quantity}) than reserved ({reservedItems}).");
                return default;
            }

            Debug.Log($"[{this}] Consuming {quantity} item(s).");

            var item = InputBuffer.Dequeue();
            reservedItems -= quantity;

            Debug.Log($"[{this}] Available items after consume: {AvailableItems}");
            Debug.Log($"[{this}] Available slots after consume: {AvailableSlots}");

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
