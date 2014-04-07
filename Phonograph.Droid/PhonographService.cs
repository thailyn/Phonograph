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
    }
}