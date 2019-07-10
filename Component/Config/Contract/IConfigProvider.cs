﻿
namespace Sencilla.Component.Config.Contract
{
    public interface IConfigProvider<TConfig> where TConfig: class
    {
        /// <summary>
        /// Retrieve config for specified type 
        /// </summary>
        /// <returns>Instance of TConfig</returns>
        TConfig GetConfig();
    }
}
