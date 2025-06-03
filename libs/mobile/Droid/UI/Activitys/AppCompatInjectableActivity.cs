using Android.Content;
using Android.OS;
using Android.Runtime;
using AndroidX.AppCompat.App;
using CheeseBind;
//using Xamarin.Essentials;

namespace Sencilla.Mobile.Xamarin.Droid.UI.Activity
{
    public class AppCompatInjectableActivity : AppCompatActivity
    {
        protected int LayoutResId = 0;

        public AppCompatInjectableActivity()
        { 
        }

        public AppCompatInjectableActivity(int layoutId)
        {
            LayoutResId = layoutId;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            //Platform.Init(this, savedInstanceState);

            ApplicationContext.Inject(this);

            SetContentView(LayoutResId);
            Cheeseknife.Bind(this);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Cheeseknife.Reset(this);
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum]Android.Content.PM.Permission[] grantResults)
        {
            //Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
}