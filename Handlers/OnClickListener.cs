using Android.Views;
using System;
using static Android.Views.View;

namespace CameraX
{
    class OnClickListener : Java.Lang.Object, IOnClickListener
    {
        private readonly Action action;

        public OnClickListener(Action callback)
        {
            this.action = callback;
        }

        public void OnClick(View v)
        {
            this.action?.Invoke();
        }      
    }
}