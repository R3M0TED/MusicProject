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
using System.IO;

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
        string folderLocation = @"C:\Users\user\source\repos\SynopticMusicPlayer\MusicProject\SynopticMusicPlayer\Music\";

        public MainWindow()
        {
            connectionString = ConfigurationManager.ConnectionStrings["SynopticMusicPlayer.Properties.Settings.SongsConnectionString"].ConnectionString;

            InitializeComponent();
            setUp();
            PopulateSongs();
        }

        private void PopulateSongs() //Populate DataGrid with ALL SONGS
        {
            using (connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Songs", connection))
            {
                DataTable songTable = new DataTable();
                adapter.Fill(songTable);
                songsDataGrid.ItemsSource = songTable.DefaultView;
            }
        }

        private void songsDataGrid_MouseDown(object sender, MouseButtonEventArgs e)//user clicks a song within the datagrid
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                try
                {
                    tableIndex = dataGrid.SelectedIndex;//Get clicked index
                    //MessageBox.Show(tableIndex.ToString());

                    TextBlock id = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;//Save Song ID from selected row index
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
                dir = folderLocation + dir;
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
            checkForUpdates();
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
        }

        private void cancelPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            playlistNameWindow.IsOpen = false;
            isCreatingPlaylist = false;
            finishPlaylistBtn.Visibility = Visibility.Hidden;
            cancelPlaylistBtn.Visibility = Visibility.Hidden;
            playlistName = null;

        }

        private void savePlaylistNameBtn_Click(object sender, RoutedEventArgs e)
        {
            playlistName = playlistNameTxtBox.Text;
            if (!string.IsNullOrWhiteSpace(playlistName))
            {
                playlistNameWindow.IsOpen = false;
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


        private void checkForUpdates()
        {
            string path = @"C:\Users\user\source\repos\SynopticMusicPlayer\MusicProject\SynopticMusicPlayer\Music";
            int fileCount = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).Length;
            int rowCount = 0;
            string rowCountQuery = "SELECT COUNT(*) FROM Songs";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand count = new SqlCommand(rowCountQuery, connection);

                rowCount = (int) count.ExecuteScalar();


                SqlCommand addSong = connection.CreateCommand();
                addSong.CommandText = "INSERT INTO Songs(Name, Artist, Directory) VALUES('hg', 'asd', 'sdfsdf')";
                addSong.ExecuteNonQuery();
                connection.Close();
            }




            //if (fileCount != rowCount)
            //{
            //    string[] fileEntries = Directory.GetFiles(path);

            //    for (int i = 0; i < fileEntries.Length; i++)
            //    {
            //        string filename = Path.GetFileName(fileEntries[i]);

            //        string returnFileNameQuery = "SELECT * FROM Songs WHERE Directory = '" + filename + "'";

            //        using (SqlConnection connection = new SqlConnection(connectionString))
            //        {
            //            connection.Open();
            //            SqlCommand search = new SqlCommand(returnFileNameQuery, connection);


            //            if (search.ExecuteScalar() == null)
            //            {
            //                var file = TagLib.File.Create(fileEntries[i]);
            //                var songTitle = file.Tag.Title;
            //                var songArtists = "";
            //                if (songTitle == null)//blank title use file name
            //                {
            //                    songTitle = filename;
            //                }

            //                string[] songArtistArray = file.Tag.AlbumArtists;

            //                if (songArtistArray.Length == 0)
            //                {
            //                    songArtists = "Blank";
            //                }
            //                else
            //                {
            //                    songArtists = songArtistArray[0];
            //                }

            //                var insertSongQuery = "INSERT INTO Songs(Name, Artist, Directory) VALUES(@name, @artist, @dir)";


            //                SqlCommand addSong = connection.CreateCommand();
            //                addSong.Parameters.AddWithValue("@name", songTitle);
            //                addSong.Parameters.AddWithValue("@artist", songArtists);
            //                addSong.Parameters.AddWithValue("@dir", filename);

            //                SqlTransaction transaction;
            //                transaction = connection.BeginTransaction();
            //                addSong.Connection = connection;
            //                addSong.Transaction = transaction;
            //                try
            //                {
            //                    addSong.CommandText = "INSERT INTO Songs(Name, Artist, Directory) VALUES(@name, @artist, @dir)";
            //                    var test = addSong.ExecuteNonQuery();
            //                    MessageBox.Show(test.ToString());
            //                    addSong.ExecuteNonQuery();
            //                    transaction.Commit();
            //                }
            //                catch (Exception ex)
            //                {
            //                    MessageBox.Show("Error");

            //                    Console.WriteLine("Commit Exception Type: {0}", ex.GetType());
            //                    Console.WriteLine("  Message: {0}", ex.Message);
            //                    try //Attempt to roll back transaction
            //                    {
            //                        transaction.Rollback();
            //                    }
            //                    catch (Exception ex2)
            //                    {
            //                        Console.WriteLine("Rollback Exception Type: {0}", ex2.GetType());
            //                        Console.WriteLine("  Message: {0}", ex2.Message);
            //                    }
            //                }




            //                add songtitle, artist and file name
            //            }

            //        }


            //   }
            //}
        }
        private void updateCurrentlyPlayingLabel(int tableIndex)
        {
            TextBlock name = songsDataGrid.Columns[0].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;
            TextBlock artist = songsDataGrid.Columns[1].GetCellContent(songsDataGrid.Items[tableIndex]) as TextBlock;

            currentlyPlayingLabel.Content = "Currently Playing: " + name.Text + " By " + artist.Text;
        }

        private void finishPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            isCreatingPlaylist = false;
            MessageBox.Show(playlistName + " " + playlistIDs);
            var insertPlaylistQuery = "INSERT INTO Playlist(PlaylistName, [Song ID]) VALUES(@playlistName, @songIDs)";
            MessageBox.Show(insertPlaylistQuery);

            //INSERT INTO Playlist(PlaylistName, [Song ID]) VALUES('@playlistName', '@PlaylistIDs')
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand insert = new SqlCommand(insertPlaylistQuery, connection);
                insert.Parameters.AddWithValue("@playlistName", playlistName);
                insert.Parameters.AddWithValue("@songIDs", playlistIDs);
                finishPlaylistBtn.Visibility = Visibility.Hidden;
                cancelPlaylistBtn.Visibility = Visibility.Hidden;

                insert.ExecuteNonQuery();
                connection.Close();
                playlistPickerComboBox.Items.Add(playlistName);
            }
        }


    }
}
    

