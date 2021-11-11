using System;
using System.Windows;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Globalization;
using System.Windows.Markup;

namespace Dependencies
{
    internal static class NameDictionaryHelper
    {
        public static string GetDisplayName(LanguageSpecificStringDictionary nameDictionary)
        {
            // Look up the display name based on the UI culture, which is the same culture
            // used for resource loading.
            XmlLanguage userLanguage = XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);

            // Look for an exact match.
            string name;
            if (nameDictionary.TryGetValue(userLanguage, out name))
            {
                return name;
            }

            // No exact match; return the name for the most closely related language.
            int bestRelatedness = -1;
            string bestName = string.Empty;

            foreach (KeyValuePair<XmlLanguage, string> pair in nameDictionary)
            {
                int relatedness = GetRelatedness(pair.Key, userLanguage);
                if (relatedness > bestRelatedness)
                {
                    bestRelatedness = relatedness;
                    bestName = pair.Value;
                }
            }

            return bestName;
        }

        public static string GetDisplayName(IDictionary<CultureInfo, string> nameDictionary)
        {
            // Look for an exact match.
            string name;
            if (nameDictionary.TryGetValue(CultureInfo.CurrentUICulture, out name))
            {
                return name;
            }

            // No exact match; return the name for the most closely related language.
            int bestRelatedness = -1;
            string bestName = string.Empty;

            XmlLanguage userLanguage = XmlLanguage.GetLanguage(CultureInfo.CurrentUICulture.IetfLanguageTag);

            foreach (KeyValuePair<CultureInfo, string> pair in nameDictionary)
            {
                int relatedness = GetRelatedness(XmlLanguage.GetLanguage(pair.Key.IetfLanguageTag), userLanguage);
                if (relatedness > bestRelatedness)
                {
                    bestRelatedness = relatedness;
                    bestName = pair.Value;
                }
            }

            return bestName;
        }

        private static int GetRelatedness(XmlLanguage keyLang, XmlLanguage userLang)
        {
            try
            {
                // Get equivalent cultures.
                CultureInfo keyCulture = CultureInfo.GetCultureInfoByIetfLanguageTag(keyLang.IetfLanguageTag);
                CultureInfo userCulture = CultureInfo.GetCultureInfoByIetfLanguageTag(userLang.IetfLanguageTag);
                if (!userCulture.IsNeutralCulture)
                {
                    userCulture = userCulture.Parent;
                }

                // If the key is a prefix or parent of the user language it's a good match.
                if (IsPrefixOf(keyLang.IetfLanguageTag, userLang.IetfLanguageTag) || userCulture.Equals(keyCulture))
                {
                    return 2;
                }

                // If the key and user language share a common prefix or parent neutral culture, it's a reasonable match.
                if (IsPrefixOf(TrimSuffix(userLang.IetfLanguageTag), keyLang.IetfLanguageTag) || userCulture.Equals(keyCulture.Parent))
                {
                    return 1;
                }
            }
            catch (ArgumentException)
            {
                // Language tag with no corresponding CultureInfo.
            }

            // They're unrelated languages.
            return 0;
        }

        private static string TrimSuffix(string tag)
        {
            int i = tag.LastIndexOf('-');
            if (i > 0)
            {
                return tag.Substring(0, i);
            }
            else
            {
                return tag;
            }
        }

        private static bool IsPrefixOf(string prefix, string tag)
        {
            return prefix.Length < tag.Length &&
                tag[prefix.Length] == '-' &&
                string.CompareOrdinal(prefix, 0, tag, 0, prefix.Length) == 0;
        }
    }

    internal class FontFamilyListItem : TextBlock, IComparable
    {
        private string _displayName;

        public FontFamilyListItem(FontFamily fontFamily)
        {
            _displayName = GetDisplayName(fontFamily);

            this.FontFamily = fontFamily;
            this.Text = _displayName;
            this.ToolTip = _displayName;

            // In the case of symbol font, apply the default message font to the text so it can be read.
            if (IsSymbolFont(fontFamily))
            {
                TextRange range = new TextRange(this.ContentStart, this.ContentEnd);
                range.ApplyPropertyValue(TextBlock.FontFamilyProperty, SystemFonts.MessageFontFamily);
            }
        }

        public override string ToString()
        {
            return _displayName;
        }

        int IComparable.CompareTo(object obj)
        {
            return string.Compare(_displayName, obj.ToString(), true, CultureInfo.CurrentCulture);
        }

        internal static bool IsSymbolFont(FontFamily fontFamily)
        {
            foreach (Typeface typeface in fontFamily.GetTypefaces())
            {
                GlyphTypeface face;
                if (typeface.TryGetGlyphTypeface(out face))
                {
                    return face.Symbol;
                }
            }
            return false;
        }

        internal static string GetDisplayName(FontFamily family)
        {
            return NameDictionaryHelper.GetDisplayName(family.FamilyNames);
        }
    }

    public partial class UserSettings : Window
    {
        private string PeviewerPath;
        private ICollection<FontFamily> _familyCollection;          // see FamilyCollection property
        private FontFamily SelectedFontFamily;

        public UserSettings()
        {
            InitializeComponent();

            TreeBuildCombo.ItemsSource = Enum.GetValues(typeof(TreeBuildingBehaviour.DependencyTreeBehaviour));
            BinaryCacheCombo.ItemsSource = Enum.GetValues(typeof(BinaryCacheOption.BinaryCacheOptionValue));
            PeviewerPath = Dependencies.Properties.Settings.Default.PeViewerPath;

        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            InitializeFontFamilyList();

            SelectedFontFamily = new FontFamily(Dependencies.Properties.Settings.Default.Font);
            SelectListItem(FontFamilyList, FontFamilyListItem.GetDisplayName(SelectedFontFamily));
            FontFamilyList.SelectionChanged += new SelectionChangedEventHandler(fontFamilyList_SelectionChanged);
            
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnValidate(object sender, RoutedEventArgs e)
        {
            // Update defaults
            Dependencies.Properties.Settings.Default.PeViewerPath = PeviewerPath;

			int TreeDepth = Dependencies.Properties.Settings.Default.TreeDepth;
			if (Int32.TryParse(TreeDepthValue.Text, out TreeDepth))
			{
				Dependencies.Properties.Settings.Default.TreeDepth = TreeDepth;
			}
			

			if (TreeBuildCombo.SelectedItem != null)
            {
                Dependencies.Properties.Settings.Default.TreeBuildBehaviour = TreeBuildCombo.SelectedItem.ToString();
            }

            if (BinaryCacheCombo.SelectedItem != null)
            {
                bool newValue = (bool) (new BinaryCacheOption()).ConvertBack(BinaryCacheCombo.SelectedItem, null, null, null);

                if (Dependencies.Properties.Settings.Default.BinaryCacheOptionValue != newValue)
                {
                    System.Windows.MessageBox.Show("The binary caching preference has been modified, you need to restart Dependencies for the modifications to be actually reloaded.");
                }

                Dependencies.Properties.Settings.Default.BinaryCacheOptionValue = newValue;
            }


            Dependencies.Properties.Settings.Default.Font = FontFamilyListItem.GetDisplayName(SelectedFontFamily);
            this.Close();
        }

        private bool SelectListItem(System.Windows.Controls.ListBox list, object value)
        {
            ItemCollection itemList = list.Items;

            // Perform a binary search for the item.
            int first = 0;
            int limit = itemList.Count;

            while (first < limit)
            {
                int i = first + (limit - first) / 2;
                IComparable item = (IComparable)(itemList[i]);
                int comparison = item.CompareTo(value);
                if (comparison < 0)
                {
                    // Value must be after i
                    first = i + 1;
                }
                else if (comparison > 0)
                {
                    // Value must be before i
                    limit = i;
                }
                else
                {
                    // Exact match; select the item.
                    list.SelectedIndex = i;
                    itemList.MoveCurrentToPosition(i);
                    list.ScrollIntoView(itemList[i]);
                    return true;
                }
            }

            // Not an exact match; move current position to the nearest item but don't select it.
            if (itemList.Count > 0)
            {
                int i = Math.Min(first, itemList.Count - 1);
                itemList.MoveCurrentToPosition(i);
                list.ScrollIntoView(itemList[i]);
            }

            return false;
        }

        public ICollection<FontFamily> FontFamilyCollection
        {
            get
            {
                return (_familyCollection == null) ? Fonts.SystemFontFamilies : _familyCollection;
            }

            set
            {
                if (value != _familyCollection)
                {
                    _familyCollection = value;
                }
            }
        }

        private void fontFamilyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FontFamilyListItem item = FontFamilyList.SelectedItem as FontFamilyListItem;
            if (item != null)
            {
                SelectedFontFamily = item.FontFamily;
            }
        }

        private void InitializeFontFamilyList()
        {
            ICollection<FontFamily> familyCollection = FontFamilyCollection;
            if (familyCollection != null)
            {
                FontFamilyListItem[] items = new FontFamilyListItem[familyCollection.Count];

                int i = 0;

                foreach (FontFamily family in familyCollection)
                {
                    items[i++] = new FontFamilyListItem(family);
                }

                Array.Sort<FontFamilyListItem>(items);

                foreach (FontFamilyListItem item in items)
                {
                    FontFamilyList.Items.Add(item);
                }
            }
        }

        private void OnPeviewerPathSettingChange(object sender, RoutedEventArgs e)
        {
            string programPath = Dependencies.Properties.Settings.Default.PeViewerPath;

            OpenFileDialog InputFileNameDlg = new OpenFileDialog()
            {
                Filter = "exe files (*.exe, *.dll)| *.exe;*.dll; | All files (*.*)|*.*",
                FilterIndex = 0,
                RestoreDirectory = true,
                InitialDirectory = System.IO.Path.GetDirectoryName(programPath)
            };


            if (InputFileNameDlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            PeviewerPath = InputFileNameDlg.FileName;
        }

		private void NumericOnly(System.Object sender, System.Windows.Input.TextCompositionEventArgs e)
		{
			e.Handled = IsTextNumeric(e.Text);

		}

		private static bool IsTextNumeric(string str)
		{
			System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("[^0-9]+");
			return reg.IsMatch(str);

		}
	}

}
