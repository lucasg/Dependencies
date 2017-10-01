namespace Dragablz
{
    /// <summary>
    /// Provide a hint for how the header should size itself if there are no tabs left (and a Window is still open).
    /// </summary>
    public enum EmptyHeaderSizingHint
    {
        /// <summary>
        /// The header size collapses to zero along the correct axis.
        /// </summary>
        Collapse,
        /// <summary>
        /// The header size remains that of the last tab prior to the tab header becoming empty.
        /// </summary>
        PreviousTab,
        //TODO implement EmptyHeaderSizingHint.Stretch        
        /// <summary>
        /// The header stretches along the according axis.
        /// </summary>
        //Stretch
    }
}