﻿using System;

namespace PeanutButter.Utils
{
    public class AutoResetter: IDisposable
    {
        private object _lock = new object();
        private Action _disposalAction;

        public AutoResetter(Action constructionAction, Action disposalAction)
        {
            constructionAction();
            _disposalAction = disposalAction;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposalAction != null)
                {
                    try
                    {
                        _disposalAction();
                    }
                    finally
                    {
                        _disposalAction = null;
                    }
                }
            }
        }
    }
}