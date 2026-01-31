using UnityEngine;

namespace FacesOfLabor.Core
{
    public class ProductionStation : Station
    {
        public int InputBufferCount => 0;
        public int OutputBufferCount => 0;
        public int InputBufferCapacity;
        public int OutputBufferCapacity;
        public bool HasInputSpace => true;
        public bool HasOutputSpace => true;

        protected override void Awake() { }

        public bool AddInput(ItemType itemType) => false;
        public bool TryConsumeInput(out ItemType itemType) { itemType = ItemType.None; return false; }
        public bool AddOutput(ItemType itemType) => false;
        public bool TryConsumeOutput(out ItemType itemType) { itemType = ItemType.None; return false; }
    }
}
