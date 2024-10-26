using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Windows;
using Microsoft.Win32;
using static RuynLancher.Constants;
using System.Drawing.Printing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RuynLancher
{

    /// <summary>
    /// Interaction logic for UploadDetailsWindow.xaml
    /// </summary>
    public partial class UploadDetailsWindow : Window
    {
        public string LevelPackName = string.Empty;
        public string Author = string.Empty;

        public UploadDetailsWindow()
        {
            InitializeComponent();
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

            string folderName = string.Empty;

            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.Title = "Select a folder";

            var folderDialog = new OpenFolderDialog { };

            if (folderDialog.ShowDialog() == true)
            {
                folderName = folderDialog.FolderName;
            }

            var fileNames = Directory.EnumerateFiles(folderName);

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

            // Call the method to zip files
            ZipFiles(validFileNames, zipFilePath);

            try
            {   
                await Server.Get().AddLevelPackAsync(new LevelData
                {
                    Author = string.IsNullOrEmpty(Author) ? "Anonymous" : Author,
                    LevelCount = levelCount,
                    UploadDate = DateTime.Now,
                    FileData = File.ReadAllBytes(zipFilePath),
                    LevelPackName = LevelPackName
                });
            }
            catch (ApiException ex)
            {
                MessageBox.Show($"Could not upload file", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            this.DialogResult = true;
            this.Close();
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
