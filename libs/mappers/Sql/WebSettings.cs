using System.Configuration;

namespace Sencilla.Infrastructure.SqlMapper.Impl
{
    /// <summary>
    /// 
    /// </summary>
    public class Settings : ConfigurationSection
    {
        /// <summary>
        /// Instance of the settings 
        /// </summary>
        public static Settings Instance => ConfigurationManager.GetSection("SqlMapper") as Settings;

        /// <summary>
        /// User name 
        /// </summary>
        [ConfigurationProperty(nameof(DefaultPrimaryKeyName), IsRequired = false, DefaultValue = "Id")]
        public string DefaultPrimaryKeyName
        {
            get { return (string)this[nameof(DefaultPrimaryKeyName)]; }
            set { this[nameof(DefaultPrimaryKeyName)] = value; }
        }

        [ConfigurationProperty(nameof(DefaultSchema), IsRequired = false, DefaultValue = null)] //"dbo"
        public string DefaultSchema
        {
            get { return (string)this[nameof(DefaultSchema)]; }
            set { this[nameof(DefaultSchema)] = value; }
        }

        [ConfigurationProperty(nameof(SpPrefix), IsRequired = false, DefaultValue = "crud_")]
        public string SpPrefix
        {
            get { return (string)this[nameof(SpPrefix)]; }
            set { this[nameof(SpPrefix)] = value; }
        }

        [ConfigurationProperty(nameof(AccessTokenProvider), DefaultValue = "https://database.windows.net/")]
        public string AccessTokenProvider => (string)this[nameof(AccessTokenProvider)];

        [ConfigurationProperty(nameof(UseAccessToken), DefaultValue = false)]
        public bool UseAccessToken => (bool)this[nameof(UseAccessToken)];
    }
}
