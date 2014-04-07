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
        public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
        {
            Android.Util.Log.Debug ("PhonographService", "PhonographService started");

            Toast.MakeText(this, "The phonograph service has started", ToastLength.Long).Show();

            return StartCommandResult.Sticky;
        }

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }
    }
}