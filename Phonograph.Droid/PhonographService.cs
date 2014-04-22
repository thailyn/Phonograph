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

        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Android.Util.Log.Debug ("PhonographService", "PhonographService started");

            Toast.MakeText(this, "The phonograph service has started", ToastLength.Long).Show();

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
    }
}