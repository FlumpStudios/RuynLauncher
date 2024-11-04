using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Windows;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Linq;
using static RuynLancher.Constants;

namespace RuynLancher
{

    /// <summary>
    /// Interaction logic for UploadDetailsWindow.xaml
    /// </summary>
    public partial class UploadDetailsWindow : Window
    {
        public string LevelPackName = string.Empty;
        public string Author = string.Empty;
        public readonly MainWindow _mainwindow;

        public UploadDetailsWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            LevelPackNameInput.Text = RuynLancher.SaveData.ActivePack;
            _mainwindow = mainWindow;
        }
        private static bool IsValidLevelName(string? fileName)
        {
            Regex regex = new Regex(@"^level\d{2}\.had$", RegexOptions.IgnoreCase);
            if (fileName is not null)
            {
                return regex.IsMatch(fileName);
            }

            return false;
        }

        static void ZipFiles(List<string> filePaths, string zipFilePath)
        {
            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath); // Delete if exists
            }

            using (FileStream zipToOpen = new FileStream(zipFilePath, FileMode.Create))
            using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
            {
                foreach (string filePath in filePaths)
                {
                    if (File.Exists(filePath))
                    {
                        string fileName = Path.GetFileName(filePath);
                        archive.CreateEntryFromFile(filePath, fileName);
                    }
                    else
                    {
                        Console.WriteLine($"File not found: {filePath}");
                    }
                }
            }
        }

        private static bool ValidateLevel(string filePath)
        {
            if (!File.Exists(filePath)) { return false; }

            using (FileStream fs = new(filePath, FileMode.Open))
            {
                if (fs != null)
                {
                    try
                    {
                        Level l = new();
                        l.Deserialise(new BinaryReader(fs));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        private async Task UploadLevelPack()
        {
            LevelPackName = LevelPackNameInput.Text;
            Author = LevelPackAuthor.Text;

            string folderName = Path.Combine(GAME_FILE_LOCATION, LEVELS_FOLDER, RuynLancher.SaveData.ActivePack.Replace(" ","_")) ;

            //OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            //openFolderDialog.Title = "Select a folder";

            //var folderDialog = new OpenFolderDialog { };

            //if (folderDialog.ShowDialog() == true)
            //{
            //    folderName = folderDialog.FolderName;
            //}

            var fileNames = Directory.EnumerateFiles(folderName);

            if (!fileNames.Any())
            {
                MessageBox.Show($"No levels found in pack", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            List<string> validFileNames = [];
            int levelCount = 0;
            foreach (var file in fileNames)
            {
                if (IsValidLevelName(Path.GetFileName(file)) && ValidateLevel(file))
                {
                    validFileNames.Add(file);
                    levelCount++;
                }
            }

            string zipFilePath = $"{LEVELS_FOLDER}\\levelPack.rpk";

            ZipFiles(validFileNames, zipFilePath);

            const int SQL_CONFLICT_ERROR_CODE = 409;
            try
            {   
                await Server.Get().AddLevelPackAsync(new LevelData
                {
                    Author = string.IsNullOrEmpty(Author) ? "Anonymous" : Author,
                    LevelCount = levelCount,
                    UploadDate = DateTime.Now,
                    FileData = File.ReadAllBytes(zipFilePath),
                    LevelPackName = LevelPackName,
                    ClientId = UserUtils.GetUserId()
                });
            }
            catch (ApiException ex)
            {
                if (ex.StatusCode == SQL_CONFLICT_ERROR_CODE)
                {
                    MessageBox.Show($"{ex.Response}", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                { 
                    MessageBox.Show($"Could not upload file", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }

            MessageBox.Show($"File uploaded successfully", "Yay!", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
            await _mainwindow.UpdateLevelPacks();
        }

        private void InputTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[a-zA-Z0-9 ]*$");
            e.Handled = !regex.IsMatch(e.Text);
        }


        private async void OK_Click(object sender, RoutedEventArgs e)
        {
            await UploadLevelPack();
        }

        private void LevelPackNameInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if(LevelPackNameInput.Text.Length > 0)
            {
                SubmitButton.IsEnabled = true;
            }
            else
            {
                SubmitButton.IsEnabled = false;
            }
        }
    }
}
