﻿namespace Nop.Services.Directory
{
    /// <summary>
    /// Represents default values related to directory services
    /// </summary>
    public static partial class NopDirectoryDefaults
    {
        #region Countries

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : country ID
        /// </remarks>
        public static string CountriesByIdCacheKey => "Nop.country.id-{0}";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : Two letter ISO code
        /// </remarks>
        public static string CountriesByTwoLetterCodeCacheKey => "Nop.country.twoletter-{0}";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : Two letter ISO code
        /// </remarks>
        public static string CountriesByThreeLetterCodeCacheKey => "Nop.country.threeletter-{0}";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : language ID
        /// {1} : show hidden records?
        /// </remarks>
        public static string CountriesAllCacheKey => "Nop.country.all-{0}-{1}";

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string CountriesPrefixCacheKey => "Nop.country.";

        #endregion

        #region Currencies

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : currency ID
        /// </remarks>
        public static string CurrenciesByIdCacheKey => "Nop.currency.id-{0}";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : show hidden records?
        /// </remarks>
        public static string CurrenciesAllCacheKey => "Nop.currency.all-{0}";

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string CurrenciesPrefixCacheKey => "Nop.currency.";

        #endregion

        #region Measures

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        public static string MeasureDimensionsAllCacheKey => "Nop.measuredimension.all";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : dimension ID
        /// </remarks>
        public static string MeasureDimensionsByIdCacheKey => "Nop.measuredimension.id-{0}";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        public static string MeasureWeightsAllCacheKey => "Nop.measureweight.all";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : weight ID
        /// </remarks>
        public static string MeasureWeightsByIdCacheKey => "Nop.measureweight.id-{0}";

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string MeasureDimensionsPrefixCacheKey => "Nop.measuredimension.";

        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string MeasureWeightsPrefixCacheKey => "Nop.measureweight.";

        #endregion

        #region States and provinces

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : country ID
        /// {1} : language ID
        /// {2} : show hidden records?
        /// </remarks>
        public static string StateProvincesAllCacheKey => "Nop.stateprovince.all-{0}-{1}-{2}";

        /// <summary>
        /// Gets a key for caching
        /// </summary>
        /// <remarks>
        /// {0} : abbreviation
        /// {1} : country ID
        /// </remarks>
        public static string StateProvincesByAbbreviationCacheKey => "Nop.stateprovince.abbreviationcountryid-{0}";


        /// <summary>
        /// Gets a key pattern to clear cache
        /// </summary>
        public static string StateProvincesPrefixCacheKey => "Nop.stateprovince.";

        #endregion
    }
}