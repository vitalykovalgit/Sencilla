using Android.Runtime;
using AndroidX.Fragment.App;
using System;

namespace Sencilla.Mobile.Xamarin.Droid.UI.Controls
{
    public class SencillaViewPagerAdapter : FragmentPagerAdapter
    {
        Fragment[] mFragments;

        public SencillaViewPagerAdapter(FragmentManager fm, Fragment[] fragments) : base(fm)
        {
            mFragments = fragments;
        }

        public override int Count => mFragments.Length;

        public override Fragment GetItem(int position)
        {
            return mFragments[position];
        }
    }
}