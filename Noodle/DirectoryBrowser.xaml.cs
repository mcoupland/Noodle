using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Noodle
{
    /// <summary>
    /// Interaction logic for DirectoryBrowser.xaml
    /// </summary>
    public partial class DirectoryBrowser : System.Windows.Controls.UserControl
    {
        public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(DirectoryBrowser), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static DependencyProperty DescriptionProperty = DependencyProperty.Register("Description", typeof(string), typeof(DirectoryBrowser), new PropertyMetadata(null));

        public string Text { get { return GetValue(TextProperty) as string; } set { SetValue(TextProperty, value); } }

        public string Description { get { return GetValue(DescriptionProperty) as string; } set { SetValue(DescriptionProperty, value); } }

        public DirectoryBrowser() { InitializeComponent(); }

        private void BrowseFolder(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = Description;
                dlg.SelectedPath = Text;
                dlg.ShowNewFolderButton = true;
                DialogResult result = dlg.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    Text = dlg.SelectedPath;
                    BindingExpression be = GetBindingExpression(TextProperty);
                    if (be != null)
                        be.UpdateSource();
                }
            }
        }
    }
}
