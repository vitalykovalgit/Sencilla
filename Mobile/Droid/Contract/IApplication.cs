﻿using Android.Content;

namespace Android.App
{
    /// <summary>
    /// 
    /// </summary>
    public interface IApplication
    {
        /// <summary>
        /// 
        /// </summary>
        Context AppContext { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="resorceId"></param>
        /// <returns></returns>
        string GetString(int resorceId);

        /// <summary>
        /// Retrice instance for provided type 
        /// </summary>
        TType R<TType>();
    }
}