using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace SynopticMusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SqlConnection connection;
        string connectionString;
        MediaPlayer player;
        bool musicPlaying;
        bool songSelected;
        bool isCreatingPlaylist;
        int currentSongID;

        public MainWindow()
        {
            connectionString = ConfigurationManager.ConnectionStrings["SynopticMusicPlayer.Properties.Settings.SongsConnectionString"].ConnectionString;

            InitializeComponent();
            setUp();
            PopulateSongs();
        }

        private void PopulateSongs()
        {
            using (connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Song", connection))
            {
                DataTable songTable = new DataTable();
                adapter.Fill(songTable);
                songsDataGrid.ItemsSource = songTable.DefaultView;
            }
        }

        private void songsDataGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                try
                {
                    var index = dataGrid.SelectedIndex + 2;
                    string dir = directoryReturner(index);
                    if (isCreatingPlaylist != true)
                    {

                        player.Open(new Uri(dir));
                        musicPlaying = true;
                        songSelected = true;
                        playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
                        currentSongID = index;
                        songsDataGrid.SelectedItems.Clear();
                        player.Play();

                    }
                    else if (isCreatingPlaylist == true) {

                    }
                }
                catch
                {

                }
            }
        }

        private string directoryReturner(int index)
        {
            var query = "SELECT Directory FROM Song WHERE Id = " + index;

            using (connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(query, connection);
                string dir = (string)command.ExecuteScalar();
                connection.Close();
                //MessageBox.Show(dir);
                return dir;
            }

        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            player.Volume = volumeSlider.Value / 100;
        }

        private void setUp()
        {
            var query = "SELECT PlaylistName FROM Playlist";

            playlistPickerComboBox.Items.Add("All Songs");
            List<String> nameData = new List<String>();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var str = reader.GetString(0);
                            if (str != null)
                            {
                                nameData.Add(str);

                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            for (var i = 0; i < nameData.Count; i++)
            {
                playlistPickerComboBox.Items.Add(nameData[i]);
            }

            player = new MediaPlayer();
            player.Volume = 0;
            volumeSlider.Value = 25;
            playlistPickerComboBox.Items.Add(" ");
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isCreatingPlaylist == false)
            {
                if (musicPlaying == true)//user pauses
                {
                    playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\play.png")));
                    musicPlaying = false;
                    player.Pause();
                }
                else if (musicPlaying == false && songSelected == true)//user resumes
                {
                    playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));

                    musicPlaying = true;
                    player.Play();
                }
                else
                {
                }
            }
            else
            {
                MessageBox.Show("You are currently creating a playlist");
            }
        }



        private void nextBtnImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int index = currentSongID + 1;
            string dir = directoryReturner(index);

            if (index != songsDataGrid.Items.Count + 2 && dir != null && isCreatingPlaylist == false)
            {

                player.Open(new Uri(dir));
                musicPlaying = true;
                songSelected = true;
                playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
                currentSongID = index;
                songsDataGrid.CurrentItem = index;
                player.Play();
            }
            else
            {
                if (dir == null)
                {

                }
                else if (isCreatingPlaylist == true)
                {
                    MessageBox.Show("You are currently creating a playlist");
                }
                else
                {
                    MessageBox.Show("No More Songs");
                }
            }

        }

        private void prevBtnImg_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int index = currentSongID - 1;
            string dir = directoryReturner(index);
            if (index > 1 && dir != null && isCreatingPlaylist == false)
            {
                player.Open(new Uri(dir));
                musicPlaying = true;
                songSelected = true;
                playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
                currentSongID = index;
                songsDataGrid.CurrentItem = index;
                player.Play();
            }
            else
            {
                if (dir == null)
                {

                }
                else if (isCreatingPlaylist == true)
                {
                    MessageBox.Show("You are currently creating a playlist");
                }
                else
                {
                    MessageBox.Show("No More Songs");
                }
            }
        }


        private void createPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            isCreatingPlaylist = true;
            if (playlistWindowGrid.Visibility != Visibility.Visible)
            {

                playlistWindowGrid.Visibility = Visibility.Visible;
            }
            else
            {
                playlistWindowGrid.Visibility = Visibility.Hidden;

            }
        }

        private void cancelPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            playlistWindowGrid.Visibility = Visibility.Hidden;
            isCreatingPlaylist = false;

        }

        private void savePlaylistNameBtn_Click(object sender, RoutedEventArgs e)
        {
            string playlistName = playlistNameTxtBox.Text;
            if (!string.IsNullOrWhiteSpace(playlistName))
            {
                playlistWindowGrid.Visibility = Visibility.Hidden;
                finishPlaylistBtn.Visibility = Visibility.Visible;
                cancelPlaylistBtn.Visibility = Visibility.Visible;
                isCreatingPlaylist = true;
            }
            else
            {
                playlistNameTxtBox.Text = "Invalid Name";

            }

        }

        private void cancelPlaylistBtn_Click_1(object sender, RoutedEventArgs e)
        {
            finishPlaylistBtn.Visibility = Visibility.Hidden;
            cancelPlaylistBtn.Visibility = Visibility.Hidden;
            isCreatingPlaylist = false;
        }

        private void updateDataGrid()
        {

        }

  

        private void playlistPickerComboBox_DropDownClosed(object sender, EventArgs e)
        {
            var selection = playlistPickerComboBox.SelectedItem;

            var query = "SELECT [Song ID] FROM Playlist WHERE PlaylistName='" + selection + "'";

            if (selection.ToString() != "All Songs")
            {
                MessageBox.Show(selection.ToString());
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            reader.Read();
                            var str = reader.GetString(0);
                            if (str != null)
                            {
                                //var idData = str.Split(',');
                                var newData = string.Join("", str.Split(','));
                                MessageBox.Show(newData);

                                for (int i = 0; i < newData.Length; i++)
                                {
                                    reader.Close();
                                    using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Song WHERE Id=" + newData[i], connection))
                                    {
                                        DataTable songTable = new DataTable();
                                        adapter.Fill(songTable);
                                        songsDataGrid.ItemsSource = songTable.DefaultView;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                PopulateSongs();
            }
        }

    }            
        }
    

