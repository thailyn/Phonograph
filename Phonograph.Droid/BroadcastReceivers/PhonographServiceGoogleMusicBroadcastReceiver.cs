using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;

namespace Phonograph.Droid.BroadcastReceivers
{
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
}

