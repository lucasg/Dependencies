namespace Dragablz
{
    public enum TabEmptiedResponse
    {
        /// <summary>
        /// Allow the Window to be closed automatically.
        /// </summary>
        CloseWindowOrLayoutBranch,
        /// <summary>
        /// The window will not be closed by the <see cref="TabablzControl"/>, probably meaning the implementor will close the window manually
        /// </summary>
        DoNothing
    }
}