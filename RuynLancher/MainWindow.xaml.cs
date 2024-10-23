using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace RuynLancher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string EDITOR_NAME = "NekoNeo";
        const string EXE_NAME = "Ruyn";
        const string LEVELS_FOLDER = @"\Levels";
#if DEBUG
        const string GAME_FILE_LOCATION = @"c:\projects\NekoEngine";

#else
        const string GAME_FILE_LOCATION = @".\";
#endif

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            bool windowed = false;
            if (Windowed?.IsChecked is not null)
            {
                windowed = Windowed.IsChecked.Value;
            }

            Environment.CurrentDirectory = GAME_FILE_LOCATION;
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

        private static bool IsValidLevelName(string? fileName)
        {
            Regex regex = new Regex(@"^level\d{2}\.had$", RegexOptions.IgnoreCase);
            if (fileName is not null)
            { 
                return regex.IsMatch(fileName);
            }

            return false;
        }

        private void UploadLevels_Click(object sender, RoutedEventArgs e)
        {
            string folderName = string.Empty;

            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            openFolderDialog.Title = "Select a folder";

            var folderDialog = new OpenFolderDialog { };

            if (folderDialog.ShowDialog() == true)
            {
                folderName = folderDialog.FolderName;
            }

            var fileNames = Directory.EnumerateFiles(folderName).Select(x => Path.GetFileName(x));

            List<string> validFileNames = [];

            foreach (var file in fileNames)
            {
                if (IsValidLevelName(file))
                {
                    validFileNames.Add(file);
                }
            }
        }
    }
}
