using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace Phonograph.Droid.BroadcastReceivers
{
    public class PhonographServiceRocketPlayerBroadcastReceiver : PhonographServiceBaseBroadcastReceiver
    {
        private string _source = "Rocket Player";
        private List<string> _dumpedCollections = new List<string>();

        public PhonographServiceRocketPlayerBroadcastReceiver()
            : base()
        {
            _verbose = false;
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

            if (_verbose)
            {
                Toast.MakeText (context, string.Format ("Rocket Player action ({0}): {1}, {2}, {3}, {4}, {5}",
                    intent.Action, artist, album, track, length, isPlaying), ToastLength.Long).Show ();
            }

            UpdateState(context, track, album, artist, 0, length, isPlaying, -1, _source, _verbose);
        }
    }
}

