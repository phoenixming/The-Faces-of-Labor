using UnityEngine;
using System;

namespace FacesOfLabor.Core
{
    public class Station : MonoBehaviour
    {
        public WorkstationType WorkstationType;
        public GridCoordinate GridPosition;

        public event Action<Station> OnStationEnabled;
        public event Action<Station> OnStationDisabled;

        protected virtual void Awake() { }
        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
        protected virtual void Start() { }

        public virtual void Initialize(GridCoordinate position) { }
        public virtual bool CanAcceptItem(ItemType itemType, string npcMask) => false;
        public virtual bool TryConsumeItem(ItemType itemType) => false;
    }

    public readonly struct GridCoordinate
    {
        public static readonly GridCoordinate zero = new GridCoordinate(0, 0);
        public readonly int x;
        public readonly int y;
        public GridCoordinate(int x, int y) { this.x = x; this.y = y; }
    }
}
