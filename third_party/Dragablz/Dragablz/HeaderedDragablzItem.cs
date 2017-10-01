using System.Windows;
using System.Windows.Controls;

namespace Dragablz
{
    public class HeaderedDragablzItem : DragablzItem
    {
        static HeaderedDragablzItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderedDragablzItem), new FrameworkPropertyMetadata(typeof(HeaderedDragablzItem)));            
        }
        
        public static readonly DependencyProperty HeaderContentProperty = DependencyProperty.Register(
            "HeaderContent", typeof (object), typeof (HeaderedDragablzItem), new PropertyMetadata(default(object)));

        public object HeaderContent
        {
            get { return (object) GetValue(HeaderContentProperty); }
            set { SetValue(HeaderContentProperty, value); }
        }

        public static readonly DependencyProperty HeaderContentStringFormatProperty = DependencyProperty.Register(
            "HeaderContentStringFormat", typeof (string), typeof (HeaderedDragablzItem), new PropertyMetadata(default(string)));

        public string HeaderContentStringFormat
        {
            get { return (string) GetValue(HeaderContentStringFormatProperty); }
            set { SetValue(HeaderContentStringFormatProperty, value); }
        }

        public static readonly DependencyProperty HeaderContentTemplateProperty = DependencyProperty.Register(
            "HeaderContentTemplate", typeof (DataTemplate), typeof (HeaderedDragablzItem), new PropertyMetadata(default(DataTemplate)));

        public DataTemplate HeaderContentTemplate
        {
            get { return (DataTemplate) GetValue(HeaderContentTemplateProperty); }
            set { SetValue(HeaderContentTemplateProperty, value); }
        }

        public static readonly DependencyProperty HeaderContentTemplateSelectorProperty = DependencyProperty.Register(
            "HeaderContentTemplateSelector", typeof (DataTemplateSelector), typeof (HeaderedDragablzItem), new PropertyMetadata(default(DataTemplateSelector)));

        public DataTemplateSelector HeaderContentTemplateSelector
        {
            get { return (DataTemplateSelector) GetValue(HeaderContentTemplateSelectorProperty); }
            set { SetValue(HeaderContentTemplateSelectorProperty, value); }
        }         
    }
}