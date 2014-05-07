using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

using Phonograph.Model;

namespace Phonograph.Droid
{
    [Service]
    [IntentFilter(new String[] { "com.zodiac.PhonographService" })]
    class PhonographService : Service
    {
        private IBinder _binder;
        private PhonographServiceGoogleMusicBroadcastReceiver _googleMusicReceiver;
        private PhonographServiceSpotifyBroadcastReceiver _spotifyMusicReceiver;
        private PhonographServiceRocketPlayerBroadcastReceiver _rocketPlayerMusicReceiver;

        public override void OnCreate()
        {
            base.OnCreate();

            Android.Util.Log.Debug("PhonographService", "PhonographService started");

            IntentFilter googleMusicIntentFilter = new IntentFilter();
            googleMusicIntentFilter.AddAction("com.android.music.metachanged");
            googleMusicIntentFilter.AddAction("com.android.music.playstatechanged");
            //iF.AddAction("com.android.music.playbackcomplete");
            //iF.AddAction("com.android.music.queuechanged");

            _googleMusicReceiver = _googleMusicReceiver ?? new PhonographServiceGoogleMusicBroadcastReceiver();
            RegisterReceiver(_googleMusicReceiver, googleMusicIntentFilter);

            IntentFilter spotifyMusicIntentFilter = new IntentFilter();
            spotifyMusicIntentFilter.AddAction("com.spotify.mobile.android.metadatachanged");
            spotifyMusicIntentFilter.AddAction("com.spotify.mobile.android.playbackstatechanged");
            _spotifyMusicReceiver = _spotifyMusicReceiver ?? new PhonographServiceSpotifyBroadcastReceiver();
            RegisterReceiver(_spotifyMusicReceiver, spotifyMusicIntentFilter);

            IntentFilter rocketPlayerMusicIntentFilter = new IntentFilter();
            rocketPlayerMusicIntentFilter.AddAction("com.jrtstudio.AnotherMusicPlayer.metachanged");
            rocketPlayerMusicIntentFilter.AddAction("com.jrtstudio.AnotherMusicPlayer.playstatechanged");
            _rocketPlayerMusicReceiver = _rocketPlayerMusicReceiver ?? new PhonographServiceRocketPlayerBroadcastReceiver();
            RegisterReceiver(_rocketPlayerMusicReceiver, rocketPlayerMusicIntentFilter);

            Toast.MakeText(this, "The phonograph service has started", ToastLength.Long).Show();
        }

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            _binder = new PhonographServiceBinder(this);
            return _binder;
        }

        public class PhonographServiceBinder : Binder
        {
            private PhonographService _service;

            public PhonographServiceBinder(PhonographService service)
            {
                _service = service;
            }

            public PhonographService GetPhonographService()
            {
                return _service;
            }
        }

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

        // Have abstract base BroadcastReceiver class which has an UpdateState method.
        // Then have subclasses of that class for each media player program that will be listened to.
        // Listen for metachanged event to see what the current track is.  Start a stopwatch when
        // the track changes (note the position, which is in thousandths of a second).  If
        // playstatechanged event happens, either note new position or pause stopwatch (or restart
        // if it was paused and media is now playing again).  When metachanged event happens again,
        // note the new song and position, but also see if the previous song played 90% of its
        // duration.  If so, record it.  If not, discard it.
        //
        // If track was seeked backwards, add the difference in time to the total needed to record it
        // (duration * 0.9) + amount_seeked.  If track was seeked forwards, either do nothing to the
        // total needed to record it, or add the difference in what was seeked.
        public class PhonographServiceGoogleMusicBroadcastReceiver : PhonographServiceBaseBroadcastReceiver
        {
            string _source = "Google Music";
            List<string> _dumpedCollections = new List<string>();

            public PhonographServiceGoogleMusicBroadcastReceiver()
                : base()
            {

            }

            public override void OnReceive(Context context, Intent intent)
            {
                //String action = intent.Action;
                String action = intent.GetStringExtra("track");
                if (!string.IsNullOrWhiteSpace(action) && !_dumpedCollections.Contains(action))
                //if (!string.IsNullOrWhiteSpace(action))
                {
                    Bundle bundle = intent.Extras;
                    if (bundle != null)
                    {
                        var keys = bundle.KeySet();
                        Android.Util.Log.Debug("PHONOGRAPH", "Dumping Intent Start - " + action);
                        foreach (var key in keys)
                        {
                            Android.Util.Log.Debug("PHONOGRAPH", string.Format("[{0}] - [{1}]", key, bundle.Get(key)));
                        }
                    }
                    _dumpedCollections.Add(action);
                }

                String cmd = intent.GetStringExtra("command");

                String artist = intent.GetStringExtra("artist");
                String album = intent.GetStringExtra("album");
                String track = intent.GetStringExtra("track");
                long position = intent.GetLongExtra("position", -1);
                long duration = intent.GetLongExtra("duration", -1);
                bool isPlaying = intent.GetBooleanExtra("playstate", false) || intent.GetBooleanExtra("playing", false) || intent.GetBooleanExtra("streaming", false);

                long lastTrackPosition = -1;
                if (!(string.Equals(_currentTrackTitle, track)
                    && string.Equals(_currentAlbumTitle, album)
                    && string.Equals(_currentArtistName, artist)))
                {
                    // Google Music seems to give the position in the last track when first
                    // changing tracks.  The second time the event is called (immediately
                    // after the first), the position is correct.
                    lastTrackPosition = position;
                    position = 0;
                }

                //Toast.MakeText(context, string.Format("{0}: {1}, {2}, {3}, {4}/{5}, {6}",
                //    action, artist, album, track, position, duration, isPlaying), ToastLength.Long).Show();

                UpdateState(context, track, album, artist, position, duration, isPlaying, lastTrackPosition,
                    _source);
            }
        }

        public class PhonographServiceSpotifyBroadcastReceiver : PhonographServiceBaseBroadcastReceiver
        {
            string _source = "Spotify";
            List<string> _dumpedCollections = new List<string>();

            private string _newArtistName;
            private string _newAlbumTitle;
            private string _newTrackTitle;
            private long _newTrackLength;
            private bool _lastKnownPlaybackState;

            public PhonographServiceSpotifyBroadcastReceiver()
                : base()
            {

            }

            public override void OnReceive(Context context, Intent intent)
            {
                String action = intent.GetStringExtra("track");
                if (!string.IsNullOrWhiteSpace(action) && !_dumpedCollections.Contains(action))
                {
                    Bundle bundle = intent.Extras;
                    if (bundle != null)
                    {
                        var keys = bundle.KeySet();
                        Android.Util.Log.Debug("PHONOGRAPH", "Dumping Intent Start - " + action);
                        foreach (var key in keys)
                        {
                            Android.Util.Log.Debug("PHONOGRAPH", string.Format("[{0}] - [{1}]", key, bundle.Get(key)));
                        }
                    }
                    _dumpedCollections.Add(action);
                }

                if (intent.Action.Equals("com.spotify.mobile.android.metadatachanged"))
                {
                    String artist = intent.GetStringExtra("artist");
                    String album = intent.GetStringExtra("album");
                    String track = intent.GetStringExtra("track");
                    long length = intent.GetIntExtra("length", -1) * 1000; // Spotify length is in seconds.

                    if (!(string.Equals(_newTrackTitle, track)
                        && string.Equals(_newAlbumTitle, album)
                        && string.Equals(_newArtistName, artist)))
                    {
                        _newArtistName = artist;
                        _newAlbumTitle = album;
                        _newTrackTitle = track;
                        _newTrackLength = length;

                        //Toast.MakeText(context, string.Format("Spotify meta changed: {0}, {1}, {2}, {3}",
                        //    artist, album, track, length), ToastLength.Long).Show();

                        UpdateState(context, _newTrackTitle, _newAlbumTitle, _newArtistName, 0, _newTrackLength,
                            _lastKnownPlaybackState, -1, _source, false);

                    }
                }
                else if (intent.Action.Equals("com.spotify.mobile.android.playbackstatechanged"))
                {
                    long position = intent.GetIntExtra("playbackPosition", -1);
                    bool isPlaying = intent.GetBooleanExtra("playing", false);
                    _lastKnownPlaybackState = isPlaying;

                    //Toast.MakeText(context, string.Format("Spotify playback state changed: {0}, {1}",
                    //    position, isPlaying), ToastLength.Long).Show();

                    UpdateState(context, _newTrackTitle, _newAlbumTitle, _newArtistName, position, _newTrackLength,
                        isPlaying, -1, _source, false);
                }
            }
        }

        public class PhonographServiceRocketPlayerBroadcastReceiver : PhonographServiceBaseBroadcastReceiver
        {
            string _source = "Rocket Player";
            List<string> _dumpedCollections = new List<string>();

            private string _newArtistName;
            private string _newAlbumTitle;
            private string _newTrackTitle;
            private long _newTrackLength;
            private bool _lastKnownPlaybackState;

            public PhonographServiceRocketPlayerBroadcastReceiver()
                : base()
            {

            }

            public override void OnReceive(Context context, Intent intent)
            {
                String action = intent.GetStringExtra("track");
                if (!string.IsNullOrWhiteSpace(action) && !_dumpedCollections.Contains(action))
                {
                    Bundle bundle = intent.Extras;
                    if (bundle != null)
                    {
                        var keys = bundle.KeySet();
                        Android.Util.Log.Debug("PHONOGRAPH", "Rocket Player - Dumping Intent Start - " + action);
                        foreach (var key in keys)
                        {
                            Android.Util.Log.Debug("PHONOGRAPH", string.Format("[{0}] - [{1}]", key, bundle.Get(key)));
                        }
                    }
                    _dumpedCollections.Add(action);
                }

                String artist = intent.GetStringExtra("artist");
                String album = intent.GetStringExtra("album");
                String track = intent.GetStringExtra("track");
                long length = intent.GetLongExtra("length", (long) -1);
                bool isPlaying = intent.GetBooleanExtra("playing", false);

                Toast.MakeText(context, string.Format("Rocket Player action ({0}): {1}, {2}, {3}, {4}, {5}",
                    intent.Action, artist, album, track, length, isPlaying), ToastLength.Long).Show();

                UpdateState(context, track, album, artist, 0, length, isPlaying, -1, _source, true);
            }
        }
    }
}