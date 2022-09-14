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
        int tableIndex;
        string playlistName;
        string playlistIDs;

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
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Songs", connection))
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
                    tableIndex = dataGrid.SelectedIndex;
                    //MessageBox.Show(tableIndex.ToString());

                    TextBlock id = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;
                    currentSongID = Int32.Parse(id.Text);
                    if (isCreatingPlaylist != true)
                    {
                        string dir = directoryReturner(Int32.Parse(id.Text));
                        player.Open(new Uri(dir));
                        musicPlaying = true;
                        songSelected = true;
                        playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
                        currentSongID = Int32.Parse(id.Text);
                        songsDataGrid.SelectedItems.Clear();
                        updateCurrentlyPlayingLabel(tableIndex);
                        player.Play();



                    }
                    else if (isCreatingPlaylist == true)
                    {
                        DataGridRow row = (DataGridRow)((DataGrid)sender).ItemContainerGenerator.ContainerFromIndex(tableIndex);
                        row.Background = new SolidColorBrush(Colors.Green);
                        playlistIDs += id.Text + ",";
                        // Get playlist name
                        //get row IDs
                        // on finish - join IDs together with commas
                        //insert into table
                    }
                }
                catch
                {

                }
            }
        }

        private string directoryReturner(int index)
        {



            var query = "SELECT Directory FROM Songs WHERE Id = " + index;

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

            int index = currentSongID;

            if ((tableIndex + 1) < songsDataGrid.Items.Count)
            {

                tableIndex = tableIndex + 1;

                TextBlock id = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;
                currentSongID = Int32.Parse(id.Text);
                string dir = directoryReturner(Int32.Parse(id.Text));
                //MessageBox.Show(id.Text); //outputs 2

                if (dir != null && isCreatingPlaylist == false)
                {

                    player.Open(new Uri(dir));
                    musicPlaying = true;
                    songSelected = true;
                    playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
                    songsDataGrid.CurrentItem = index;
                    updateCurrentlyPlayingLabel(tableIndex);
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
        }

        private void prevBtnImg_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int index = currentSongID;

            if ((tableIndex - 1) >= 0)
            {
                tableIndex = tableIndex - 1;

                TextBlock id = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;
                currentSongID = Int32.Parse(id.Text);
                string dir = directoryReturner(Int32.Parse(id.Text));
                //MessageBox.Show(id.Text); //outputs 2


                if (dir != null && isCreatingPlaylist == false)
                {
                    player.Open(new Uri(dir));
                    musicPlaying = true;
                    songSelected = true;
                    playBtnImage.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\pause.png")));
                    songsDataGrid.CurrentItem = index;
                    updateCurrentlyPlayingLabel(tableIndex);
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
        }

        private void createPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            isCreatingPlaylist = true;

            if(playlistNameWindow.IsOpen != true)
            {
                playlistNameWindow.IsOpen = true;
            }
            else
            {
                playlistNameWindow.IsOpen = false;
            }

            //if (playlistWindowGrid.Visibility != Visibility.Visible)
            //{

            //    playlistWindowGrid.Visibility = Visibility.Visible;
            //}
            //else
            //{
            //    playlistWindowGrid.Visibility = Visibility.Hidden;

            //}
        }

        private void cancelPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            playlistWindowGrid.Visibility = Visibility.Hidden;
            isCreatingPlaylist = false;
            playlistName = null;

        }

        private void savePlaylistNameBtn_Click(object sender, RoutedEventArgs e)
        {
            playlistName = playlistNameTxtBox.Text;
            if (!string.IsNullOrWhiteSpace(playlistName))
            {
                playlistWindowGrid.Visibility = Visibility.Hidden;
                finishPlaylistBtn.Visibility = Visibility.Visible;
                cancelPlaylistBtn.Visibility = Visibility.Visible;
                isCreatingPlaylist = true;
            }
            else
            {
                playlistName = null;
                playlistNameTxtBox.Text = "Invalid Name";

            }

        }

        private void cancelPlaylistBtn_Click_1(object sender, RoutedEventArgs e)
        {
            finishPlaylistBtn.Visibility = Visibility.Hidden;
            cancelPlaylistBtn.Visibility = Visibility.Hidden;
            isCreatingPlaylist = false;
            playlistName = null;

        }




        private void playlistPickerComboBox_DropDownClosed(object sender, EventArgs e)
        {
            var selection = playlistPickerComboBox.SelectedItem;

            var query = "SELECT [Song ID] FROM Playlist WHERE PlaylistName='" + selection + "'";

            if (selection.ToString() != "All Songs")
            {
                //MessageBox.Show(selection.ToString());
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
                                //MessageBox.Show(newData);
                                DataTable songTable = new DataTable();

                                for (int i = 0; i < newData.Length; i++)
                                {
                                    reader.Close();
                                    using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Songs WHERE Id=" + newData[i], connection))
                                    {
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

        private void updateCurrentlyPlayingLabel(int tableIndex)
        {
            TextBlock name = songsDataGrid.Columns[0].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;
            TextBlock artist = songsDataGrid.Columns[1].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;

            currentlyPlayingLabel.Content = "Currently Playing: " + name.Text + " By " + artist.Text;
        }

        private void finishPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            var query = "INSERT INTO Playlist(PlaylistName, [Song ID]) VALUES('thisisatest', '4,2,3')";
            MessageBox.Show(query);

            //INSERT INTO Playlist(PlaylistName, [Song ID]) VALUES('@playlistName', '@PlaylistIDs')
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand insert = new SqlCommand(query, connection);

                insert.ExecuteNonQuery();
                connection.Close();
            }
        }
    }
}
    

