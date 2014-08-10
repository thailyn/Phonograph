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
using Phonograph.Droid.BroadcastReceivers;

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
            spotifyMusicIntentFilter.AddAction("com.spotify.music.metadatachanged");
            spotifyMusicIntentFilter.AddAction("com.spotify.music.playbackstatechanged");
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
    }
}