using UnityEngine;
using FacesOfLabor.Core;

namespace FacesOfLabor.Demo
{
    /// <summary>
    /// Infinite sink for testing purposes.
    /// Accepts any items and destroys them.
    /// Always has available slots for delivery.
    /// </summary>
    public class InfiniteSink : Infrastructure
    {
        [Header("Configuration")]
        public override int Capacity => int.MaxValue;
        public override bool AcceptsInput => true;
        public override int AvailableSlots => int.MaxValue;

        protected override void Awake()
        {
            base.Awake();
            forPickup = false;
        }

        public override void AddItem(RealItem item, int quantity = 1)
        {
            Debug.Log($"[InfiniteSink] Received item: {item.BaseType} ({item.State})");
        }
    }
}