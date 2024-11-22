using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Compression;
using Microsoft.VisualBasic;
using System.Windows.Controls;
using System.Linq;
using System.Text;
using static RuynLancher.Constants;
using static RuynLancher.UserUtils;
using System.Windows.Input;

namespace RuynLancher
{
    public static class SaveData
    {
        public static string ActivePack = string.Empty;
        public static string DisplayName = string.Empty;
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int MAX_INPUT_LENGTH = 50;
        const string ACTIVE_STRING = " ✔";
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
            string path = Path.Combine(GAME_FILE_LOCATION, LEVELS_FOLDER);
            if (!Directory.Exists(path)) { return; }

            DownloadedLevelPacks.Items.Clear();
            foreach (var levelpack in Directory.GetDirectories(path))
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
            DisplayNameTextBox.Text = SaveData.DisplayName;
            await UpdateLevelPacks();
            UpdateAvailablePackList();
        }

        private async Task RunUpvote(string levelPackName)
        {
            await Server.Get().UpvoteAsync(levelPackName, GetUserId());       
        }

        private async Task RunDownVote(string levelPackName)
        {
            await Server.Get().DownvoteAsync(levelPackName, GetUserId());
        }

        private async void UpvoteButton_Click(object sender, RoutedEventArgs e)
        {
            var f = e as dynamic;
            var levelPackName = f.OriginalSource.DataContext.LevelPackName;
            if (levelPackName is not null)
            { 
                await RunUpvote(levelPackName);
                await UpdateLevelPacks();
            }
            e.Handled = true;
        }

        private void DownloadedLevelPacks_KeyDown(object sender, KeyEventArgs e)
        {   
            string folderPath = Path.Combine(GAME_FILE_LOCATION, LEVELS_FOLDER, SaveData.ActivePack).Replace(" ", "_");
            if (e.Key == Key.F2)
            {   
                string input = Interaction.InputBox($"Please enter the new name for {SaveData.ActivePack}:", "Rename your level pack", "");
                if (input.Length > MAX_INPUT_LENGTH)
                {
                    MessageBox.Show($"Exceeded max pack name of {MAX_INPUT_LENGTH} characters", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (input.Length < 1)
                {
                    MessageBox.Show($"Please enter a valid name", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                else 
                { 
                    try 
                    {
                        string newPath = Path.Combine(GAME_FILE_LOCATION, LEVELS_FOLDER, input.Replace(" ", "_"));
                        SaveData.ActivePack = input;
                        Directory.Move(folderPath, newPath);
                        UpdateAvailablePackList();
                    }
                    catch
                    {
                        MessageBox.Show($"Unable to rename level pack", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else if (e.Key == Key.Delete)
            {
                MessageBoxResult result = MessageBox.Show($"Are you sure you want to delete {SaveData.ActivePack} and all its levels??", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // TODO: These string replaces are in too many places now, need to move to a method
                    try
                    {
                        foreach (var file in Directory.GetFiles(folderPath))
                        {
                            File.Delete(file);
                        }

                        Directory.Delete(folderPath);
                        MessageBox.Show($"Pack deleted", "Done!", MessageBoxButton.OK, MessageBoxImage.Information);
                        SaveData.ActivePack = string.Empty;
                        UpdateAvailablePackList();
                        SaveSettings();
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show($"Unable to delete level pack", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }   
                }
            }
            
        }

        private async void DownvoteButton_Click(object sender, RoutedEventArgs e)
        {
            var f = e as dynamic;
            var levelPackName = f.OriginalSource.DataContext.LevelPackName;
            if (levelPackName is not null)
            {
                await RunDownVote(levelPackName);
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

            args += $" -u {SaveData.DisplayName}";

            args += $" -c {GetUserId()}";

            Process.Start(new ProcessStartInfo
            {
                FileName = $"{GAME_FILE_LOCATION}\\{EXE_NAME}.exe",
               // WorkingDirectory = GAME_FILE_LOCATION,
               
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
                // WorkingDirectory = GAME_FILE_LOCATION,
                Arguments = $" { SaveData.ActivePack.Replace(" ", "_")}" 
            });
        }

        private void Launch2DEditor_Click(object sender, RoutedEventArgs e)
        {
            if (!CheckForActiveLevelBack()) { return; }

            Environment.CurrentDirectory = GAME_FILE_LOCATION;
            Process.Start(new ProcessStartInfo
            {
                FileName = $"{GAME_FILE_LOCATION}\\{TWO_D_EDITOR_NAME}.exe",
               // WorkingDirectory = GAME_FILE_LOCATION,
                Arguments = $" {SaveData.ActivePack.Replace(" ", "_")}"
            });
        }

        private void ShowInputDialog_Click()
        {
            // Create and show the input dialog
            UploadDetailsWindow inputDialog = new UploadDetailsWindow(this);
            bool? result = inputDialog.ShowDialog();
        }

        private void UploadLevels_Click(object sender, RoutedEventArgs e)
        {
            ShowInputDialog_Click();
        }

        private static string? _selectedPackName = null;        

        private void LevelPackDataGrid_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count < 1) { return; }
            var data = e.AddedItems[0] as dynamic;
            string? levelPackName = data?.LevelPackName;

            if (levelPackName is not null)
            {
                _selectedPackName = levelPackName;
            }

            DownloadButton.IsEnabled = levelPackName is not null;
        }

        private async Task DownloadPack(string levelPackName3)
        {
            try
            {
                LevelData downloadedPack = await Server.Get().GetLevelPackByNameAsync(levelPackName3);
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
                MessageBox.Show($"Level pack downloaded!", "Yay!", MessageBoxButton.OK, MessageBoxImage.Information);
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

        public async Task UpdateLevelPacks()
        {
           LoadingSpinner.Visibility = Visibility.Visible;
           ICollection<LevelListResponse> levelPacks = await Server.Get().GetLevelListAsync(_searchTerm, 0, 20, _currentFilter, _decending);
           LevelPackDataGrid.ItemsSource = levelPacks.Select(x => new { x.LevelPackName, UploadDate = x.UploadDate.ToString()[..10],x.Author, x.LevelCount, x.DownloadCount, x.Ranking });
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
            if (_selectedPackName is not null)
            { 
                await DownloadPack(_selectedPackName);
            }
        }

        private void LoadSettings()
        {
            string path = Path.Combine(GAME_FILE_LOCATION, SETTINGS_SAVE_FILE_NAME);
            if (File.Exists(path))
            {
                var saveData = File.ReadAllBytes(path);
                using var memoryStream = new MemoryStream(saveData);
                using var reader = new BinaryReader(memoryStream, Encoding.UTF8, true);
                SaveData.ActivePack = reader.ReadString();
                SaveData.DisplayName = reader.ReadString();
            }
            else
            {
                UpdateDisplayName("Welcome to Ruyn! Please enter a display name");
            }
        }
        
        private void SaveSettings()
        {
            if(!Directory.Exists(GAME_FILE_LOCATION)) { return; }
            using var memoryStream = new MemoryStream();

            using var writer = new BinaryWriter(memoryStream, Encoding.UTF8, true);
            writer.Write(SaveData.ActivePack);
            writer.Write(SaveData.DisplayName);
            var bd = memoryStream.ToArray();
            File.WriteAllBytes(Path.Combine(GAME_FILE_LOCATION, SETTINGS_SAVE_FILE_NAME), bd);
        }

        private void DownloadedLevelPacks_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count < 1) { return; }
            var selection = e.AddedItems[0] as dynamic;
            if (selection is not null)
            {   
                if((selection as string).ToUpper().Contains(ACTIVE_STRING)) { return; }

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

            if (input.Length >= MAX_INPUT_LENGTH)
            {
                MessageBox.Show($"Your level pack name must be under {MAX_INPUT_LENGTH} characters", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void UpdateDisplayName(string message)
        {
            while (true)
            {
                string input = Interaction.InputBox(message, "Update display name", "");
                if (input.Length <= MAX_INPUT_LENGTH)
                {
                    if (string.IsNullOrEmpty(input))
                    {
                        input = "Anonymous";
                    }
                    SaveData.DisplayName = input;
                    break;
                }
                else
                {
                    MessageBox.Show($"Display name cannot exceed {MAX_INPUT_LENGTH} characters. Please try again.");
                }
            }
            
            
            DisplayNameTextBox.Text = SaveData.DisplayName;
            SaveSettings();
        }

        private void EditDisplayName_Click(object sender, RoutedEventArgs e)
        {
            UpdateDisplayName("Please enter your new Display Name");
        }
    }
}
