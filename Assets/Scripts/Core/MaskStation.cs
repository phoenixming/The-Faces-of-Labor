using UnityEngine;
using System.Collections.Generic;

namespace FacesOfLabor.Core
{
    public class MaskStation : Infrastructure
    {
        // Defines the *expect* mask type that this station can give.
        public ItemPromise MaskLabel;
        // Defines the queue of masks that this station has. May not match MaskLabel.
        public Queue<ItemPromise> MaskQueue = new Queue<ItemPromise>();
        public int MaskCapacity;

        public bool TryPutMask(ItemPromise mask)
        {
            if (MaskQueue.Count >= MaskCapacity)
            {
                return false;
            }

            MaskQueue.Enqueue(mask);
            return true;
        }

        public bool TryGetMask(out ItemPromise mask)
        {
            mask = ItemPromise.None;
            if (MaskQueue.Count == 0)
            {
                return false;
            }

            mask = MaskQueue.Dequeue();
            return true;
        }
    }
}
