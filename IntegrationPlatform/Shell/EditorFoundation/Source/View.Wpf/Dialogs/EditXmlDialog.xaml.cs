// Copyright © Microsoft Corporation.  All Rights Reserved.
// This code released under the terms of the 
// Microsoft Public License (MS-PL, http://opensource.org/licenses/ms-pl.html.)

using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.TeamFoundation.Migration.Shell.View
{
    /// <summary>
    /// Interaction logic for EditXmlDialog.xaml
    /// </summary>
    public partial class EditXmlDialog : Window
    {
        private SerializableElement m_element;

        public EditXmlDialog(SerializableElement element)
        {
            InitializeComponent();
            m_element = element;
            DataContext = m_element;
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.LineCount > 0)
            {
                m_element.LineNumber = textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart) + 1;
                m_element.ColumnNumber = textBox.SelectionStart - textBox.GetCharacterIndexFromLineIndex(m_element.LineNumber - 1) + 1;
            }
            else
            {
                m_element.LineNumber = 1;
                m_element.ColumnNumber = 1;
            }
            int offset = 1;
            m_element.LineNumber += offset;
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            m_element.Save();
            DialogResult = true;
            Close();
        }

        private void xmlEditorTextBox_RequestBringIntoView(object sender, RequestBringIntoViewEventArgs e)
        {
            e.Handled = true;
        }

        private void xmlEditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int length;
            if (xmlEditorTextBox.Text.Length > 0)
            {
                length = xmlEditorTextBox.LineCount;
            }
            else
            {
                length = 1;
            }
            length += 2;
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 1; i <= length; i++)
            {
                stringBuilder.AppendLine(i.ToString());
            }
            xmlEditorMargin.Text = stringBuilder.ToString().Trim();
        }

        private void TextBox_PreviewGotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            (sender as TextBox).RequestBringIntoView += new RequestBringIntoViewEventHandler(xmlEditorTextBox_RequestBringIntoView);
        }

        private void TextBox_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            (sender as TextBox).RequestBringIntoView -= xmlEditorTextBox_RequestBringIntoView;
        }
    }
}
