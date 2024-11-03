using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Compression;
using static RuynLancher.Constants;
using Microsoft.VisualBasic;

using System.Windows.Controls;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Drawing.Printing;

namespace RuynLancher
{

    public static class SaveData
    {
        public static string ActivePack = string.Empty;
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string ACTIVE_STRING = " - ACTIVE";
        private static OrderByFilters _currentFilter = OrderByFilters.UploadedDate;
        private static string _searchTerm = string.Empty;
        private static string _currentSelection = string.Empty;
        private static bool _decending = true;

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
                string fileName = Path.GetFileName(levelpack).Replace("_", " ");
                
                if (string.IsNullOrEmpty(SaveData.ActivePack))
                {
                    SaveData.ActivePack = fileName;
                }

                if (fileName == SaveData.ActivePack) { fileName += ACTIVE_STRING; }
                DownloadedLevelPacks.Items.Add(fileName);
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
            await UpdateLevelPacks();
            UpdateAvailablePackList();
        }

        private async Task RunUpvote(int id)
        {
            await Server.Get().UpvoteAsync(id);       
        }

        private async Task RunDownVote(int id)
        {
            await Server.Get().DownvoteAsync(id);
        }

        private async void UpvoteButton_Click(object sender, RoutedEventArgs e)
        {
            var f = e as dynamic;
            var id = f.OriginalSource.DataContext.Id;
            if (id is not null && id > 0)
            { 
                await RunUpvote(id);
                await UpdateLevelPacks();
            }
            e.Handled = true;
        }

        private async void DownvoteButton_Click(object sender, RoutedEventArgs e)
        {
            var f = e as dynamic;
            var id = f.OriginalSource.DataContext.Id;
            if (id is not null && id > 0)
            {
                await RunDownVote(id);
                await UpdateLevelPacks();
            }
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForActiveLevelBack()) { return; }

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

            args += $" -p {SaveData.ActivePack.Replace(" ","_")}";

            Process.Start(new ProcessStartInfo
            {
                FileName = $"{GAME_FILE_LOCATION}\\{EXE_NAME}.exe",
                WorkingDirectory = GAME_FILE_LOCATION,
               
                Arguments = args
            });
        }


        private bool CheckForActiveLevelBack()
        {
            if (string.IsNullOrEmpty(SaveData.ActivePack))
            {
                MessageBox.Show($"There are no level packs acvite", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        private void LaunchEditor_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForActiveLevelBack()) { return; }

            Environment.CurrentDirectory = GAME_FILE_LOCATION; 
            Process.Start(new ProcessStartInfo
            {
                FileName = $"{GAME_FILE_LOCATION}\\{EDITOR_NAME}.exe",
                WorkingDirectory = GAME_FILE_LOCATION,
                Arguments = $" { SaveData.ActivePack.Replace(" ", "_")}" 
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
        private object memoryStream;

        private void LevelPackDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count < 1) { return; }
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

                string directory = Path.Combine(GAME_FILE_LOCATION, LEVELS_FOLDER, downloadedPack.LevelPackName).Replace(" ","_");
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
                MessageBox.Show($"Level pack downloaded!", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (ApiException ex)
            {
                MessageBox.Show($"Server error, could not download file", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something went wrong. Could not save to levels to disk", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        private async Task UpdateLevelPacks()
        {
           LoadingSpinner.Visibility = Visibility.Visible;
           ICollection<LevelListResponse> levelPacks = await Server.Get().GetLevelListAsync(_searchTerm, 0, 20, _currentFilter, _decending);
           LevelPackDataGrid.ItemsSource = levelPacks.Select(x => new { x.Id, UploadDate = x.UploadDate.ToString()[..10], x.LevelPackName, x.Author, x.LevelCount, x.DownloadCount, x.Ranking });
           LoadingSpinner.Visibility = Visibility.Hidden;
        }

        private async void LevelPackDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            // Get the column that was clicked
            var column = e.Column.Header as string;

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
                    decending = true;
                    break;
                case "Download Count":
                    filter = OrderByFilters.DownloadCount;
                    decending = true;
                    break;
            }

            if (filter == _currentFilter)
            {
                return;
            }

            _currentFilter = filter;
            _decending = decending;

            await UpdateLevelPacks();

            e.Handled = true;
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedId > -1)
            { 
                await DownloadPack(_selectedId);
            }
        }

        private void LoadSettings()
        {

            if(File.Exists(SETTINGS_SAVE_FILE_NAME))
            { 
                var saveData = File.ReadAllBytes(SETTINGS_SAVE_FILE_NAME);

                using var memoryStream = new MemoryStream(saveData);
                using var reader = new BinaryReader(memoryStream, Encoding.UTF8, true);
                SaveData.ActivePack = reader.ReadString();
            }

        }
        
        private void SaveSettings()
        {
            using var memoryStream = new MemoryStream();

            using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);
            writer.Write(SaveData.ActivePack);
            var bd = memoryStream.ToArray();
            File.WriteAllBytes(SETTINGS_SAVE_FILE_NAME, bd);
        }

        private void DownloadedLevelPacks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1) { return; }
            var selection = e.AddedItems[0] as dynamic;
            if (selection is not null)
            {   if((selection as string).ToUpper().Contains("- ACTIVE")) { return; }

                _currentSelection = selection as string;
                SaveData.ActivePack = _currentSelection;
                UpdateAvailablePackList();
                SaveSettings();
            }
        }

        private async Task RunSearch()
        {
            _searchTerm = SearchBox.Text;
            await UpdateLevelPacks();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            await RunSearch();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string input = Interaction.InputBox("Please entere a name for your new level pack:", "Name your level pack", "");
            
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            if (input.Length >= 50)
            {
                MessageBox.Show($"Your level pack name must be under 50 characters", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string directory = Path.Combine(GAME_FILE_LOCATION, LEVELS_FOLDER, input.Replace(" ", "_"));

            if (Directory.Exists(directory))
            {
                MessageBox.Show($"Level Pack Already Exists!", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }

            Directory.CreateDirectory(directory);

            MessageBox.Show($"New Level Pack Created!", "Yay!", MessageBoxButton.OK, MessageBoxImage.Information);
            SaveData.ActivePack = input;
            UpdateAvailablePackList();
        }
    }
}
