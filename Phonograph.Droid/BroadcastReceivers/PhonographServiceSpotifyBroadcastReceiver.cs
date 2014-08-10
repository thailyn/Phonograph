using System;
using System.Collections.Generic;
using Android.Content;
using Android.OS;
using Android.Widget;

namespace Phonograph.Droid.BroadcastReceivers
{
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
                    Android.Util.Log.Debug("PHONOGRAPH", "Dumping Intent Start - " + action);
                    foreach (var key in keys)
                    {
                        Android.Util.Log.Debug("PHONOGRAPH", string.Format("[{0}] - [{1}]", key, bundle.Get(key)));
                    }
                }
                _dumpedCollections.Add(action);
            }

            if (intent.Action.Equals("com.spotify.music.metadatachanged"))
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

                    if (_verbose)
                    {
                        Toast.MakeText (context, string.Format ("Spotify meta changed: {0}, {1}, {2}, {3}",
                            artist, album, track, length), ToastLength.Long).Show ();
                    }

                    UpdateState(context, _newTrackTitle, _newAlbumTitle, _newArtistName, 0, _newTrackLength,
                        _lastKnownPlaybackState, -1, _source, _verbose);

                }
            }
            else if (intent.Action.Equals("com.spotify.music.playbackstatechanged"))
            {
                long position = intent.GetIntExtra("playbackPosition", -1);
                bool isPlaying = intent.GetBooleanExtra("playing", false);
                _lastKnownPlaybackState = isPlaying;

                if (_verbose)
                {
                    Toast.MakeText (context, string.Format ("Spotify playback state changed: {0}, {1}",
                        position, isPlaying), ToastLength.Long).Show ();
                }

                UpdateState(context, _newTrackTitle, _newAlbumTitle, _newArtistName, position, _newTrackLength,
                    isPlaying, -1, _source, _verbose);
            }
        }
    }
}

