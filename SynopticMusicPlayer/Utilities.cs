using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Configuration;
using System.Data.OleDb;
using System.Data;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace SynopticMusicPlayer
{
    class Utilities
    {
        OleDbConnection connection;
        private DataGrid songsDataGrid;
        private string connectionString;
        private Label noSongsFoundLabel;
        private ComboBox playlistPickerComboBox;
        private Image playButtonImg;
        private MediaPlayer player;
        private Label currentlyPlayingLabel;

        public Utilities(MediaPlayer player,string connectionString, DataGrid dataGrid,Label currentlyPlayingLabel , Label noSongsFoundLabel, ComboBox playlistPickerComboBox, bool musicPlaying, Image playButtonImg)
        {
            this.player = player;
            this.songsDataGrid = dataGrid;
            this.noSongsFoundLabel = noSongsFoundLabel;
            this.playlistPickerComboBox = playlistPickerComboBox;
            this.playButtonImg = playButtonImg;
            this.currentlyPlayingLabel = currentlyPlayingLabel;
            this.connectionString = connectionString;
        }


        public void checkForUpdates(string folderLocation)
        {
            int fileCount = Directory.GetFiles(folderLocation, "*.*", SearchOption.AllDirectories).Length;
            int rowCount = 0;
            string rowCountQuery = "SELECT COUNT(*) FROM Songs";
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    OleDbCommand count = new OleDbCommand(rowCountQuery, connection);
                    rowCount = (int)count.ExecuteScalar();
                    connection.Close();
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message + "Stacktrace: " + ex.StackTrace);
                }
            }
            if (fileCount != rowCount)
            {
                string[] fileEntries = Directory.GetFiles(folderLocation);
                for (int i = 0; i < fileEntries.Length; i++)
                {
                    string filename = Path.GetFileName(fileEntries[i]);
                    string returnFileNameQuery = "SELECT * FROM Songs WHERE Directory = '" + filename + "'";
                    using (OleDbConnection connection = new OleDbConnection(connectionString))
                    {
                        connection.Open();
                        OleDbCommand search = new OleDbCommand(returnFileNameQuery, connection);
                        if (search.ExecuteScalar() == null)
                        {
                            var file = TagLib.File.Create(fileEntries[i]);
                            var songTitle = file.Tag.Title;
                            var songArtists = "";
                            var songAlbum = file.Tag.Album;
                            var songLength = file.Properties.Duration.Minutes + ":" + file.Properties.Duration.Seconds;
                            if (songTitle == null)//blank title use file name
                            {
                                songTitle = filename;
                            }
                            if (songAlbum == null)
                            {
                                songAlbum = "No Album";
                            }
                            string[] songArtistArray = file.Tag.AlbumArtists;
                            if (songArtistArray.Length == 0)
                            {
                                songArtists = "Blank";
                            }
                            else
                            {
                                songArtists = songArtistArray[0];
                            }
                            OleDbCommand addSong = connection.CreateCommand();
                            addSong.Parameters.AddWithValue("@name", songTitle);
                            addSong.Parameters.AddWithValue("@artist", songArtists);
                            addSong.Parameters.AddWithValue("@dir", filename);
                            addSong.Parameters.AddWithValue("@album", songAlbum);
                            addSong.Parameters.AddWithValue("@length", songLength);
                            try
                            {
                                addSong.CommandText = "INSERT INTO Songs(SongName, Artist, Directory, Album, Length) VALUES(@name, @artist, @dir, @album, @length)";
                                addSong.ExecuteNonQuery();
                            }
                            catch
                            {
                                MessageBox.Show("Failed to insert data");
                            }
                        }
                    }

                }
            }
        }

        

        public DataGridRow getClickedRow(MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            while ((dep != null) && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep == null)
            {

            }
            if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
            }
            return dep as DataGridRow;
        }

        public void loadPlaylistNames(ComboBox comboBox)
        {
            var query = "SELECT PlaylistName FROM Playlist";
            List<String> nameData = new List<String>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    using (OleDbDataReader reader = command.ExecuteReader())
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
                comboBox.Items.Add(nameData[i]);
            }
        }

        public void loadAlbumNames(ComboBox comboBox)
        {
            comboBox.Items.Add("--Albums--");
            var query = "SELECT Album FROM Songs";

            List<String> nameData = new List<String>();
            using (OleDbConnection connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                using (OleDbCommand command = new OleDbCommand(query, connection))
                {
                    using (OleDbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
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
                            catch
                            {

                            }
                        }
                    }
                }
            }
            for (var i = 0; i < nameData.Count; i++)
            {
                if (nameData[i] != null)
                {
                    if (!comboBox.Items.Contains(nameData[i]))
                    {
                        comboBox.Items.Add(nameData[i]);
                    }
                }
            }
        }

        public string directoryReturner(int index, string folderLocation) //Return the directory of a file using its database ID
        {
            var query = "SELECT Directory FROM Songs WHERE Id = @index";
            using (connection = new OleDbConnection(connectionString))
            {
                connection.Open();
                OleDbCommand command = new OleDbCommand(query, connection);
                command.Parameters.AddWithValue("@index", index);
                string dir = (string)command.ExecuteScalar();
                connection.Close();
                dir = folderLocation + @"\" + dir;
                return dir;
            }
        }


        public int getAlbumIndex()
        {
            int AlbumItemIndex;
            for (int i = 0; i < playlistPickerComboBox.Items.Count; i++)
            {
                if ((string)playlistPickerComboBox.Items[i] == "--Albums--")
                {
                    AlbumItemIndex = i;
                    return AlbumItemIndex;
                }
            }
            return 5;
        }

        


    }


}
