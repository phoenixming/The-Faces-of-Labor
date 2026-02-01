using UnityEngine;
using FacesOfLabor.Core;

namespace FacesOfLabor.Demo
{
    public class DemoManager : MonoBehaviour
    {
        public static DemoManager Instance { get; private set; }

        [Header("Task Configuration")]
        [Tooltip("List of task definitions to schedule on start.")]
        public TaskDefinition[] oneTimeTasks;
        public TaskDefinition[] foreverTasks;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            ScheduleOneTimeTasks();
            ScheduleForeverTasks();
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
