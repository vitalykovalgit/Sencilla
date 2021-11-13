using Android.OS;
using Android.Views;
using Android.Content;
using CheeseBind;
using AndroidX.Fragment.App;

namespace Sencilla.Mobile.Xamarin.Droid.UI.Fragments
{
    public class InjectableFragment : Fragment
    {
        protected int LayoutResId = 0;

        public InjectableFragment()
        {
        }

        public InjectableFragment(int layoutResId)
        {
            LayoutResId = layoutResId;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Inject all dependencies 
            Activity.ApplicationContext.Inject(this);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Inject views 
            var view = inflater.Inflate(LayoutResId, container, false);
            Cheeseknife.Bind(this, view);

            return view;
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();
            Cheeseknife.Reset(this);
        }
    }
}