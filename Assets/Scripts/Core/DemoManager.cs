using UnityEngine;

namespace FacesOfLabor.Core
{
    public class DemoManager : MonoBehaviour
    {
        public static DemoManager Instance { get; private set; }

        public int NpcCount;
        public float HungerDecayRate;

        private void Awake() { }
        private void Start() { }
        private void Update() { }

        private void CreateStations() { }
        private void CreateNPCs() { }
        private void CreateInitialJobs() { }
        private void SpawnProductionJobs() { }
        private void SpawnDeliveryJobs() { }
    }
}
