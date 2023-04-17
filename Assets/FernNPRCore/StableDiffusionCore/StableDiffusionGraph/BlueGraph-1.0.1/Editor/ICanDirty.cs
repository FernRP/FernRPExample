namespace BlueGraph.Editor
{
    /// <summary>
    /// Objects that can be dirtied by canvas changes and later updated in response. 
    /// </summary>
    public interface ICanDirty
    {
        /// <summary>
        /// Called when the Canvas dirties this object
        /// </summary>
        void Dirty();

        /// <summary>
        /// Called when the canvas iterates through dirtied objects during an update loop
        /// </summary>
        void Update();
    }
}
