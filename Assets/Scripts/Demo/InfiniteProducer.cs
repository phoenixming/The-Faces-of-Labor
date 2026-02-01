using UnityEngine;
using FacesOfLabor.Core;

namespace FacesOfLabor.Demo
{
    /// <summary>
    /// Infinite producer for testing purposes.
    /// Always has available items to pick up.
    /// </summary>
    public class InfiniteProducer : Infrastructure
    {
        [Header("Configuration")]
        [Tooltip("Base item type produced by this producer.")]
        public BaseItemType ProducesBaseItem;

        [Tooltip("Processing state of produced items.")]
        public ProcessingState ProducesState = ProcessingState.Raw;

        public override int Capacity => 0;
        public override bool AcceptsInput => false;

        public override int AvailableItems => int.MaxValue;

        protected override void Awake()
        {
            base.Awake();
            forPickup = true;
        }

        public override RealItem ConsumeItem(int quantity = 1)
        {
            var item = new RealItem
            {
                BaseType = ProducesBaseItem,
                State = ProducesState
            };
            Debug.Log($"[InfiniteProducer] Produced item: {item.BaseType} ({item.State})");
            return item;
        }
    }
}