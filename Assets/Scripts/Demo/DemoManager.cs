using UnityEngine;
using FacesOfLabor.Core;
using System.Collections.Generic;
using System;

namespace FacesOfLabor.Demo
{
    [Serializable]
    public struct MaskStationConfig
    {
        public MaskStation Station;
        public int InitialMaskCount;
    }

    public class DemoManager : MonoBehaviour
    {
        public static DemoManager Instance { get; private set; }

        [Header("Task Configuration")]
        [Tooltip("List of task definitions to schedule on start.")]
        public TaskDefinition[] oneTimeTasks;
        public TaskDefinition[] foreverTasks;

        [Header("Mask Station Configuration")]
        [Tooltip("List of mask stations with desired initial mask counts.")]
        public List<MaskStationConfig> maskStationConfigs;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            InitializeMaskStations();
            ScheduleOneTimeTasks();
            ScheduleForeverTasks();
        }

        private void InitializeMaskStations()
        {
            if (maskStationConfigs == null || maskStationConfigs.Count == 0)
            {
                Debug.Log("[DemoManager] No mask station configurations provided.");
                return;
            }

            foreach (var config in maskStationConfigs)
            {
                if (config.Station == null)
                {
                    Debug.LogWarning("[DemoManager] MaskStationConfig has null station reference.");
                    continue;
                }

                for (int i = 0; i < config.InitialMaskCount; i++)
                {
                    ItemPromise mask = config.Station.MaskLabel;
                    if (config.Station.TryPutMask(mask))
                    {
                        Debug.Log($"[DemoManager] Added initial mask to {config.Station.name}: {mask}");
                    }
                    else
                    {
                        Debug.LogWarning($"[DemoManager] Could not add mask to {config.Station.name} (queue full).");
                        break;
                    }
                }
            }

            Debug.Log($"[DemoManager] Initialized {maskStationConfigs.Count} mask stations.");
        }

        private void ScheduleOneTimeTasks()
        {
            if (oneTimeTasks == null || oneTimeTasks.Length == 0)
            {
                Debug.Log($"[DemoManager] No initial tasks configured.");
                return;
            }

            TaskManager taskManager = TaskManager.Instance;
            if (taskManager == null)
            {
                Debug.LogError("[DemoManager] TaskManager instance not found.");
                return;
            }

            Debug.Log($"[DemoManager] Scheduling {oneTimeTasks.Length} initial tasks...");

            foreach (var taskDefinition in oneTimeTasks)
            {
                if (taskDefinition != null)
                {
                    taskManager.AddTask(taskDefinition);
                    Debug.Log($"[DemoManager] Scheduled task: {taskDefinition.DisplayName} ({taskDefinition.Type})");
                }
            }

            Debug.Log($"[DemoManager] Finished scheduling initial tasks.");
        }

        private void ScheduleForeverTasks()
        {
            if (foreverTasks == null || foreverTasks.Length == 0)
            {
                Debug.Log($"[DemoManager] No forever tasks configured.");
                return;
            }

            TaskManager taskManager = TaskManager.Instance;
            if (taskManager == null)
            {
                Debug.LogError("[DemoManager] TaskManager instance not found.");
                return;
            }

            Debug.Log($"[DemoManager] Scheduling {foreverTasks.Length} forever tasks...");

            foreach (var taskDefinition in foreverTasks)
            {
                // Must create a forever task first
                TaskInstance tempTask = new TaskInstance(taskDefinition, repeatCount: int.MaxValue);
                if (taskDefinition != null)
                {
                    taskManager.AddTask(tempTask);
                    Debug.Log($"[DemoManager] Scheduled forever task: {taskDefinition.DisplayName} ({taskDefinition.Type})");
                }
            }

            Debug.Log($"[DemoManager] Finished scheduling forever tasks.");
        }
    }
}
