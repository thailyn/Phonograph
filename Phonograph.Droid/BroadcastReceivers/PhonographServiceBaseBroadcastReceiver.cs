using System;
using Android.Content;
using Phonograph.Model;
using Android.Widget;

namespace Phonograph.Droid.BroadcastReceivers
{
    public abstract class PhonographServiceBaseBroadcastReceiver : BroadcastReceiver
    {
        protected string _currentTrackTitle;
        protected string _currentAlbumTitle;
        protected string _currentArtistName;
        protected long _currentPosition;
        protected long _currentDuration;
        protected bool _isPlaying;

        protected long _cumulativePlayedTime;

        protected System.Diagnostics.Stopwatch _currentElapsedTimer;
        protected System.Diagnostics.Stopwatch _sinceLastUpdateTimer;

        public PhonographServiceBaseBroadcastReceiver()
        {
            _currentElapsedTimer = new System.Diagnostics.Stopwatch();
            _sinceLastUpdateTimer = new System.Diagnostics.Stopwatch();
        }

        public void RecordPlay(string trackTitle, string albumTitle, string artistName, DateTime timePlayed,
            string sourceName)
        {
            string applicationDirectory = System.IO.Path.Combine("/storage", "emulated", "legacy", "Phonograph");
            if (!System.IO.Directory.Exists(applicationDirectory))
            {
                System.IO.Directory.CreateDirectory(applicationDirectory);
            }

            string databasePath = System.IO.Path.Combine(applicationDirectory, "PlaysDatabase.sqlite3");
            var pdb = new Phonograph.Model.PhonographDatabase(databasePath);

            Artist artist = null;
            try
            {
                artist = (from a in pdb.Table<Artist>()
                    where a.Name == artistName
                    select a).FirstOrDefault();
                if (artist == null)
                {
                    artist = new Artist
                    {
                        Name = artistName,
                        SortName = artistName
                    };
                    pdb.Insert(artist);
                }
            }
            catch (SQLite.SQLiteException ex)
            {
                Android.Util.Log.Error("PHONOGRAPH", "SQLite Exception when fetching artist: " + ex.Message);
                return;
            }

            Album album = null;
            try
            {
                album = (from a in pdb.Table<Album>()
                    where a.Title == albumTitle
                    where a.AlbumArtistId == artist.Id
                    select a).FirstOrDefault();
                if (album == null)
                {
                    album = new Album
                    {
                        Title = albumTitle,
                        SortTitle = albumTitle,
                        AlbumArtistId = artist.Id
                    };
                    pdb.Insert(album);
                }
            }
            catch (SQLite.SQLiteException ex)
            {
                Android.Util.Log.Error("PHONOGRAPH", "SQLite Exception when fetching album: " + ex.Message);
                return;
            }

            Track track = null;
            try
            {
                track = (from t in pdb.Table<Track>()
                    where t.Title == trackTitle
                    where t.ArtistId == artist.Id
                    where t.AlbumId == album.Id
                    select t).FirstOrDefault();
                if (track == null)
                {
                    track = new Track
                    {
                        Title = trackTitle,
                        AlbumId = album.Id,
                        ArtistId = artist.Id
                    };
                    pdb.Insert(track);
                }
            }
            catch (SQLite.SQLiteException ex)
            {
                Android.Util.Log.Error("PHONOGRAPH", "SQLite Exception when fetching track: " + ex.Message);
                return;
            }

            Source source = null;
            try
            {
                source = (from s in pdb.Table<Source>()
                    where s.Name == sourceName
                    select s).FirstOrDefault();
                if (source == null)
                {
                    source = new Source
                    {
                        Name = sourceName,
                    };
                    pdb.Insert(source);
                }
            }
            catch (SQLite.SQLiteException ex)
            {
                Android.Util.Log.Error("PHONOGRAPH", "SQLite Exception when fetching source: " + ex.Message);
                return;
            }

            try
            {
                pdb.Insert(new Play
                    {
                        TrackId = track.Id,
                        SourceId = source.Id,
                        Time = timePlayed
                    });
            }
            catch (SQLite.SQLiteException ex)
            {
                Android.Util.Log.Error("PHONOGRAPH", "SQLite Exception when inserting play: " + ex.Message);
                return;
            }
        }

        public void UpdateState(Context context, string currentTrackTitle, string currentAlbumTitle,
            string currentArtistName, long currentPosition, long duration, bool isPlaying, long lastTrackPosition,
            string sourceName, bool verbose = false)
        {
            string message;

            _sinceLastUpdateTimer.Stop();

            // See how long it's been since the last time the state changed.
            long timeSinceLastUpdate = _sinceLastUpdateTimer.ElapsedMilliseconds;
            if (_currentElapsedTimer.IsRunning)
            {
                _cumulativePlayedTime += timeSinceLastUpdate;
            }

            if (!(string.Equals(_currentTrackTitle, currentTrackTitle)
                && string.Equals(_currentAlbumTitle, currentAlbumTitle)
                && string.Equals(_currentArtistName, currentArtistName)))
            {
                message = string.Format("New track: {0}, {1}, {2}, {3}/{4} ({5}%), {6}",
                    currentArtistName, currentAlbumTitle, currentTrackTitle, currentPosition,
                    duration, ((double)currentPosition) / duration, isPlaying);
                Android.Util.Log.Debug("PHONOGRAPH", message);
                if (verbose) Toast.MakeText(context, message, ToastLength.Short).Show();

                if (_currentDuration > 1
                    && (double)_cumulativePlayedTime > (double)_currentDuration * 0.9)
                {
                    message = "Recording last track now.";
                    Android.Util.Log.Debug("PHONOGRAPH", message);
                    if (verbose) Toast.MakeText(context, message, ToastLength.Short).Show();

                    RecordPlay(_currentTrackTitle, _currentAlbumTitle, _currentArtistName, DateTime.UtcNow,
                        sourceName);
                }
                else
                {
                    message = string.Format("Did not play track long enough before changing. {0} <= {1}",
                        (double)_cumulativePlayedTime, (double)_currentDuration * 0.9);
                    Android.Util.Log.Debug("PHONOGRAPH", message);
                    if (verbose) Toast.MakeText(context, message, ToastLength.Short).Show();
                }

                // Regardless of whether we recorded the play or not, we are in a new
                // song, so reset everything.
                _currentTrackTitle = currentTrackTitle;
                _currentAlbumTitle = currentAlbumTitle;
                _currentArtistName = currentArtistName;
                _currentDuration = duration;
                _cumulativePlayedTime = 0;

                // Start the timer for the [new] current song from 0.
                _currentElapsedTimer.Restart();
            }
            else
            {
                message = string.Format("Same track: {0}, {1}, {2}, {3}/{4} ({5}%), {6}",
                    currentArtistName, currentAlbumTitle, currentTrackTitle, currentPosition,
                    duration, ((double)currentPosition) / duration, isPlaying);
                Android.Util.Log.Debug("PHONOGRAPH", message);
                if (verbose) Toast.MakeText(context, message, ToastLength.Short).Show();

                if (_currentDuration > 1
                    && (double)_cumulativePlayedTime > (double)_currentDuration * 0.9)
                {
                    message = string.Format("Recording current track now. {0} > {1}",
                        _cumulativePlayedTime, (double)_currentDuration * 0.9);
                    Android.Util.Log.Debug("PHONOGRAPH", message);
                    if (verbose) Toast.MakeText(context, message, ToastLength.Short).Show();

                    RecordPlay(_currentTrackTitle, _currentAlbumTitle, _currentArtistName, DateTime.UtcNow,
                        sourceName);

                    _cumulativePlayedTime = 0;
                }
                else
                {
                    message = string.Format("Have not played current track long enough yet. {0} <= {1}",
                        (double)_cumulativePlayedTime, (double)_currentDuration * 0.9);
                    Android.Util.Log.Debug("PHONOGRAPH", message);
                    if (verbose) Toast.MakeText(context, message, ToastLength.Short).Show();
                }
            }

            if (isPlaying && !_currentElapsedTimer.IsRunning)
            {
                message = "Started current elapsed timer.";
                Android.Util.Log.Debug("PHONOGRAPH", message);
                if (verbose) Toast.MakeText(context, message, ToastLength.Short).Show();

                _currentElapsedTimer.Start();
            }

            if (!isPlaying && _currentElapsedTimer.IsRunning)
            {
                message = "Stopped current elapsed timer.";
                Android.Util.Log.Debug("PHONOGRAPH", message);
                if (verbose) Toast.MakeText(context, message, ToastLength.Short).Show();

                _currentElapsedTimer.Stop();
            }

            _sinceLastUpdateTimer.Reset();
            _sinceLastUpdateTimer.Start();
        }
    }
}

