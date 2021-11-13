

using Android.Content;
using Android.Runtime;
using Android.Util;
using AndroidX.Fragment.App;
using AndroidX.ViewPager.Widget;
using System;

namespace Sencilla.Mobile.Xamarin.Droid.UI.Controls
{
    public class SencillaViewPager : ViewPager
    {
        public SencillaViewPager(Context context) : base(context)
        {
        }

        public SencillaViewPager(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public SencillaViewPager AddPages(FragmentManager frm, params Fragment[] fragments)
        {
            Adapter = new SencillaViewPagerAdapter(frm, fragments);
            return this;
        }
    }
}