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
using System.IO.Compression;
using static RuynLancher.Constants;

namespace RuynLancher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
    }
}
