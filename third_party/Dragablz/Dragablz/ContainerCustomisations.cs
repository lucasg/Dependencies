using System;
using System.Windows;

namespace Dragablz
{
    internal class ContainerCustomisations
    {
        private readonly Func<DragablzItem> _getContainerForItemOverride;
        private readonly Action<DependencyObject, object> _prepareContainerForItemOverride;
        private readonly Action<DependencyObject, object> _clearingContainerForItemOverride;

        public ContainerCustomisations(Func<DragablzItem> getContainerForItemOverride = null, Action<DependencyObject, object> prepareContainerForItemOverride = null, Action<DependencyObject, object> clearingContainerForItemOverride = null)
        {
            _getContainerForItemOverride = getContainerForItemOverride;
            _prepareContainerForItemOverride = prepareContainerForItemOverride;
            _clearingContainerForItemOverride = clearingContainerForItemOverride;
        }

        public Func<DragablzItem> GetContainerForItemOverride
        {
            get { return _getContainerForItemOverride; }
        }

        public Action<DependencyObject, object> PrepareContainerForItemOverride
        {
            get { return _prepareContainerForItemOverride; }
        }

        public Action<DependencyObject, object> ClearingContainerForItemOverride
        {
            get { return _clearingContainerForItemOverride; }
        }
    }
}