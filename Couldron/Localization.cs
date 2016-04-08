﻿using System;
using System.Globalization;

namespace Couldron
{
    /// <summary>
    /// Provides methods regarding localization
    /// </summary>
    [Factory(typeof(Localization), FactoryCreationPolicy.Singleton)]
    public sealed class Localization
    {
        private ILocalizationSource source;

        /// <summary>
        /// Initiates a new instance of the <see cref="Localization"/> class
        /// </summary>
        /// <param name="localizationSource"></param>
        [Inject]
        public Localization(ILocalizationSource localizationSource)
        {
            this.source = localizationSource;
            this.CultureInfo = CultureInfo.InstalledUICulture;

            if (this.source == null)
                throw new Exception("There is no valid implementation of 'ILocalizationSource' found");
        }

        /// <summary>
        /// Gets or sets the culture used for the localization. Default is <see cref="CultureInfo.InstalledUICulture"/>
        /// </summary>
        public CultureInfo CultureInfo { get; set; }

        /// <summary>
        /// Gets the localized string with the specified key
        /// </summary>
        /// <param name="key">The key of the localized string</param>
        /// <returns>The localized string</returns>
        public string this[string key]
        {
            get
            {
                if (string.IsNullOrEmpty(key))
                    return string.Empty;

                if (this.source.Contains(key, this.CultureInfo.TwoLetterISOLanguageName))
                    return this.source.GetValue(key, this.CultureInfo.TwoLetterISOLanguageName);

                return key + "●"; // ● indicates that the localization was not provided. Someone has to do his homework
            }
        }
    }
}