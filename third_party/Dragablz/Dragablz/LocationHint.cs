namespace Dragablz
{
    /// <summary>
    /// Specifies where an item should appear when added to tab control, as the headers order do not
    /// specifically correlate to the order of the the source items.
    /// </summary>
    public enum AddLocationHint
    {        
        /// <summary>
        /// Display item in the first position.
        /// </summary>
        First,
        /// <summary>
        /// Display item in the first position.
        /// </summary>
        Last,
        /// <summary>
        /// Display an item prior to the selected, or specified item.
        /// </summary>
        Prior,
        /// <summary>
        /// Display an item after the selected, or specified item.
        /// </summary>
        After
    }
}