using UnityEngine;

namespace FacesOfLabor.Core
{
    public enum MaskType
    {
        None,
        Farmer,
        Chef,
        Carrier
    }

    public class MaskStation : MonoBehaviour
    {
        public MaskType MaskToGive;
        public int MaskInventory;
        public int MaxMaskInventory;
        public bool HasMask => true;
        public bool CanTakeMasks;

        public bool TryGiveMask(out MaskType mask) { mask = MaskType.None; return false; }
        public bool TryTakeMask(MaskType mask) => false;
    }
}
