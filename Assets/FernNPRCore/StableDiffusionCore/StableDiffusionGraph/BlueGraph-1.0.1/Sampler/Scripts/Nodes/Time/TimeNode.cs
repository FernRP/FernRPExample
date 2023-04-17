using BlueGraph;
using UnityEngine;

namespace BlueGraphSamples
{
    /// <summary>
    /// Expose Time information from Unity
    /// </summary>
    [Node("Time", Path = "Utility")]
    [Tags("Utility")]
    [Output("deltaTime", typeof(float))]
    [Output("fixedDeltaTime", typeof(float))]
    [Output("realtimeSinceStartup", typeof(float))]
    [Output("timeSinceLevelLoad", typeof(float))]
    public class TimeNode : Node
    {
        public override object OnRequestValue(Port port)
        {
            switch (port.Name)
            {
                case "deltaTime":
                    return Time.deltaTime;
                case "fixedDeltaTime":
                    return Time.fixedDeltaTime;
                case "realtimeSinceStartup":
                    return Time.realtimeSinceStartup;
                case "timeSinceLevelLoad":
                    return Time.timeSinceLevelLoad;
                default:
                    return null;
            }
        }
    }
}
