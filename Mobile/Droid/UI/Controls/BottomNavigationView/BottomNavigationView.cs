
using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using AndroidX.ViewPager.Widget;
using AndroidX.ViewPager2.Widget;
using Google.Android.Material.BottomNavigation;
using System;
using static Google.Android.Material.BottomNavigation.BottomNavigationView;

namespace Sencilla.Mobile.Xamarin.Droid.UI.Controls
{
    public class SencillaBottomNavigationView : BottomNavigationView, IOnNavigationItemSelectedListener
    {
        ViewPager mViewPager;

        public SencillaBottomNavigationView(Context context) : base(context)
        {
        }

        public SencillaBottomNavigationView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public SencillaBottomNavigationView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        protected SencillaBottomNavigationView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public bool OnNavigationItemSelected(IMenuItem selectedItem)
        {
            var size = Menu.Size();
            for (var idx = 0; idx < size; idx++)
            {
                var item = Menu.GetItem(idx);
                if (item.ItemId == selectedItem.ItemId)
                {
                    mViewPager?.SetCurrentItem(idx, true);
                    return true;
                }
            }

            return false;
        }

        public SencillaBottomNavigationView SetViewPager(ViewPager viewPager)
        {
            mViewPager = viewPager;
            SetOnNavigationItemSelectedListener(this);

            return this;
        }
    }
}