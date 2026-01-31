using UnityEngine;

namespace FacesOfLabor.Core
{
    public class ConsumerStation : Station
    {
        public int InputBufferCount => 0;
        public int InputBufferCapacity;
        public bool HasInputSpace => true;

        protected override void Awake() { }

        public override bool CanAcceptItem(ItemType itemType, string npcMask) => false;
        public bool TryDeliverItem(ItemType itemType, string npcMask) => false;
        public override bool TryConsumeItem(ItemType itemType) => false;
    }
}
