using UnityEngine;

namespace FacesOfLabor.Core
{
    public class MaskStation : MonoBehaviour
    {
        // ItemPromise is equivalent to MaskType for masks
        public ItemPromise MaskToGive;
        public int MaskInventory;
        public int MaxMaskInventory;
        public bool HasMask => true;
        public bool CanTakeMasks;

        public bool TryGiveMask(out ItemPromise mask) { mask = ItemPromise.None; return false; }
        public bool TryTakeMask(ItemPromise mask) => false;
    }
}
