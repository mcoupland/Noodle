using ReportBuilder;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using static WhereIsIt.Cache;

namespace WhereIsIt
{
    #region Enumerations
    public enum PopupButtonTypes { None, Ok, Close, OkCancel, Yes, No, YesNo, YesNoCancel };
    #endregion

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Custom Events
        public delegate void ProcessClicked(ProcessClickedArgs args);
        public event ProcessClicked OnProcessClicked;
        #endregion

        #region Custom Event Handlers
        public class ProcessClickedArgs
        {
            public bool IsValid { get; set; }
            public ProcessClickedArgs(bool isValid)
            {
                IsValid = isValid;
            }
        }
        #endregion

        #region Properties and Fields
        public ObservableCollection<CacheEntry> Results { get; set; } = new ObservableCollection<CacheEntry>();
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
        private FileTypes filetype;
        private Searcher searcher = new Searcher();

        #region Reporting
        private Template Report;
        private FileInfo ReportFile;
        private TemplateSection DictionarySection;
        private TemplateSection GroupSection;
        private TemplateSection LoggingSection;
        #endregion

        #region Timing
        // Research DispatcherPriority to see what you really need
        // https://docs.microsoft.com/en-us/dotnet/api/system.windows.threading.dispatcherpriority?view=netframework-4.8
        private DispatcherTimer timing_progress_updater = new DispatcherTimer(DispatcherPriority.Send);
        private DateTime process_start_time = DateTime.Now;
        #endregion

        #endregion

        #region Constructors
        public MainWindow()
        {
            InitializeComponent();
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(TextBox_SelectAll_MouseClick), true);
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.GotKeyboardFocusEvent, new RoutedEventHandler(TextBox_SelectAll), true);
            DataContext = this;
            timing_progress_updater.Interval = new TimeSpan(0, 0, 1);
            timing_progress_updater.Tick += Timing_progress_updater_Tick;
            uiContains.IsChecked = true;
            uiFileNames.IsChecked = true;
            uiFileType.SelectedIndex = 0;
            uiSearch.IsEnabled = false;
            uiResultsLabel.Content = "";

            if(Convert.ToBoolean(ConfigurationManager.AppSettings["Test"]))
            {
                uiServer.Text = "ias-sqlsss5";
                uiDatabase.Text = "dbIASosData";
                uiUsername.Text = "sa";
                uiPassword.Password = "chess";
                uiRootFolder.Text = @"c:\Assurant Files";
                uiSearchTerm.Text = "html";
            }
        }

        private void Timing_progress_updater_Tick(object sender, EventArgs e)
        {
            uiModalMessage.Content = $"Elapsed time {Shared.GetTimespanSentence(DateTime.Now.Subtract(process_start_time))}";
        }
        #endregion

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

        private async void uiCache_Click(object sender, RoutedEventArgs e)
        {
            ShowPopup("Caching Data", "");
            rootfolder = uiRootFolder.Text;
            server = uiServer.Text;
            database = uiDatabase.Text;
            username = uiUsername.Text;
            password = uiPassword.Password;
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
            
            await Task.Run(() => {
                Cache.RootFolder = rootfolder;
                Cache.Server = server;
                Cache.Database = database;
                Cache.UserName = username;
                Cache.Password = password;
                Cache.FileType = filetype;
                Cache.UpdateCache();      
            });
            uiSearch.IsEnabled = true;
            uiDebugLabel.Content = $"{Shared.GetDateTimeStampAMPM(DateTime.Now)}: Updated Cache, found {Cache.AllCache.Count.ToString("N0")} items.";
            HidePopup();
        }

        private void ShowPopup(string title, string message)
        {
            process_start_time = DateTime.Now;
            timing_progress_updater.Start();
            uiPopupBackground.Visibility = Visibility.Visible;
            uiModal.Visibility = Visibility.Visible;
            uiModalTitle.Content = title;
            uiModalMessage.Content = message;
        }

        private void HidePopup()
        {
            timing_progress_updater.Stop();
            uiPopupBackground.Visibility = Visibility.Collapsed;
            uiModal.Visibility = Visibility.Collapsed;
            uiModalTitle.Content = "";
            uiModalMessage.Content = "";
        }
        private async void uiSearch_Click(object sender, RoutedEventArgs e)
        {
            if (uiServer.Text != server) { NeedToRecache(); return; }
            if (uiDatabase.Text != database) { NeedToRecache(); return; }
            if (uiUsername.Text != username) { NeedToRecache(); return; }
            if (uiPassword.Password != password) { NeedToRecache(); return; }
            if (uiRootFolder.Text != rootfolder) { NeedToRecache(); return; }

            Results = new ObservableCollection<CacheEntry>();
            searcher.CaseSensitive = uiCaseSensitive.IsChecked.Value;
            searcher.Contains = uiContains.IsChecked.Value;
            searcher.WholeWord = uiWholeWord.IsChecked.Value;
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
            ShowPopup($"Searching for \"{uiSearchTerm.Text}\"", "");
            await process;

            HidePopup();
            uiResults.ItemsSource = Results;
            uiResultsLabel.Content = $"Found {Results.Count} results for\"{uiSearchTerm.Text}\"";
            uiDebugLabel.Content = $"{Shared.GetDateTimeStampAMPM(DateTime.Now)}: Searched for {uiSearchTerm.Text}.";
        }

        private async Task<int> Find(string searchterm) 
        {
            await Task.Run(() =>
            {
                if (filename) { searcher.Find(Cache.FileNameCache, searchterm).ForEach(x => Results.Add(x)); }
                if (filecontent) { searcher.Find(Cache.FileContentCache, searchterm).ForEach(x => Results.Add(x)); }
                if (tablename) { searcher.Find(Cache.TableNameCache, searchterm).ForEach(x => Results.Add(x)); }
                if (columnname) { searcher.Find(Cache.ColumnNameCache, searchterm).ForEach(x => Results.Add(x)); }
                if (procedurename) { searcher.Find(Cache.ProcedureNameCache, searchterm).ForEach(x => Results.Add(x)); }
                if (procedurecontent) { searcher.Find(Cache.ProcedureContentCache, searchterm).ForEach(x => Results.Add(x)); }
                if (functionname) { searcher.Find(Cache.FunctionNameCache, searchterm).ForEach(x => Results.Add(x)); }
                if (functioncontent) { searcher.Find(Cache.FunctionContentCache, searchterm).ForEach(x => Results.Add(x)); }
                if (viewname) { searcher.Find(Cache.ViewNameCache, searchterm).ForEach(x => Results.Add(x)); }
                if (viewcontent) { searcher.Find(Cache.ViewContentCache, searchterm).ForEach(x => Results.Add(x)); }
            });
            return 0;
        }
        private void NeedToRecache()
        {
            uiDebugLabel.Content = $"{Shared.GetDateTimeStampAMPM(DateTime.Now)}: Need to re-cache.";
            uiCache.IsEnabled = true;
            uiSearch.IsEnabled = false;
        }

        private void uiWholeWord_Checked(object sender, RoutedEventArgs e)
        {
            uiContains.IsChecked = false;
            uiRegEx.IsChecked = false;
        }

        private void uiContains_Checked(object sender, RoutedEventArgs e)
        {
            uiWholeWord.IsChecked = false;
            uiRegEx.IsChecked = false;
        }

        private void uiRegEx_Checked(object sender, RoutedEventArgs e)
        {
            uiContains.IsChecked = false;
            uiWholeWord.IsChecked = false;
            uiCaseSensitive.IsChecked = false;
        }

        private void uiCaseSensitive_Checked(object sender, RoutedEventArgs e)
        {
            uiRegEx.IsChecked = false;
        }

        //#region Class Methods
        //private void InitializeApplication()
        //{
        //    timing_progress_updater.Interval = new TimeSpan(0, 0, 1);
        //    timing_progress_updater.Tick += Timing_progress_updater_Tick;
        //    root_directory = ConfigurationManager.AppSettings["SomeDirectory"];
        //    template_directory = ConfigurationManager.AppSettings["ReportTemplateDirectory"];
        //    database_connection = ConfigurationManager.ConnectionStrings["SomeDatabase"].ConnectionString;
        //    Directory.CreateDirectory(root_directory);
        //}

        //private async void DoAsyncTasks()
        //{
        //    Task<int> task = AsyncDemoProgressUpdate();
        //    await task;
        //    CreateReport();
        //}

        //private void WriteToFileStream(Dictionary<string, string> rows)
        //{
        //    using (var fileStream = new StreamWriter(@"C:\Somewhere\Somefile.txt"))
        //    {
        //        foreach (var row in rows)
        //        {
        //            fileStream.Write(row.Key);
        //            var columns = row.Value.Split(':');
        //            foreach (var column in columns)
        //            {
        //                fileStream.Write(column);
        //                if (column != columns.Last())
        //                {
        //                    fileStream.Write("|");
        //                }
        //            }
        //            fileStream.Write("\n");
        //        }
        //    }
        //}
        //#endregion

        //#region Database
        //private int DBQuery()
        //{
        //    var rowCount = 0;
        //    using (var command = new SqlCommand($"SELECT COUNT(*) FROM SomeTable;"))
        //    using (command.Connection = new SqlConnection(database_connection))
        //    {
        //        command.CommandType = CommandType.Text;
        //        command.CommandTimeout = 900000;
        //        command.Connection.Open();
        //        using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection))
        //        {
        //            if (reader.HasRows)
        //            {
        //                reader.Read();
        //                rowCount = reader.GetInt32(0);
        //            }
        //        }
        //    }
        //    return rowCount;
        //}

        //private bool DBProcedure()
        //{
        //    var exists = false;
        //    using (var command = new SqlCommand("StoredProcedureName"))
        //    using (command.Connection = new SqlConnection(database_connection))
        //    {
        //        command.CommandType = CommandType.StoredProcedure;
        //        command.Parameters.Add("TableName", SqlDbType.VarChar, 100).Value = "SomeTableName";
        //        command.CommandTimeout = 900000;
        //        command.Connection.Open();
        //        using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection))
        //        {
        //            if (reader.HasRows)
        //            {
        //                reader.Read();
        //                exists = Convert.ToBoolean(reader.GetValue(0));
        //            }
        //        }
        //    }
        //    return exists;
        //}

        //private int DBMultipleRows()
        //{
        //    var recordCount = 0;
        //    var fieldTypes = new List<string>();
        //    var fieldNames = new List<string>();
        //    var fieldValues = new List<string>();

        //    using (var command = new SqlCommand("StoredProcedureName"))
        //    using (command.Connection = new SqlConnection(database_connection))
        //    {
        //        command.CommandType = CommandType.StoredProcedure;
        //        command.Parameters.Add("TableName", SqlDbType.VarChar, 100).Value = "SomeTableName";
        //        command.CommandTimeout = 900000;
        //        command.Connection.Open();
        //        using (var reader = command.ExecuteReader(CommandBehavior.CloseConnection))
        //        {
        //            while(reader.Read())
        //            {
        //                if(recordCount == 0)
        //                {
        //                    for (int i = 0; i < reader.FieldCount; i++)
        //                    {
        //                        fieldTypes.Add(reader.GetFieldType(i).Name);
        //                        fieldNames.Add(reader.GetName(i));
        //                    }
        //                    fieldValues.Add(reader.GetValue(0).ToString());  // Get value of first column (0)
        //                }
        //                recordCount++;
        //            }
        //        }
        //    }
        //    return recordCount - 1;
        //}
        //#endregion

        //#region Logging and Reporting
        //private void InitializeReport()
        //{
        //    var TemplatePath = new DirectoryInfo(template_directory);
        //    ReportFile = new FileInfo($@"{root_directory}\Report-{Shared.GetMonthDay()}_{Shared.GetTimeStampMilitary()}.html");
        //    Report = new Template(ReportFile, TemplatePath);
        //    DictionarySection = new TemplateSection(TemplatePath, "Dictionary Section", TemplateSection.SectionTypes.Dictionary);
        //    GroupSection = new TemplateSection(TemplatePath, "Group Section", TemplateSection.SectionTypes.Group);
        //    LoggingSection = new TemplateSection(TemplatePath, "Dictionary Section", TemplateSection.SectionTypes.Log);
        //}

        //private void CreateReport()
        //{
        //    #region Another Section            
        //    DictionarySection.AddRow("skipped", "ITD files were not sent.", EmphasisColor.Red);
        //    DictionarySection.AddRow("skipped", "IFO files were not sent.", EmphasisColor.Red);
        //    #endregion

        //    #region Group Section
        //    GroupSection.AddRow("tables", "list of tables");
        //    GroupSection.AddRow("", "table1");
        //    GroupSection.AddRow("", "table2");
        //    GroupSection.AddRow("files", "list of files");
        //    GroupSection.AddRow("", "file1");
        //    GroupSection.AddRow("", "file2");
        //    #endregion

        //    #region Assemble and Write Report            
        //    var first = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        //    var last = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
        //    Report.AddTitle("Report Title", $"{Shared.GetDayMonthNameYear(first)} - {Shared.GetDayMonthNameYear(last)}", $"{Shared.GetDayMonthNameYearTime(DateTime.Now)}");
        //    Report.AddSection(DictionarySection);
        //    Report.AddSection(GroupSection);
        //    Report.AddSection(LoggingSection);
        //    Report.AddFooter($"Finished", $"Elapsed time approximately {Shared.GetTimespanSentence(DateTime.Now.Subtract(process_start_time))}");
        //    Report.Write();
        //    #endregion
        //}

        //private void LogError(Exception exception, string message)
        //{
        //    Shared.Logger.Error(exception, message);
        //}
        //#endregion

        //#region Modal
        //private void UpdateProgressWindow(string content)
        //{
        //    uiProgressWindowLabel1.Content = content;
        //}

        //private void UpdateProgressWindowTime(string content)
        //{
        //    uiProgressWindowLabel2.Content = content;
        //}
        //#endregion

        //#region Async Methods
        //private async Task<int> AsyncDemoProgressUpdate()
        //{
        //    for (int i = 0; i < 1000; i++)
        //    {
        //        Task<int> task = AsyncDoNothing();
        //        UpdateProgressWindow($"{i}");
        //        await task;
        //    }
        //    return 0;
        //}

        //private async Task<int> AsyncDoNothing()
        //{
        //    await Task.Delay(new TimeSpan(0, 0, 0, 0, 125));
        //    return 0;
        //}
        //#endregion

        //#region Event Handlers
        //private void uiProcess_Click(object sender, RoutedEventArgs e)
        //{
        //    OnProcessClicked?.Invoke(new ProcessClickedArgs(true));
        //    DoAsyncTasks();
        //    process_start_time = DateTime.Now;
        //    timing_progress_updater.Start();
        //    uiPopupBackground.Visibility = Visibility.Visible;
        //    uiProgressWindow.Visibility = Visibility.Visible;
        //}

        //private void Timing_progress_updater_Tick(object sender, EventArgs e)
        //{
        //    UpdateProgressWindowTime($"Elapsed time {Shared.GetTimespanSentence(DateTime.Now.Subtract(process_start_time))}");
        //}
        //#endregion
    }
}
