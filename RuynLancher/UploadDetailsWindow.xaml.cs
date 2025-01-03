﻿using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Text.RegularExpressions;
using System;
using System.Windows;
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
            LevelPackAuthor.Text = RuynLancher.SaveData.DisplayName;
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
                File.Delete(zipFilePath);
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

        private static bool ValidateLevelName(string name)
        {
            var filename = Path.GetFileName(name).ToUpper();
            filename = filename.Split(".")[0];
            bool isOk =  Regex.IsMatch(filename, @"^LEVEL([0-8][0-9]|9[0-8])$");
            return isOk;
        }
        private static bool ValidateLevel(string filePath)
        {         
            if (!File.Exists(filePath)) { return false; }

            using (FileStream fs = new(filePath, FileMode.Open))
            {
                if (fs != null)
                {
                    if (fs.Length != LEVEL_SIZE_FOR_VALIDATION)
                    {
                        return false;
                    }
                    try
                    {
                        Level l = new();
                        l.Deserialise(new BinaryReader(fs));
                        if (l.stepSize > Level.MAX_STEP_SIZE)
                        {
                            return false;
                        }
                        if (l.ceilHeight > Level.MAX_WALL_SIZE)
                        {
                            return false;
                        }
                        if (l.floorHeight > Level.MAX_WALL_SIZE)
                        {
                            return false;
                        }
                        if (l.PlayerStart[0] > 64 || l.PlayerStart[1] > 64)
                        {
                            return false;
                        }
                        if (l.doorLevitation > Level.MAX_WALL_SIZE)
                        { 
                            return false;
                        }                        
                        return true;
                    }
                    catch
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
                if (IsValidLevelName(Path.GetFileName(file)) 
                    && ValidateLevel(file) 
                    && ValidateLevelName(file))
                {
                    validFileNames.Add(file);
                    levelCount++;
                }
            }

            if (levelCount < 1)
            {
                MessageBox.Show("Could not find any levels in selected pack", "Nope!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
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

            MessageBox.Show($"Uploaded level pack {LevelPackName} with {levelCount} levels ", "Yay!", MessageBoxButton.OK, MessageBoxImage.Information);
            this.DialogResult = true;
            this.Close();
            File.Delete(zipFilePath);
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

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
