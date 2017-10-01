using System.Threading;
using System.Windows;

namespace Dragablz
{
    /// <summary>
    /// Implementors should provide mechanisms for providing new windows and closing old windows.
    /// </summary>
    public interface IInterTabClient
    {
        /// <summary>
        /// Provide a new host window so a tab can be teared from an existing window into a new window.
        /// </summary>
        /// <param name="interTabClient"></param>
        /// <param name="partition">Provides the partition where the drag operation was initiated.</param>
        /// <param name="source">The source control where a dragging operation was initiated.</param>
        /// <returns></returns>
        INewTabHost<Window> GetNewHost(IInterTabClient interTabClient, object partition, TabablzControl source);
        /// <summary>
        /// Called when a tab has been emptied, and thus typically a window needs closing.
        /// </summary>
        /// <param name="tabControl"></param>
        /// <param name="window"></param>
        /// <returns></returns>
        TabEmptiedResponse TabEmptiedHandler(TabablzControl tabControl, Window window);
    }
}