using System;
using UnityEngine;

namespace FernGraph
{
    public enum CommentTheme
    {
        Primary = 0,
        Secondary = 1,
        Tertiary = 3
    }
       
    /// <summary>
    /// Comments placed within the CanvasView to document and group placed nodes.
    /// 
    /// These are typically ignored during runtime and only used for documentation.
    /// </summary>
    [Serializable]
    public class Comment
    {
        [SerializeField] private string text;

        /// <summary>
        /// Comment content
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }
        
        [SerializeField] private CommentTheme theme;

        /// <summary>
        /// Theme used to display the comment in CanvasView
        /// </summary>
        public CommentTheme Theme
        {
            get { return theme; }
            set { theme = value; }
        }

        [SerializeField] private Rect region;

        /// <summary>
        /// Region covered by this comment in the CanvasView
        /// </summary>
        public Rect Region
        {
            get { return region; }
            set { region = value; }
        }
    }
}
