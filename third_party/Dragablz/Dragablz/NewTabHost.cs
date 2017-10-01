using System;
using System.Windows;

namespace Dragablz
{
    public class NewTabHost<TElement> : INewTabHost<TElement> where TElement : UIElement
    {
        private readonly TElement _container;
        private readonly TabablzControl _tabablzControl;

        public NewTabHost(TElement container, TabablzControl tabablzControl)
        {
            if (container == null) throw new ArgumentNullException("container");
            if (tabablzControl == null) throw new ArgumentNullException("tabablzControl");

            _container = container;
            _tabablzControl = tabablzControl;
        }

        public TElement Container
        {
            get { return _container; }
        }

        public TabablzControl TabablzControl
        {
            get { return _tabablzControl; }
        }
    }
}