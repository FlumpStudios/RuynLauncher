using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Compression;
using static RuynLancher.Constants;
using System.Windows.Controls;
using System.Linq;

namespace RuynLancher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string ACTIVE_STRING = " - ACTIVE";

        private static OrderByFilters currentFilter = OrderByFilters.UploadedDate;

        private static string _currentSelection = string.Empty;
        private static string _activePack = string.Empty;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void UpdateAvailablePackList()
        {
            if (!Directory.Exists(LEVELS_FOLDER)) { return; }

            DownloadedLevelPacks.Items.Clear();
            foreach (var levelpack in Directory.GetDirectories(LEVELS_FOLDER))
            {
                string fileName = Path.GetFileName(levelpack);

                if (fileName == _activePack) { fileName += ACTIVE_STRING; }
                DownloadedLevelPacks.Items.Add(fileName);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateLevelPacks();
            UpdateAvailablePackList();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bool windowed = false;
            if (Windowed?.IsChecked is not null)
            {
                windowed = Windowed.IsChecked.Value;
            }

            Environment.CurrentDirectory = Constants.GAME_FILE_LOCATION;
            string args = "";
            if (windowed)
            {
                args += " -w";
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = $"{GAME_FILE_LOCATION}\\{EXE_NAME}.exe",
                WorkingDirectory = GAME_FILE_LOCATION,
               
                Arguments = args
            });
        }

        private void LaunchEditor_Click(object sender, RoutedEventArgs e)
        {
            Environment.CurrentDirectory = GAME_FILE_LOCATION;
 
            Process.Start(new ProcessStartInfo
            {
                FileName = $"{GAME_FILE_LOCATION}\\{EDITOR_NAME}.exe",
                WorkingDirectory = GAME_FILE_LOCATION
                // Arguments = args
            });
        }

        private void ShowInputDialog_Click()
        {
            // Create and show the input dialog
            UploadDetailsWindow inputDialog = new UploadDetailsWindow();
            bool? result = inputDialog.ShowDialog();
        }

        private void UploadLevels_Click(object sender, RoutedEventArgs e)
        {
            ShowInputDialog_Click();
        }

        private static int _selectedId = -1;

        private void LevelPackDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var data = e.AddedItems[0] as dynamic;
            int? id = data?.Id ?? -1;

            if (id is not null && id > -1)
            {
                _selectedId = id ?? -1;
            }

            DownloadButton.IsEnabled = id > -1;
        }

        private async Task DownloadPack(int id)
        {
            try
            {
                LevelData downloadedPack = await Server.Get().GetLevelPackByIdAsync(id);
                if (downloadedPack?.LevelPackName is null) { return; }

                string directory = Path.Combine(GAME_FILE_LOCATION, LEVELS_FOLDER, downloadedPack.LevelPackName);
                Directory.CreateDirectory(directory);

                string tempName = Guid.NewGuid().ToString();

                string fullPath = Path.Combine(directory, tempName);
                
                // Clear out all the old level files
                if (Directory.Exists(directory))
                {
                    foreach (string filePath in Directory.GetFiles(directory))
                    {
                        File.Delete(filePath);
                    }
                }

                File.WriteAllBytes(fullPath, downloadedPack.FileData);

                using (FileStream zipToOpen = new FileStream(fullPath, FileMode.Open))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                    {
                        archive.ExtractToDirectory(directory);
                    }
                }
                File.Delete(fullPath);
                UpdateAvailablePackList();
                MessageBox.Show($"Level pack downloaded!", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ApiException ex)
            {
                MessageBox.Show($"Server error, could not download file", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something went wrong. Could not save to levels to disk", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        private async Task UpdateLevelPacks(OrderByFilters filters = OrderByFilters.UploadedDate)
        {
            ICollection<LevelListResponse> levelPacks = await Server.Get().GetLevelListAsync(null, 0, 20, filters, false);
            LevelPackDataGrid.ItemsSource = levelPacks.Select(x => new { x.Id, UploadDate = x.UploadDate.ToString()[..10], x.LevelPackName, x.Author, x.LevelCount, x.DownloadCount });
        }

        private async void LevelPackDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            // Get the column that was clicked
            var column = e.Column.Header;

            OrderByFilters filter = OrderByFilters.UploadedDate;

            bool decending = false;

            switch (column)
            {
                case "Upload Date":
                    filter = OrderByFilters.UploadedDate;
                    decending = true;
                    break;

                case "Name":
                    filter = OrderByFilters.Name;
                    decending = false;
                    break;
                case "Author":
                    filter = OrderByFilters.Author;
                    decending = false;
                    break;
                case "Level Count":
                    filter = OrderByFilters.LevelCount;
                    decending = false;
                    break;
                case "Download Count":
                    filter = OrderByFilters.DownloadCount;
                    decending = false;
                    break;
            }

            await UpdateLevelPacks(filter);

            e.Handled = true;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedId > -1)
            { 
                await DownloadPack(_selectedId);
            }
        }

        
        private void DownloadedLevelPacks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1) { return; }
            var selection = e.AddedItems[0] as dynamic;
            if (selection is not null)
            {   if((selection as string).ToUpper().Contains("- ACTIVE")) { return; }

                _currentSelection = selection as string;
                _activePack = _currentSelection;
                UpdateAvailablePackList();
            }
        }
    }
}
