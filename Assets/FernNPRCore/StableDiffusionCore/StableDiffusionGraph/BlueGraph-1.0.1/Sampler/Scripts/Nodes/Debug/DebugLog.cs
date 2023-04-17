using UnityEngine;
using BlueGraph;

namespace BlueGraphSamples
{
    public enum LogMode
    {
        Debug,
        Warning,
        Error
    };

    [Node(Path = "Debug")]
    [Tags("Executable")]
    public class DebugLog : ExecutableNode 
    { 
        [Input] public string message;
        [Input(Editable = false)] public object obj;
        [Input] public Object context;

        [Editable] public LogMode mode;

        public override IExecutableNode Execute(ExecutionFlowData data)
        {
            string msg = GetInputValue("message", message);
            object obj = GetInputValue("obj", this.obj);
            Object context = GetInputValue("context", this.context);

            if (obj != null)
            {
                msg += obj.ToString();
            }
            
            switch (mode)
            { 
                case LogMode.Debug:
                    Debug.Log(msg, context);
                    break;
                case LogMode.Warning:
                    Debug.LogWarning(msg, context);
                    break;
                case LogMode.Error:
                    Debug.LogError(msg, context);
                    break;
                default: break;
            }

            return base.Execute(data);
        }
    }
}
