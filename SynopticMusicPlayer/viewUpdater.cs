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
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace SynopticMusicPlayer
{
   
    class viewUpdater
    {
        SqlConnection connection;
        private DataGrid songsDataGrid;
        private Label noSongsFoundLabel;
        private string connectionString;
        private ComboBox playlistPickerComboBox;
        private Image playButtonImg;
        private string currentAlbum;
        private Label currentlyPlayingLabel;


        public viewUpdater(DataGrid dataGrid, Label noSongsFoundLabel, string connectionString, ComboBox playlistPickerComboBox, Image playButtonImg, Label currentlyPlayingLabel)
        {
            this.songsDataGrid = dataGrid;
            this.noSongsFoundLabel = noSongsFoundLabel;
            this.connectionString = connectionString;
            this.playlistPickerComboBox = playlistPickerComboBox;
            this.playButtonImg = playButtonImg;
            this.currentlyPlayingLabel = currentlyPlayingLabel;
        }


        public void updateCurrentlyPlayingLabel(int currentTableIndex, bool musicPlaying)
        {

            if (musicPlaying == true)
            {
                TextBlock name = songsDataGrid.Columns[1].GetCellContent(songsDataGrid.Items[currentTableIndex]) as TextBlock;
                TextBlock artist = songsDataGrid.Columns[2].GetCellContent(songsDataGrid.Items[currentTableIndex]) as TextBlock;
                currentlyPlayingLabel.Content = name.Text + " - " + artist.Text;
            }
            else
            {
                currentlyPlayingLabel.Content = "Nothing";

            }
        }
        public void PopulateSongs(string folderLocation) //Populate DataGrid with ALL SONGS from database
        {
            using (connection = new SqlConnection(connectionString))
            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Songs", connection))
            {
                DataTable songTable = new DataTable();
                adapter.Fill(songTable);
                songsDataGrid.ItemsSource = songTable.DefaultView;
            }
            if (songsDataGrid.Items.Count == 0)
            {
                noSongsFoundLabel.Content = "No songs found in: " + folderLocation;
                noSongsFoundLabel.Visibility = Visibility.Visible;
            }
            else
            {
                noSongsFoundLabel.Visibility = Visibility.Hidden;
            }
        }

        public void updateDataGrid(string folderLocation, bool musicPlaying, int? albumIndex)
        {
            var selection = playlistPickerComboBox.SelectedValue;
            playButtonImg.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\play.png")));
            if (selection.ToString() != "All Songs" && selection.ToString() != "--Albums--")
            {
                if (playlistPickerComboBox.SelectedIndex < albumIndex) //Selected a playlist
                {
                    var query = "SELECT [Song ID] FROM Playlist WHERE PlaylistName='" + selection + "'";
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
                                    string[] newData = str.Split(',');
                                    //MessageBox.Show(newData);
                                    DataTable songTable = new DataTable();
                                    for (int i = 0; i < newData.Length; i++)
                                    {
                                        if (!String.IsNullOrWhiteSpace(newData[i]))
                                        {
                                            reader.Close();
                                            using (SqlDataAdapter adapter = new SqlDataAdapter("SELECT * FROM Songs WHERE Id=" + newData[i], connection))
                                            {
                                                adapter.Fill(songTable);
                                                songsDataGrid.ItemsSource = songTable.DefaultView;
                                                musicPlaying = false;
                                                updateCurrentlyPlayingLabel(0, musicPlaying);
                                                playButtonImg.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\play.png")));
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("Null exception");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        var ad = new SqlDataAdapter("SELECT * FROM Songs WHERE Album = @album", connection);
                        ad.SelectCommand.Parameters.AddWithValue("@album", selection);
                        DataTable songTable = new DataTable();
                        ad.Fill(songTable);
                        songsDataGrid.ItemsSource = songTable.DefaultView;
                        currentAlbum = selection.ToString();
                        playlistPickerComboBox.SelectedValue = (selection);
                        updateCurrentlyPlayingLabel(0, musicPlaying);
                        playButtonImg.Source = new BitmapImage(new Uri(System.IO.Path.Combine(Environment.CurrentDirectory, @"Icons\play.png")));
                    }
                }
            }
            else
            {
                playlistPickerComboBox.SelectedValue = "All Songs";
                PopulateSongs(folderLocation);
            }
        }


    }
    }

