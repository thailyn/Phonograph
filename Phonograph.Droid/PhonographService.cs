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

namespace Phonograph.Droid
{
    [Service]
    [IntentFilter(new String[] { "com.zodiac.PhonographService" })]
    class PhonographService : Service
    {
        private IBinder _binder;
        private PhonographServiceGoogleMusicBroadcastReceiver _receiver;

        public override void OnCreate()
        {
            base.OnCreate();

            Android.Util.Log.Debug("PhonographService", "PhonographService started");

            IntentFilter iF = new IntentFilter();
            iF.AddAction("com.android.music.metachanged");
            iF.AddAction("com.android.music.playstatechanged");
            //iF.AddAction("com.android.music.playbackcomplete");
            //iF.AddAction("com.android.music.queuechanged");

            _receiver = _receiver ?? new PhonographServiceGoogleMusicBroadcastReceiver();
            RegisterReceiver(_receiver, iF);

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

            public void UpdateState(Context context, string currentTrackTitle, string currentAlbumTitle,
                string currentArtistName, long currentPosition, long duration, bool isPlaying, long lastTrackPosition)
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
                    Toast.MakeText(context, message, ToastLength.Short).Show();

                    if (_currentDuration > 1
                        && (double)_cumulativePlayedTime > (double)_currentDuration * 0.9)
                    {
                        message = "Would record last play here.";
                        Android.Util.Log.Debug("PHONOGRAPH", message);
                        Toast.MakeText(context, message, ToastLength.Short).Show();
                    }
                    else
                    {
                        message = string.Format("Did not play track long enough before changing. {0} <= {1}",
                            (double)_cumulativePlayedTime, (double)_currentDuration * 0.9);
                        Android.Util.Log.Debug("PHONOGRAPH", message);
                        Toast.MakeText(context, message,
                            ToastLength.Short).Show();
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
                    Toast.MakeText(context, message, ToastLength.Short).Show();

                    if (_currentDuration > 1
                        && (double)_cumulativePlayedTime > (double)_currentDuration * 0.9)
                    {
                        message = string.Format("Would record current track here. {0} > {1}",
                            _cumulativePlayedTime, (double)_currentDuration * 0.9);
                        Android.Util.Log.Debug("PHONOGRAPH", message);
                        Toast.MakeText(context, message, ToastLength.Short).Show();

                        _cumulativePlayedTime = 0;
                    }
                    else
                    {
                        message = string.Format("Have not played current track long enough yet. {0} <= {1}",
                            (double)_cumulativePlayedTime, (double)_currentDuration * 0.9);
                        Android.Util.Log.Debug("PHONOGRAPH", message);
                        Toast.MakeText(context, message,
                            ToastLength.Short).Show();
                    }
                }

                if (isPlaying && !_currentElapsedTimer.IsRunning)
                {
                    message = "Started current elapsed timer.";
                    Android.Util.Log.Debug("PHONOGRAPH", message);
                    Toast.MakeText(context, message, ToastLength.Short).Show();

                    _currentElapsedTimer.Start();
                }

                if (!isPlaying && _currentElapsedTimer.IsRunning)
                {
                    message = "Stopped current elapsed timer.";
                    Android.Util.Log.Debug("PHONOGRAPH", message);
                    Toast.MakeText(context, message, ToastLength.Short).Show();

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
                        Android.Util.Log.Error("PHONOGRAPH", "Dumping Intent Start - " + action);
                        foreach (var key in keys)
                        {
                            Android.Util.Log.Error("PHONOGRAPH", string.Format("[{0}] - [{1}]", key, bundle.Get(key)));
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

                UpdateState(context, track, album, artist, position, duration, isPlaying, lastTrackPosition);
            }
        }
    }
}