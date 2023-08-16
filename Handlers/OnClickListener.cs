using System;
using Android.Views;
using static Android.Views.View;

namespace CameraX.Handlers
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