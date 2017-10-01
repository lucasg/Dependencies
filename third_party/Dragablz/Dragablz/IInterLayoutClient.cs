using System.Windows;

namespace Dragablz
{
    /// <summary>
    /// Implementors should provide a mechanism to provide a new host control which contains a new <see cref="TabablzControl"/>.
    /// </summary>
    public interface IInterLayoutClient
    {
        /// <summary>
        /// Provide a new host control and tab into which will be placed into a newly created layout branch.
        /// </summary>
        /// <param name="partition">Provides the partition where the drag operation was initiated.</param>
        /// <param name="source">The source control where a dragging operation was initiated.</param>
        /// <returns></returns>
        INewTabHost<UIElement> GetNewHost(object partition, TabablzControl source);
        
    }
}