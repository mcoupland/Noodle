using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using static Noodle.Cache;

namespace Noodle
{
    #region Enumerations
    public enum PopupButtonTypes { None, Ok, Close, OkCancel, Yes, No, YesNo, YesNoCancel };
    #endregion

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //the WPF designer, select the relevant control, or place the mouse cursor on the relevant control in the XAML.
        //Press F4 to open the Properties Window.
        //Open the Miscellaneous category to find the Template property, or type Template in the search field at the top of the Window.
        //Click on the little square to the right of the Template field and select the Convert to New Resource... option:

        #region Properties and Fields
        public ObservableCollection<CacheEntry> Results { get; set; } = new ObservableCollection<CacheEntry>();
        public List<CacheEntry> ResultList { get; set; } = new List<CacheEntry>();
        private string server;
        private string database;
        private string username;
        private string password;
        private string rootfolder;
        private bool filename;
        private bool filecontent;
        private bool tablename;
        private bool columnname;
        private bool procedurename;
        private bool procedurecontent;
        private bool functionname;
        private bool functioncontent;
        private bool viewname;
        private bool viewcontent;
        private bool searchaftercache;
        private CacheTypes cachetype;
        private FileTypes filetype;
        private int maxRowsPerReport;
        private Searcher searcher = new Searcher();

        // Research DispatcherPriority to see what you really need
        // https://docs.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcherpriority?view=netframework-4.8
        private DispatcherTimer timing_progress_updater = new DispatcherTimer(DispatcherPriority.Send);
        private DateTime process_start_time = DateTime.Now;
        private TimeSpan tickInterval = new TimeSpan(0,0,0,0,125);
        #endregion

        #region Constructors
        public MainWindow()
        {
            InitializeComponent();

            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(TextBox_SelectAll_MouseClick), true);
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.GotKeyboardFocusEvent, new RoutedEventHandler(TextBox_SelectAll), true);
            DataContext = this;
            timing_progress_updater.Interval = tickInterval;
            timing_progress_updater.Tick += Timing_progress_updater_Tick;
            uiSearchTerm.Focus();
            maxRowsPerReport = Convert.ToInt32(ConfigurationManager.AppSettings["MaxRowsPerReport"]);
            uiContains.IsChecked = true;
            uiFileType.SelectedIndex = 0;
            uiResultsLabel.Content = "";
            uiModal.Visibility = Visibility.Collapsed;
            uiCacheSettings.Visibility = Visibility.Collapsed;
            uiResultsStack.Visibility = Visibility.Visible;
            uiBrowser.Text = ConfigurationManager.AppSettings["DefaultRootFolder"];

            uiServer.Text = ConfigurationManager.AppSettings["Server"];
            uiDatabase.Text = ConfigurationManager.AppSettings["Database"];
            uiUsername.Text = ConfigurationManager.AppSettings["Username"];
            uiPassword.Password = ConfigurationManager.AppSettings["Password"];
            uiFileNames.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["FileNamesChecked"]);
            uiFileContent.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["FileContentChecked"]);
            uiProcedureNames.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["ProcedureNamesChecked"]);
            uiProcedureContent.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["ProcedureContentChecked"]);
            uiFunctionNames.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["FunctionNamesChecked"]);
            uiFunctionContent.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["FunctionContentChecked"]);
            uiViewNames.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["ViewNamesChecked"]);
            uiViewContent.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["ViewContentChecked"]);
            uiTableNames.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["TableNamesChecked"]);
            uiTableColumns.IsChecked = Convert.ToBoolean(ConfigurationManager.AppSettings["TableColumnsChecked"]);

            uiSearchTerm.Text = "[search term]";
        }
        #endregion

        #region Class Methods
        private void ShowPopup(PopupButtonTypes buttonTypes, string title, string message)
        {
            this.Cursor = Cursors.Wait;
            uiRootGrid.IsEnabled = false;
            if (buttonTypes == PopupButtonTypes.None)
            {
                uiCancel.Visibility = Visibility.Collapsed;
                uiOK.Visibility = Visibility.Collapsed;
                process_start_time = DateTime.Now;
                timing_progress_updater.Start();
                uiModalMessage.Text = $"{Shared.GetTimespan(DateTime.Now.Subtract(process_start_time))}";
            }
            if (buttonTypes == PopupButtonTypes.Ok)
            {
                uiOK.Focus();
                timing_progress_updater.Stop();
                uiOK.Visibility = Visibility.Visible;
            }
            uiRootGrid.Effect = new BlurEffect { Radius = 5, KernelType = KernelType.Gaussian };
            uiRootGrid.Opacity = 0.2;
            uiCacheSettings.Visibility = Visibility.Collapsed;
            uiSearchOptions.Visibility = Visibility.Collapsed;
            uiModalTitle.Content = title;
            uiModalMessage.Text = message;
            uiModal.Visibility = Visibility.Visible;
            var ani = new DoubleAnimation(0, 1, new Duration(new TimeSpan(0, 0, 0, 0, 250)));
            uiModal.BeginAnimation(OpacityProperty, ani);
        }

        private void HidePopup()
        {
            uiSearch.Focus();
            uiRootGrid.IsEnabled = true;
            timing_progress_updater.Stop();
            var ani = new DoubleAnimation(1, 0, new Duration(new TimeSpan(0, 0, 0, 0, 250)));
            ani.Completed += Ani_Completed;
            uiModal.BeginAnimation(OpacityProperty, ani);
            this.Cursor = Cursors.Arrow;
        }

        private void ShowCacheSettings()
        {
            uiBrowser.Focus();
            uiRootGrid.IsEnabled = false;
            uiSearchOptions.Visibility = Visibility.Collapsed;
            uiModal.Visibility = Visibility.Collapsed;
            uiRootGrid.Effect = new BlurEffect { Radius = 5, KernelType = KernelType.Gaussian };
            uiRootGrid.Opacity = 0.2;
            uiCacheSettings.Visibility = Visibility.Visible;
            var ani = new DoubleAnimation(0, 1, new Duration(new TimeSpan(0, 0, 0, 0, 250)));
            uiCacheSettings.BeginAnimation(OpacityProperty, ani);
        }

        private void HideCacheSettings()
        {
            timing_progress_updater.Stop();
            var ani = new DoubleAnimation(1, 0, new Duration(new TimeSpan(0, 0, 0, 0, 250)));
            ani.Completed += Ani_Completed;
            uiCacheSettings.BeginAnimation(OpacityProperty, ani);
        }

        private void ShowSearchOptions()
        {
            uiOK.Focus();
            uiCacheSettings.Visibility = Visibility.Collapsed;
            uiModal.Visibility = Visibility.Collapsed;
            uiRootGrid.IsEnabled = false;
            uiRootGrid.Effect = new BlurEffect { Radius = 5, KernelType = KernelType.Gaussian };
            uiRootGrid.Opacity = 0.2;
            uiSearchOptions.Visibility = Visibility.Visible;
            var ani = new DoubleAnimation(0, 1, new Duration(new TimeSpan(0, 0, 0, 0, 250)));
            uiSearchOptions.BeginAnimation(OpacityProperty, ani);
        }

        private void HideSearchOptions()
        {
            timing_progress_updater.Stop();
            var ani = new DoubleAnimation(1, 0, new Duration(new TimeSpan(0, 0, 0, 0, 250)));
            ani.Completed += Ani_Completed;
            uiSearchOptions.BeginAnimation(OpacityProperty, ani);
        }

        private async Task<int> Find(string searchterm)
        {
            await Task.Run(() =>
            {
                if (filename) { cachetype = CacheTypes.FileName; AddResults(Cache.FileNameCache, searchterm); }
                if (filecontent) { cachetype = CacheTypes.FileContent;  AddResults(Cache.FileContentCache, searchterm); }
                if (tablename) { cachetype = CacheTypes.TableName;  AddResults(Cache.TableNameCache, searchterm); }
                if (columnname) { cachetype = CacheTypes.ColumnName;  AddResults(Cache.ColumnNameCache, searchterm); }
                if (procedurename) { cachetype = CacheTypes.ProcedureName;  AddResults(Cache.ProcedureNameCache, searchterm); }
                if (procedurecontent) { cachetype = CacheTypes.ProcedureContent;  AddResults(Cache.ProcedureContentCache, searchterm); }
                if (functionname) { cachetype = CacheTypes.FunctionName;  AddResults(Cache.FunctionNameCache, searchterm); }
                if (functioncontent) { cachetype = CacheTypes.FunctionContent;  AddResults(Cache.FunctionContentCache, searchterm); }
                if (viewname) { cachetype = CacheTypes.ViewName; AddResults(Cache.ViewNameCache, searchterm); }
                if (viewcontent) { cachetype = CacheTypes.ViewContent; AddResults(Cache.ViewContentCache, searchterm); }
            });
            return 0;
        }

        private void AddResults(List<CacheEntry> cacheEntries, string searchTerm)
        {
            //Using a dispatcher so I can update the collection in the search click event.
            var results = searcher.Find(cacheEntries, searchTerm);
            foreach(var result in results)
            {
               App.Current.Dispatcher.Invoke((System.Action)delegate
               {
                   Results.Add(result);
               });
            }
        }

        private void NeedToRecache()
        {
            ShowPopup(PopupButtonTypes.Ok, "Update Cache", "Search targets have changed - you must re-cache before searching.");
            uiCache.IsEnabled = true;
            uiSearch.IsEnabled = false;
        }

        private void Copy(string copyText)
        {
            try
            {
                Clipboard.SetText(copyText);
            }
            catch { }
        }

        private void Open(CacheEntry cacheEntry)
        {
            cacheEntry.Launch(uiServer.Text, uiDatabase.Text, uiUsername.Text, uiPassword.Password);
        }

        public DataGridCell GetCell(int row, int column)
        {
            DataGridRow rowContainer = GetRow(row);
            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(rowContainer);
                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                if (cell == null)
                {
                    uiResults.ScrollIntoView(rowContainer, uiResults.Columns[column]);
                    cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                }
                return cell;
            }
            return null;
        }

        public DataGridRow GetRow(int index)
        {
            DataGridRow row = (DataGridRow)uiResults.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                uiResults.UpdateLayout();
                uiResults.ScrollIntoView(uiResults.Items[index]);
                row = (DataGridRow)uiResults.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        private async void BuildCache()
        {
            filename = uiFileNames.IsChecked.Value;
            filecontent = uiFileContent.IsChecked.Value;
            tablename = uiTableNames.IsChecked.Value;
            columnname = uiTableColumns.IsChecked.Value;
            procedurename = uiProcedureNames.IsChecked.Value;
            procedurecontent = uiProcedureContent.IsChecked.Value;
            functionname = uiFunctionNames.IsChecked.Value;
            functioncontent = uiFunctionContent.IsChecked.Value;
            viewname = uiViewNames.IsChecked.Value;
            viewcontent = uiViewContent.IsChecked.Value;


            rootfolder = uiBrowser.Text;
            server = uiServer.Text;
            database = uiDatabase.Text;
            username = uiUsername.Text;
            password = uiPassword.Password;
            uiModalFooter.Content = $"Root Directory: {rootfolder} - Database: {server}\\{database}";
            ShowPopup(PopupButtonTypes.None, "Building Cache", "");
            foreach (var item in uiFileType.Items)
            {
                if ((item as ComboBoxItem).IsSelected)
                {
                    var name = (item as ComboBoxItem).Content;
                    switch (name)
                    {
                        case ".Net":
                            filetype = FileTypes.DotNet;
                            break;
                        case "SQL":
                            filetype = FileTypes.SQL;
                            break;
                        case "VB 6":
                            filetype = FileTypes.VB;
                            break;
                        case "Text":
                            filetype = FileTypes.Text;
                            break;
                        case "Any":
                        default:
                            break;
                    }
                }
            }
            await Task.Run(() =>
            {
                Cache.RootFolder = rootfolder;
                Cache.Server = server;
                Cache.Database = database;
                Cache.UserName = username;
                Cache.Password = password;
                Cache.FileType = filetype;
                Cache.AllCache.Clear();
                //if (tablename && columnname && procedurename && procedurecontent && functionname && functioncontent && viewname && viewcontent)
                //{
                //    foreach (CacheTypes ct in Enum.GetValues(typeof(CacheTypes)))
                //    {
                //        if (ct == CacheTypes.FileName || ct == CacheTypes.FileContent) { continue; }
                //        Cache.UpdateDatabaseCache(ct);
                //    }
                //}
                //else
                //{
                    if (tablename) { cachetype = CacheTypes.TableName; Cache.UpdateDatabaseCache(CacheTypes.TableName); }
                    if (columnname) { cachetype = CacheTypes.ColumnName; Cache.UpdateDatabaseCache(CacheTypes.ColumnName); }
                    if (procedurename) { cachetype = CacheTypes.ProcedureName; Cache.UpdateDatabaseCache(CacheTypes.ProcedureName); }
                    if (procedurecontent) { cachetype = CacheTypes.ProcedureContent; Cache.UpdateDatabaseCache(CacheTypes.ProcedureContent); }
                    if (functionname) { cachetype = CacheTypes.FunctionName; Cache.UpdateDatabaseCache(CacheTypes.FunctionName); }
                    if (functioncontent) { cachetype = CacheTypes.FunctionContent; Cache.UpdateDatabaseCache(CacheTypes.FunctionContent); }
                    if (viewname) { cachetype = CacheTypes.ViewName; Cache.UpdateDatabaseCache(CacheTypes.ViewName); }
                    if (viewcontent) { cachetype = CacheTypes.ViewContent; Cache.UpdateDatabaseCache(CacheTypes.ViewContent); }
                //}

                if (filename) { Cache.UpdateFileCache(CacheTypes.FileName); }
                if (filecontent) { Cache.UpdateFileCache(CacheTypes.FileContent); }
            });
            HidePopup();
            TextBox_SelectAll(uiSearchTerm, new RoutedEventArgs(null, uiSearchTerm));            
            if(searchaftercache)
            {
                PerformSearch();
            }
        }

        private async void PerformSearch()
        {
            Results.Clear();

            if (uiSearchTerm.Text.Length < 3) { return; }
            if (uiServer.Text != server) { NeedToRecache(); return; }
            if (uiDatabase.Text != database) { NeedToRecache(); return; }
            if (uiUsername.Text != username) { NeedToRecache(); return; }
            if (uiPassword.Password != password) { NeedToRecache(); return; }
            if (uiBrowser.Text != rootfolder) { NeedToRecache(); return; }

            searcher.CaseSensitive = uiCaseSensitive.IsChecked.Value;
            searcher.Contains = uiContains.IsChecked.Value;
            searcher.WholeWord = uiWholeWord.IsChecked.Value;
            searcher.ExactMatch = uiExactMatch.IsChecked.Value;
            searcher.Regex = uiRegEx.IsChecked.Value;
            filename = uiFileNames.IsChecked.Value;
            filecontent = uiFileContent.IsChecked.Value;
            tablename = uiTableNames.IsChecked.Value;
            columnname = uiTableColumns.IsChecked.Value;
            procedurename = uiProcedureNames.IsChecked.Value;
            procedurecontent = uiProcedureContent.IsChecked.Value;
            functionname = uiFunctionNames.IsChecked.Value;
            functioncontent = uiFunctionContent.IsChecked.Value;
            viewname = uiViewNames.IsChecked.Value;
            viewcontent = uiViewContent.IsChecked.Value;

            Task<int> process = Find(uiSearchTerm.Text);
            process_start_time = DateTime.Now;
            ShowPopup(PopupButtonTypes.None, $"Searching for \"{uiSearchTerm.Text}\"", "");
            await process;

            uiResults.ItemsSource = Results.Take(Convert.ToInt32(ConfigurationManager.AppSettings["MaxResults"]));
            HidePopup();
            searchaftercache = false;            
            uiModalFooter.Content = "";
            uiResultsLabel.Content = $"{Shared.GetDateTimeStampAMPM(DateTime.Now)}: Found {Results.Count} results for \"{uiSearchTerm.Text}\"";

            SaveReport(uiSearchTerm.Text);
            TextBox_SelectAll(uiSearchTerm, new RoutedEventArgs(null, uiSearchTerm));
            uiSearchTerm.Focus();
        }

        private void SaveReport(string term)
        {
            var x = Results.Skip(0).Take(maxRowsPerReport);

            int i = 1;
            while(x.Count() > 0)
            {
                SaveReportRows(term, x.ToList());
                x = Results.Skip(i * maxRowsPerReport).Take(maxRowsPerReport);
                i++;
            }
        }

        private void SaveReportRows(string term, List<CacheEntry> cacheEntries) 
        { 
            var reportFileName = $@"C:\CODE MINE\CURRENT\Noodle\noodle report-{term}-{DateTime.Now.ToString("yy-mm-dd HH-mm-ss-fff")}.xlsx";
            var excelDocument = new ExcelDocument($"Noodle Results for {uiSearchTerm.Text} Elapsed time {Shared.GetTimespanSentence(DateTime.Now.Subtract(process_start_time)).ToLower()}", new List<string> { "Result Type", "Location", "Name", "Line Number", "Content/Value" });
            excelDocument.AddRow(ExcelDocumentRowTypes.TITLE);
            excelDocument.AddRow(ExcelDocumentRowTypes.BLANK);
            excelDocument.AddRow(ExcelDocumentRowTypes.HEADER);

            if (File.Exists(reportFileName))
            {
                var backupFile = reportFileName;
                while (File.Exists(backupFile))
                {
                    backupFile = Shared.GetNewPath(backupFile, $" {DateTime.Now.ToString("MM-dd-hh-mm-ss")}");
                }
                File.Copy(reportFileName, backupFile);
                File.Delete(reportFileName);
            }
            
            for (int i = 0; i < cacheEntries.Count(); i++)
            {
                var row = cacheEntries.ElementAt(i);
                var rowValues = new List<object> { CacheEntry.GetCacheTypeName(row.CacheType), row.Location, row.Name, row.LineNumber, row.Value };
                excelDocument.AddRow(ExcelDocumentRowTypes.NORMAL, rowValues);
            }

            try
            {
                excelDocument.AutosizeColumns(1250);
                excelDocument.SortAndFilter(2);
                excelDocument.SaveWorkbook(reportFileName);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region Event Handlers
        private static void TextBox_SelectAll_MouseClick(object sender, MouseButtonEventArgs e)
        {
            var textbox = sender as TextBox;
            if (textbox != null && !textbox.IsKeyboardFocusWithin)
            {
                if (e.OriginalSource.GetType().Name == "TextBoxView")
                {
                    e.Handled = true;
                    textbox.Focus();
                }
            }
        }

        private static void TextBox_SelectAll(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null) { textBox.SelectAll(); }
        }

        private void uiSettings_Click(object sender, RoutedEventArgs args)
        {
            ShowSearchOptions();
        }

        private void uiCache_Click(object sender, RoutedEventArgs e)
        {
            ShowCacheSettings();
        }
        
        private void uiSearch_Click(object sender, RoutedEventArgs e)
        {
            if (Cache.CachedDatabaseItemCount == 0 && Cache.CachedFileItemCount == 0) 
            {
                searchaftercache = true;
                ShowCacheSettings();
                return;
            }
            PerformSearch();
        }

        private void uiWholeWord_Checked(object sender, RoutedEventArgs e)
        {
            uiExactMatch.IsChecked = false;
            uiContains.IsChecked = false;
            uiRegEx.IsChecked = false;
        }

        private void uiContains_Checked(object sender, RoutedEventArgs e)
        {
            uiExactMatch.IsChecked = false;
            uiWholeWord.IsChecked = false;
            uiRegEx.IsChecked = false;
        }

        private void uiRegEx_Checked(object sender, RoutedEventArgs e)
        {
            uiExactMatch.IsChecked = false;
            uiContains.IsChecked = false;
            uiWholeWord.IsChecked = false;
            uiCaseSensitive.IsChecked = false;
        }

        private void uiCaseSensitive_Checked(object sender, RoutedEventArgs e)
        {
            uiRegEx.IsChecked = false;
        }

        private void uiCancel_Click(object sender, RoutedEventArgs e)
        {
            HidePopup();
        }

        private void uiOK_Click(object sender, RoutedEventArgs e)
        {
            HidePopup();
        }

        private void Timing_progress_updater_Tick(object sender, EventArgs e)
        {
            var displayCacheType = CacheEntry.GetCacheTypeName(cachetype);
            var cacheCount = 0;

            if(cachetype == CacheTypes.FileName || cachetype == CacheTypes.FileContent) { cacheCount = Cache.CachedFileItemCount; }
            else { cacheCount = Cache.CachedDatabaseItemCount; }

            var cacheType = displayCacheType;
            var count = cacheCount;
            uiModalMessage.Text = $"{count.ToString("N0")} items in {cacheType} Cache\nElapsed time {Shared.GetTimespanSentence(DateTime.Now.Subtract(process_start_time)).ToLower()}";
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void uiResults_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if((sender as DataGrid).SelectedCells.Count == 0) { return; }
            Open((sender as DataGrid).SelectedCells[0].Item as CacheEntry);
        }

        private void uiResults_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void uiCopyRow_Click(object sender, RoutedEventArgs e)
        {
            Copy((uiResults.SelectedCells[0].Item as CacheEntry).ToString());
        }

        private void uiCopyCell_Click(object sender, RoutedEventArgs e)
        {
            var c = GetCell(uiResults.Items.IndexOf(uiResults.CurrentItem), (uiResults.SelectedCells[0]).Column.DisplayIndex);
            Copy((c.Content as TextBlock).Text);
        }

        private void uiOpen_Click(object sender, RoutedEventArgs e)
        {
            var entry = uiResults.SelectedCells[0].Item as CacheEntry;
            Open(entry);
        }

        private void Ani_Completed(object sender, EventArgs e)
        {
            uiSearch.Focus();
            uiRootGrid.Effect = null;
            uiRootGrid.IsEnabled = true;
            uiRootGrid.Opacity = 1;
            uiCacheSettings.Visibility = Visibility.Collapsed;
            uiSearchOptions.Visibility = Visibility.Collapsed;
            uiSearch.IsEnabled = true;
            uiCache.IsDefault = false;
            uiSearch.IsDefault = true;
            this.Cursor = Cursors.Arrow;
        }

        private void uiCloseCacheSettings_Click(object sender, RoutedEventArgs e)
        {
            HideCacheSettings();
        }

        private void uiBuildCache_Click(object sender, RoutedEventArgs e)
        {
            BuildCache();
        }

        private void uiCloseSearchOptions_Click(object sender, RoutedEventArgs e)
        {
            HideSearchOptions();
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if(Keyboard.Modifiers == ModifierKeys.Alt && e.SystemKey == Key.D)
            {
                uiSearchTerm.Focus();
            }
        }

        private void uiExactMatch_Checked(object sender, RoutedEventArgs e)
        {
            uiWholeWord.IsChecked = false;
            uiContains.IsChecked = false;
            uiRegEx.IsChecked = false;
            uiCaseSensitive.IsChecked = true;
        }

        private void uiCaseSensitive_Unchecked(object sender, RoutedEventArgs e)
        {
            uiExactMatch.IsChecked = false;
        }
        #endregion
    }
}
