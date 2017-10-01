using System;
using System.Runtime.ConstrainedExecution;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Dragablz
{
    /// <summary>
    /// Provides a simple implementation of <see cref="IInterLayoutClient"/>, but only really useful if 
    /// <see cref="TabItem"/> instances are specified in XAML.  If you are binding via ItemsSource then
    /// you most likely want to create your own implementation of <see cref="IInterLayoutClient"/>.
    /// </summary>
    public class DefaultInterLayoutClient : IInterLayoutClient
    {
        public INewTabHost<UIElement> GetNewHost(object partition, TabablzControl source)
        {
            var tabablzControl = new TabablzControl {DataContext = source.DataContext};

            Clone(source, tabablzControl);

            if (source.InterTabController == null)
                throw new InvalidOperationException("Source tab does not have an InterTabCOntroller set.  Ensure this is set on initial, and subsequently generated tab controls.");

            var newInterTabController = new InterTabController
            {
                Partition = source.InterTabController.Partition
            };
            Clone(source.InterTabController, newInterTabController);
            tabablzControl.SetCurrentValue(TabablzControl.InterTabControllerProperty, newInterTabController);            

            return new NewTabHost<UIElement>(tabablzControl, tabablzControl);
        }

        private static void Clone(DependencyObject from, DependencyObject to)
        {
            var localValueEnumerator = from.GetLocalValueEnumerator();
            while (localValueEnumerator.MoveNext())
            {
                if (localValueEnumerator.Current.Property.ReadOnly ||
                    localValueEnumerator.Current.Value is FrameworkElement) continue;
                
                if (!(localValueEnumerator.Current.Value is BindingExpressionBase))
                    to.SetCurrentValue(localValueEnumerator.Current.Property, localValueEnumerator.Current.Value);                
            }            
        }
    }
}