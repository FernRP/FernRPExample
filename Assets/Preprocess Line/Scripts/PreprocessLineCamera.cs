using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PreprocessLine {
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class PreprocessLineCamera : MonoBehaviour {
        private CommandBuffer command;
        private Camera thisCamera;
        private string commandName = "Render Cartoon Line";
        private CameraEvent cameraEvent = CameraEvent.AfterForwardOpaque;

        private void OnEnable() {
            thisCamera = GetComponent<Camera>();
            command = new CommandBuffer();
            command.name = commandName;
            thisCamera.AddCommandBuffer(cameraEvent, command);
    #if UNITY_EDITOR
            Camera.onPreCull += DrawWithCamera;
    #endif
        }

        private void OnDisable() {
            if (command != null) {
                thisCamera.RemoveCommandBuffer(cameraEvent, command);
                command.Release();
                command = null;
            }
    #if UNITY_EDITOR
            Camera.onPreCull -= DrawWithCamera;
            for (int i = 0; i < Camera.allCamerasCount; i++) {
                Camera camera = Camera.allCameras[i];
                if (camera.name != "SceneCamera" && camera.name != name) {
                    continue;
                }

                CommandBuffer[] commandBuffers = camera.GetCommandBuffers(cameraEvent);
                for (int j = 0; j < commandBuffers.Length; j++) {
                    if (commandBuffers[j].name == commandName) {
                        Camera.allCameras[i].RemoveCommandBuffer(cameraEvent, commandBuffers[j]);
                        commandBuffers[j].Release();
                    }
                }
            }
    #endif
        }

    #if UNITY_EDITOR
        private void DrawWithCamera(Camera camera) {
            if (camera.name == "SceneCamera" || camera.name == name) {
                CommandBuffer targetCommand = null;
                CommandBuffer[] commandBuffers = camera.GetCommandBuffers(cameraEvent);
                for (int j = 0; j < commandBuffers.Length; j++) {
                    if (commandBuffers[j].name == commandName) {
                        targetCommand = commandBuffers[j];
                        break;
                    }
                }

                if (targetCommand == null) {
                    targetCommand = new CommandBuffer();
                    targetCommand.name = commandName;
                    camera.AddCommandBuffer(cameraEvent, targetCommand);
                }
                targetCommand.Clear();
                DrawLine(targetCommand);
            }
        }
    #else
        private void LateUpdate() {
            command.Clear();
            DrawLine(command);
        }
    #endif

        private void DrawLine(CommandBuffer command) {
            var cartoonLines = PreprocessLineCore.GetCollection();
            foreach (var line in cartoonLines) {
                if (line.IsVisible()) {
                    line.Draw(command);
                }
            }
        }
    }
}
