﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Shipping;
using Nop.Plugin.Shipping.UPS.Domain;
using Nop.Services;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using Nop.Services.Shipping.Tracking;

namespace Nop.Plugin.Shipping.UPS.Services
{
    /// <summary>
    /// Represents UPS service
    /// </summary>
    public class UPSService
    {
        #region Constants

        /// <summary>
        /// Package weight limit (lbs) https://www.ups.com/us/en/help-center/packaging-and-supplies/prepare-overize.page
        /// </summary>
        private const int WEIGHT_LIMIT = 150;

        /// <summary>
        /// Packahe size limit (inches in length and girth combined) https://www.ups.com/us/en/help-center/packaging-and-supplies/prepare-overize.page
        /// </summary>
        private const int SIZE_LIMIT = 165;

        /// <summary>
        /// Package length limit (inches) https://www.ups.com/us/en/help-center/packaging-and-supplies/prepare-overize.page
        /// </summary>
        private const int LENGTH_LIMIT = 108;

        /// <summary>
        /// Used measure weight code
        /// </summary>
        private const string LBS_WEIGHT_CODE = "LBS";

        /// <summary>
        /// Used measure weight system keyword
        /// </summary>
        private const string LBS_WEIGHT_SYSTEM_KEYWORD = "lb";

        /// <summary>
        /// Used measure dimension code
        /// </summary>
        private const string INCHES_DIMENSION_CODE = "IN";

        /// <summary>
        /// Used measure dimension system keyword
        /// </summary>
        private const string INCHES_DIMENSION_SYSTEM_KEYWORD = "inches";

        #endregion

        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IMeasureService _measureService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IShippingService _shippingService;
        private readonly IWorkContext _workContext;
        private readonly UPSSettings _upsSettings;

        #endregion

        #region Ctor

        public UPSService(CurrencySettings currencySettings,
            ICountryService countryService,
            ICurrencyService currencyService,
            ILocalizationService localizationService,
            ILogger logger,
            IMeasureService measureService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IShippingService shippingService,
            IWorkContext workContext,
            UPSSettings upsSettings)
        {
            _currencySettings = currencySettings;
            _countryService = countryService;
            _currencyService = currencyService;
            _localizationService = localizationService;
            _logger = logger;
            _measureService = measureService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _shippingService = shippingService;
            _workContext = workContext;
            _upsSettings = upsSettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets an attribute value on an enum field value
        /// </summary>
        /// <typeparam name="TAttribute">Type of the attribute</typeparam>
        /// <param name="enumValue">Enum value</param>
        /// <returns>The attribute value</returns>
        private TAttribute GetAttributeValue<TAttribute>(Enum enumValue) where TAttribute : Attribute
        {
            var enumType = enumValue.GetType();
            var enumValueInfo = enumType.GetMember(enumValue.ToString()).FirstOrDefault();
            var attribute = enumValueInfo?.GetCustomAttributes(typeof(TAttribute), false).FirstOrDefault();
            return attribute as TAttribute;
        }

        /// <summary>
        /// Serialize object to XML
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="value">Object to serialize</param>
        /// <returns>XML string</returns>
        private string ToXml<T>(T value)
        {
            using (var writer = new StringWriter())
            {
                using (var xmlWriter = new XmlTextWriter(writer) { Formatting = Formatting.Indented })
                {
                    var xmlSerializer = new XmlSerializer(typeof(T));
                    xmlSerializer.Serialize(xmlWriter, value);
                    return writer.ToString();
                }
            }
        }

        /// <summary>
        /// Get tracking info
        /// </summary>
        /// <param name="request">Request details</param>
        /// <returns>The asynchronous task whose result contains the tracking info</returns>
        private async Task<UPSTrack.TrackResponse> TrackAsync(UPSTrack.TrackRequest request)
        {
            try
            {
                //create client
                var trackPort = _upsSettings.UseSandbox
                    ? UPSTrack.TrackPortTypeClient.EndpointConfiguration.TrackPort
                    : UPSTrack.TrackPortTypeClient.EndpointConfiguration.ProductionTrackPort;
                using (var client = new UPSTrack.TrackPortTypeClient(trackPort))
                {
                    //create object to authenticate request
                    var security = new UPSTrack.UPSSecurity
                    {
                        ServiceAccessToken = new UPSTrack.UPSSecurityServiceAccessToken
                        {
                            AccessLicenseNumber = _upsSettings.AccessKey
                        },
                        UsernameToken = new UPSTrack.UPSSecurityUsernameToken
                        {
                            Username = _upsSettings.Username,
                            Password = _upsSettings.Password
                        }
                    };

                    //save debug info
                    if (_upsSettings.Tracing)
                        _logger.Information($"UPS shipment tracking. Request: {ToXml(new UPSTrack.TrackRequest1(security, request))}");

                    //try to get response details
                    var response = await client.ProcessTrackAsync(security, request);

                    //save debug info
                    if (_upsSettings.Tracing)
                        _logger.Information($"UPS shipment tracking. Response: {ToXml(response)}");

                    return response.TrackResponse;
                }
            }
            catch (FaultException<UPSTrack.ErrorDetailType[]> ex)
            {
                //get error details
                var message = ex.Message;
                if (ex.Detail.Any())
                {
                    message = ex.Detail.Aggregate(message, (details, detail) =>
                        $"{details}{Environment.NewLine}{detail.Severity} error: {detail.PrimaryErrorCode?.Description}");
                }

                //rethrow exception
                throw new Exception(message, ex);
            }
        }

        /// <summary>
        /// Create request details to track shipment
        /// </summary>
        /// <param name="trackingNumber">Tracking number</param>
        /// <returns>Track request details</returns>
        private UPSTrack.TrackRequest CreateTrackRequest(string trackingNumber)
        {
            return new UPSTrack.TrackRequest
            {
                Request = new UPSTrack.RequestType
                {
                    //use the RequestOption field to indicate the specific types of information to receive
                    //15 = POD, Signature Image, COD, Receiver Address, All Activity (all that's available)
                    RequestOption = new[] { "15" }
                },
                InquiryNumber = trackingNumber
            };
        }

        /// <summary>
        /// Prepare shipment status event by the passed track activity
        /// </summary>
        /// <param name="activity">Track activity</param>
        /// <returns>Shipment status event </returns>
        private ShipmentStatusEvent PrepareShipmentStatusEvent(UPSTrack.ActivityType activity)
        {
            var shipmentStatusEvent = new ShipmentStatusEvent();

            try
            {
                //prepare date
                shipmentStatusEvent.Date = DateTime
                    .ParseExact($"{activity.Date} {activity.Time}", "yyyyMMdd HHmmss", CultureInfo.InvariantCulture);

                //prepare address
                var addressDetails = new List<string>();
                if (!string.IsNullOrEmpty(activity.ActivityLocation?.Address?.CountryCode))
                    addressDetails.Add(activity.ActivityLocation.Address.CountryCode);
                if (!string.IsNullOrEmpty(activity.ActivityLocation?.Address?.StateProvinceCode))
                    addressDetails.Add(activity.ActivityLocation.Address.StateProvinceCode);
                if (!string.IsNullOrEmpty(activity.ActivityLocation?.Address?.City))
                    addressDetails.Add(activity.ActivityLocation.Address.City);
                if (activity.ActivityLocation?.Address?.AddressLine?.Any() ?? false)
                    addressDetails.AddRange(activity.ActivityLocation.Address.AddressLine);
                if (!string.IsNullOrEmpty(activity.ActivityLocation?.Address?.PostalCode))
                    addressDetails.Add(activity.ActivityLocation.Address.PostalCode);

                shipmentStatusEvent.CountryCode = activity.ActivityLocation?.Address?.CountryCode;
                shipmentStatusEvent.Location = string.Join(", ", addressDetails);

                if (activity.Status == null)
                    return shipmentStatusEvent;

                //prepare description
                var eventName = string.Empty;
                switch (activity.Status.Type)
                {
                    case "I":
                        switch (activity.Status.Code)
                        {
                            case "DP":
                                eventName = "Plugins.Shipping.Tracker.Departed";
                                break;

                            case "EP":
                                eventName = "Plugins.Shipping.Tracker.ExportScanned";
                                break;

                            case "OR":
                                eventName = "Plugins.Shipping.Tracker.OriginScanned";
                                break;

                            default:
                                eventName = "Plugins.Shipping.Tracker.Arrived";
                                break;
                        }
                        break;

                    case "X":
                        eventName = "Plugins.Shipping.Tracker.NotDelivered";
                        break;

                    case "M":
                        eventName = "Plugins.Shipping.Tracker.Booked";
                        break;

                    case "D":
                        eventName = "Plugins.Shipping.Tracker.Delivered";
                        break;

                    case "P":
                        eventName = "Plugins.Shipping.Tracker.Pickup";
                        break;
                }
                shipmentStatusEvent.EventName = _localizationService.GetResource(eventName);
            }
            catch { }

            return shipmentStatusEvent;
        }

        /// <summary>
        /// Get rates
        /// </summary>
        /// <param name="request">Request details</param>
        /// <returns>The asynchronous task whose result contains the rates info</returns>
        private async Task<UPSRate.RateResponse> GetRatesAsync(UPSRate.RateRequest request)
        {
            try
            {
                //create client
                var ratePort = _upsSettings.UseSandbox
                    ? UPSRate.RatePortTypeClient.EndpointConfiguration.RatePort
                    : UPSRate.RatePortTypeClient.EndpointConfiguration.ProductionRatePort;
                using (var client = new UPSRate.RatePortTypeClient(ratePort))
                {
                    //create object to authenticate request
                    var security = new UPSRate.UPSSecurity
                    {
                        ServiceAccessToken = new UPSRate.UPSSecurityServiceAccessToken
                        {
                            AccessLicenseNumber = _upsSettings.AccessKey
                        },
                        UsernameToken = new UPSRate.UPSSecurityUsernameToken
                        {
                            Username = _upsSettings.Username,
                            Password = _upsSettings.Password
                        }
                    };

                    //save debug info
                    if (_upsSettings.Tracing)
                        _logger.Information($"UPS rates. Request: {ToXml(new UPSRate.RateRequest1(security, request))}");

                    //try to get response details
                    var response = await client.ProcessRateAsync(security, request);

                    //save debug info
                    if (_upsSettings.Tracing)
                        _logger.Information($"UPS rates. Response: {ToXml(response)}");

                    return response.RateResponse;
                }
            }
            catch (FaultException<UPSRate.ErrorDetailType[]> ex)
            {
                //get error details
                var message = ex.Message;
                if (ex.Detail.Any())
                {
                    message = ex.Detail.Aggregate(message, (details, detail) =>
                        $"{details}{Environment.NewLine}{detail.Severity} error: {detail.PrimaryErrorCode?.Description}");
                }

                //rethrow exception
                throw new Exception(message, ex);
            }
        }

        /// <summary>
        /// Create request details to get shipping rates
        /// </summary>
        /// <param name="shippingOptionRequest">Shipping option request</param>
        /// <param name="saturdayDelivery">Whether to get rates for Saturday Delivery</param>
        /// <returns>Rate request details</returns>
        private UPSRate.RateRequest CreateRateRequest(GetShippingOptionRequest shippingOptionRequest, bool saturdayDelivery = false)
        {
            //set request details
            var request = new UPSRate.RateRequest
            {
                Request = new UPSRate.RequestType
                {
                    //used to define the request type
                    //Shop - the server validates the shipment, and returns rates for all UPS products from the ShipFrom to the ShipTo addresses
                    RequestOption = new[] { "Shop" }
                }
            };

            //prepare addresses details
            var stateCodeTo = shippingOptionRequest.ShippingAddress.StateProvince?.Abbreviation;
            var stateCodeFrom = shippingOptionRequest.StateProvinceFrom?.Abbreviation;
            var countryCodeFrom = (shippingOptionRequest.CountryFrom ?? _countryService.GetAllCountries().FirstOrDefault())
                .TwoLetterIsoCode ?? string.Empty;

            var addressFromDetails = new UPSRate.ShipAddressType
            {
                AddressLine = new[] { shippingOptionRequest.AddressFrom },
                City = shippingOptionRequest.CityFrom,
                StateProvinceCode = stateCodeFrom,
                CountryCode = countryCodeFrom,
                PostalCode = shippingOptionRequest.ZipPostalCodeFrom
            };
            var addressToDetails = new UPSRate.ShipToAddressType
            {
                AddressLine = new[] { shippingOptionRequest.ShippingAddress.Address1, shippingOptionRequest.ShippingAddress.Address2 },
                City = shippingOptionRequest.ShippingAddress.City,
                StateProvinceCode = stateCodeTo,
                CountryCode = shippingOptionRequest.ShippingAddress.Country.TwoLetterIsoCode,
                PostalCode = shippingOptionRequest.ShippingAddress.ZipPostalCode,
                ResidentialAddressIndicator = string.Empty
            };

            //set shipment details
            request.Shipment = new UPSRate.ShipmentType
            {
                Shipper = new UPSRate.ShipperType
                {
                    ShipperNumber = _upsSettings.AccountNumber,
                    Address = addressFromDetails
                },
                ShipFrom = new UPSRate.ShipFromType
                {
                    Address = addressFromDetails
                },
                ShipTo = new UPSRate.ShipToType
                {
                    Address = addressToDetails
                }
            };

            //set pickup options and customer classification for US shipments
            if (countryCodeFrom.Equals("US", StringComparison.InvariantCultureIgnoreCase))
            {
                request.PickupType = new UPSRate.CodeDescriptionType
                {
                    Code = GetUpsCode(_upsSettings.PickupType)
                };
                request.CustomerClassification = new UPSRate.CodeDescriptionType
                {
                    Code = GetUpsCode(_upsSettings.CustomerClassification)
                };
            }

            //set negotiated rates details
            if (!string.IsNullOrEmpty(_upsSettings.AccountNumber) && !string.IsNullOrEmpty(stateCodeFrom) && !string.IsNullOrEmpty(stateCodeTo))
            {
                request.Shipment.ShipmentRatingOptions = new UPSRate.ShipmentRatingOptionsType
                {
                    NegotiatedRatesIndicator = string.Empty,
                    UserLevelDiscountIndicator = string.Empty
                };
            }

            //set saturday delivery details
            if (saturdayDelivery)
            {
                request.Shipment.ShipmentServiceOptions = new UPSRate.ShipmentServiceOptionsType
                {
                    SaturdayDeliveryIndicator = string.Empty
                };
            }

            //set packages details
            switch (_upsSettings.PackingType)
            {
                case PackingType.PackByOneItemPerPackage:
                    request.Shipment.Package = GetPackagesForOneItemPerPackage(shippingOptionRequest).ToArray();
                    break;

                case PackingType.PackByVolume:
                    request.Shipment.Package = GetPackagesByCubicRoot(shippingOptionRequest).ToArray();
                    break;

                case PackingType.PackByDimensions:
                default:
                    request.Shipment.Package = GetPackagesByDimensions(shippingOptionRequest).ToArray();
                    break;
            }

            return request;
        }

        /// <summary>
        /// Create package details
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="length">Length</param>
        /// <param name="height">Height</param>
        /// <param name="weight">Weight</param>
        /// <param name="insuranceAmount">Insurance amount</param>
        /// <returns>Package details</returns>
        private UPSRate.PackageType CreatePackage(decimal width, decimal length, decimal height, decimal weight, decimal insuranceAmount)
        {
            //set package details
            var package = new UPSRate.PackageType
            {
                PackagingType = new UPSRate.CodeDescriptionType
                {
                    Code = GetUpsCode(_upsSettings.PackagingType)
                }
            };

            //set dimensions and weight details
            if (!_upsSettings.PassDimensions)
                width = length = height = 0;
            package.Dimensions = new UPSRate.DimensionsType
            {
                Width = width.ToString(),
                Length = length.ToString(),
                Height = height.ToString(),
                UnitOfMeasurement = new UPSRate.CodeDescriptionType { Code = INCHES_DIMENSION_CODE }
            };
            package.PackageWeight = new UPSRate.PackageWeightType
            {
                Weight = weight.ToString(),
                UnitOfMeasurement = new UPSRate.CodeDescriptionType { Code = LBS_WEIGHT_CODE },
            };

            //set insurance details
            if (_upsSettings.InsurePackage && insuranceAmount > decimal.Zero)
            {
                var currencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode;
                package.PackageServiceOptions = new UPSRate.PackageServiceOptionsType
                {
                    Insurance = new UPSRate.InsuranceType
                    {
                        BasicFlexibleParcelIndicator = new UPSRate.InsuranceValueType
                        {
                            CurrencyCode = currencyCode,
                            MonetaryValue = insuranceAmount.ToString()
                        }
                    }
                };
            }

            return package;
        }

        /// <summary>
        /// Create packages (each shopping cart item is a separate package)
        /// </summary>
        /// <param name="shippingOptionRequest">shipping option request</param>
        /// <returns>Packages</returns>
        private IEnumerable<UPSRate.PackageType> GetPackagesForOneItemPerPackage(GetShippingOptionRequest shippingOptionRequest)
        {
            return shippingOptionRequest.Items.SelectMany(packageItem =>
            {
                //get dimensions and weight of the single item
                var (width, length, height) = GetDimensionsForSingleItem(packageItem.ShoppingCartItem);
                var weight = GetWeightForSingleItem(packageItem.ShoppingCartItem);

                var insuranceAmount = 0;
                if (_upsSettings.InsurePackage)
                {
                    //The maximum declared amount per package: 50000 USD.
                    //TODO: Currently using Product.Price - should we use GetUnitPrice() instead?
                    // Convert.ToInt32(_priceCalculationService.GetUnitPrice(sci, includeDiscounts:false))
                    //One could argue that the insured value should be based on Cost rather than Price.
                    //GetUnitPrice handles Attribute Adjustments and also Customer Entered Price.
                    //But, even with includeDiscounts:false, it could apply a "discount" from Tier pricing.
                    insuranceAmount = Convert.ToInt32(packageItem.ShoppingCartItem.Product.Price);
                }

                //create packages according to item quantity
                var package = CreatePackage(width, length, height, weight, insuranceAmount);
                return Enumerable.Repeat(package, packageItem.GetQuantity());
            });
        }

        /// <summary>
        /// Create packages (total dimensions of shopping cart items determines number of packages)
        /// </summary>
        /// <param name="shippingOptionRequest">shipping option request</param>
        /// <returns>Packages</returns>
        private IEnumerable<UPSRate.PackageType> GetPackagesByDimensions(GetShippingOptionRequest shippingOptionRequest)
        {
            //get dimensions and weight of the whole package
            var (width, length, height) = GetDimensions(shippingOptionRequest.Items);
            var weight = GetWeight(shippingOptionRequest);

            //whether the package doesn't exceed the weight and size limits
            if (weight <= WEIGHT_LIMIT && GetPackageSize(width, length, height) <= SIZE_LIMIT)
            {
                var insuranceAmount = 0;
                if (_upsSettings.InsurePackage)
                {
                    //The maximum declared amount per package: 50000 USD.
                    //use subTotalWithoutDiscount as insured value
                    var cart = shippingOptionRequest.Items.Select(item =>
                    {
                        var shoppingCartItem = item.ShoppingCartItem;
                        shoppingCartItem.Quantity = item.GetQuantity();
                        return shoppingCartItem;
                    }).ToList();
                    _orderTotalCalculationService.GetShoppingCartSubTotal(cart, false, out var _, out var _, out var subTotalWithoutDiscount, out var _);
                    insuranceAmount = Convert.ToInt32(subTotalWithoutDiscount);
                }

                return new[] { CreatePackage(width, length, height, weight, insuranceAmount) };
            }

            //get total packages number according to package limits
            var totalPackagesByWeightLimit = weight > WEIGHT_LIMIT
                ? Convert.ToInt32(Math.Ceiling(weight / WEIGHT_LIMIT))
                : 1;
            var totalPackagesBySizeLimit = GetPackageSize(width, length, height) > SIZE_LIMIT
                ? Convert.ToInt32(Math.Ceiling(GetPackageSize(width, length, height) / LENGTH_LIMIT))
                : 1;
            var totalPackages = Math.Max(Math.Max(totalPackagesBySizeLimit, totalPackagesByWeightLimit), 1);

            width = Math.Max(width / totalPackages, 1);
            length = Math.Max(length / totalPackages, 1);
            height = Math.Max(height / totalPackages, 1);
            weight = Math.Max(weight / totalPackages, 1);

            var insuranceAmountPerPackage = 0;
            if (_upsSettings.InsurePackage)
            {
                //The maximum declared amount per package: 50000 USD.
                //use subTotalWithoutDiscount as insured value
                var cart = shippingOptionRequest.Items.Select(item =>
                {
                    var shoppingCartItem = item.ShoppingCartItem;
                    shoppingCartItem.Quantity = item.GetQuantity();
                    return shoppingCartItem;
                }).ToList();
                _orderTotalCalculationService.GetShoppingCartSubTotal(cart, false, out var _, out var _, out var subTotalWithoutDiscount, out var _);
                insuranceAmountPerPackage = Convert.ToInt32(subTotalWithoutDiscount / totalPackages);
            }

            //create packages according to calculated value
            var package = CreatePackage(width, length, height, weight, insuranceAmountPerPackage);
            return Enumerable.Repeat(package, totalPackages);
        }

        /// <summary>
        /// Create packages (total volume of shopping cart items determines number of packages)
        /// </summary>
        /// <param name="shippingOptionRequest">shipping option request</param>
        /// <returns>Packages</returns>
        private IEnumerable<UPSRate.PackageType> GetPackagesByCubicRoot(GetShippingOptionRequest shippingOptionRequest)
        {
            //Dimensional weight is based on volume (the amount of space a package occupies in relation to its actual weight). 
            //If the cubic size of package measures three cubic feet (5,184 cubic inches or 84,951 cubic centimetres) or greater, you will be charged the greater of the dimensional weight or the actual weight.
            //This algorithm devides total package volume by the UPS settings PackingPackageVolume so that no package requires dimensional weight; this could result in an under-charge.

            var totalPackagesBySizeLimit = 1;
            var width = 0M;
            var length = 0M;
            var height = 0M;

            //if there is only one item, no need to calculate dimensions
            if (shippingOptionRequest.Items.Count == 1 && shippingOptionRequest.Items.FirstOrDefault().GetQuantity() == 1)
            {
                //get dimensions and weight of the single cubic size of package
                var item = shippingOptionRequest.Items.FirstOrDefault().ShoppingCartItem;
                (width, length, height) = GetDimensionsForSingleItem(item);
            }
            else
            {
                //or try to get them
                var dimension = 0;

                //get total volume of the package
                var totalVolume = shippingOptionRequest.Items.Sum(item =>
                {
                    //get dimensions and weight of the single item
                    var (itemWidth, itemLength, itemHeight) = GetDimensionsForSingleItem(item.ShoppingCartItem);
                    return item.GetQuantity() * itemWidth * itemLength * itemHeight;
                });
                if (totalVolume > decimal.Zero)
                {
                    //use default value (in cubic inches) if not specified
                    var packageVolume = _upsSettings.PackingPackageVolume;
                    if (packageVolume <= 0)
                        packageVolume = 5184;

                    //calculate cube root (floor)
                    dimension = Convert.ToInt32(Math.Floor(Math.Pow(Convert.ToDouble(packageVolume), 1.0 / 3.0)));
                    if (GetPackageSize(dimension, dimension, dimension) > SIZE_LIMIT)
                        throw new NopException("PackingPackageVolume exceeds max package size");

                    //adjust package volume for dimensions calculated
                    packageVolume = dimension * dimension * dimension;

                    totalPackagesBySizeLimit = Convert.ToInt32(Math.Ceiling(totalVolume / packageVolume));
                }

                width = length = height = dimension;
            }

            //get total packages number according to package limits
            var weight = GetWeight(shippingOptionRequest);
            var totalPackagesByWeightLimit = weight > WEIGHT_LIMIT
                ? Convert.ToInt32(Math.Ceiling(weight / WEIGHT_LIMIT))
                : 1;
            var totalPackages = Math.Max(Math.Max(totalPackagesBySizeLimit, totalPackagesByWeightLimit), 1);

            var insuranceAmountPerPackage = 0;
            if (_upsSettings.InsurePackage)
            {
                //The maximum declared amount per package: 50000 USD.
                //use subTotalWithoutDiscount as insured value
                var cart = shippingOptionRequest.Items.Select(item =>
                {
                    var shoppingCartItem = item.ShoppingCartItem;
                    shoppingCartItem.Quantity = item.GetQuantity();
                    return shoppingCartItem;
                }).ToList();
                _orderTotalCalculationService.GetShoppingCartSubTotal(cart, false, out var _, out var _, out var subTotalWithoutDiscount, out var _);
                insuranceAmountPerPackage = Convert.ToInt32(subTotalWithoutDiscount / totalPackages);
            }

            //create packages according to calculated value
            var package = CreatePackage(width, length, height, weight / totalPackages, insuranceAmountPerPackage);
            return Enumerable.Repeat(package, totalPackages);
        }

        /// <summary>
        /// Get dimensions values of the single shopping cart item
        /// </summary>
        /// <param name="item">Shopping cart item</param>
        /// <returns>Dimensions values</returns>
        private (decimal width, decimal length, decimal height) GetDimensionsForSingleItem(ShoppingCartItem item)
        {
            var items = new[] { new GetShippingOptionRequest.PackageItem(item, 1) };
            return GetDimensions(items);
        }

        /// <summary>
        /// Get dimensions values of the package
        /// </summary>
        /// <param name="items">Package items</param>
        /// <returns>Dimensions values</returns>
        private (decimal width, decimal length, decimal height) GetDimensions(IList<GetShippingOptionRequest.PackageItem> items)
        {
            var measureDimension = _measureService.GetMeasureDimensionBySystemKeyword(INCHES_DIMENSION_SYSTEM_KEYWORD)
                ?? throw new NopException($"UPS shipping service. Could not load \"{INCHES_DIMENSION_SYSTEM_KEYWORD}\" measure dimension");

            decimal convertAndRoundDimension(decimal dimension)
            {
                dimension = _measureService.ConvertFromPrimaryMeasureDimension(dimension, measureDimension);
                dimension = Convert.ToInt32(Math.Ceiling(dimension));
                return Math.Max(dimension, 1);
            }

            _shippingService.GetDimensions(items, out var width, out var length, out var height, true);
            width = convertAndRoundDimension(width);
            length = convertAndRoundDimension(length);
            height = convertAndRoundDimension(height);

            return (width, length, height);
        }

        /// <summary>
        /// Get weight value of the single shopping cart item
        /// </summary>
        /// <param name="item">Shopping cart item</param>
        /// <returns>Weight value</returns>
        private decimal GetWeightForSingleItem(ShoppingCartItem item)
        {
            var shippingOptionRequest = new GetShippingOptionRequest
            {
                Customer = item.Customer,
                Items = new[] { new GetShippingOptionRequest.PackageItem(item, 1) }
            };
            return GetWeight(shippingOptionRequest);
        }

        /// <summary>
        /// Get weight value of the package
        /// </summary>
        /// <param name="shippingOptionRequest">Shipping option request</param>
        /// <returns>Weight value</returns>
        private decimal GetWeight(GetShippingOptionRequest shippingOptionRequest)
        {
            var measureWeight = _measureService.GetMeasureWeightBySystemKeyword(LBS_WEIGHT_SYSTEM_KEYWORD)
                ?? throw new NopException($"UPS shipping service. Could not load \"{LBS_WEIGHT_SYSTEM_KEYWORD}\" measure weight");

            var weight = _shippingService.GetTotalWeight(shippingOptionRequest, ignoreFreeShippedItems: true);
            weight = _measureService.ConvertFromPrimaryMeasureWeight(weight, measureWeight);
            weight = Convert.ToInt32(Math.Ceiling(weight));
            return Math.Max(weight, 1);
        }

        /// <summary>
        /// Get package size
        /// </summary>
        /// <param name="length">Length</param>
        /// <param name="height">Height</param>
        /// <param name="width">Width</param>
        /// <returns>Package size</returns>
        private decimal GetPackageSize(decimal width, decimal length, decimal height)
        {
            //To measure ground packages use the following formula: Length + 2x Width +2x Height. Details: https://www.ups.com/us/en/help-center/packaging-and-supplies/prepare-overize.page
            return length + width * 2 + height * 2;
        }

        /// <summary>
        /// Gets shipping rates
        /// </summary>
        /// <param name="shippingOptionRequest">Shipping option request details</param>
        /// <param name="saturdayDelivery">Whether to get rates for Saturday Delivery</param>
        /// <returns>Shipping options; errors if exist</returns>
        private (IList<ShippingOption> shippingOptions, string error) GetShippingOptions(GetShippingOptionRequest shippingOptionRequest,
            bool saturdayDelivery = false)
        {
            try
            {
                //create request details
                var request = CreateRateRequest(shippingOptionRequest, saturdayDelivery);

                //get rate response
                var rateResponse = GetRatesAsync(request).Result;

                //prepare shipping options
                return (PrepareShippingOptions(rateResponse).Select(shippingOption =>
                {
                    //correct option name
                    if (!shippingOption.Name.ToLower().StartsWith("ups"))
                        shippingOption.Name = $"UPS {shippingOption.Name}";
                    if (saturdayDelivery)
                        shippingOption.Name = $"{shippingOption.Name} - Saturday Delivery";

                    //add additional handling charge
                    shippingOption.Rate += _upsSettings.AdditionalHandlingCharge;

                    return shippingOption;
                }).ToList(), null);
            }
            catch (Exception exception)
            {
                //log errors
                var message = $"Error while getting UPS rates{Environment.NewLine}{exception.Message}";
                _logger.Error(message, exception, shippingOptionRequest.Customer);

                return (new List<ShippingOption>(), message);
            }
        }

        /// <summary>
        /// Prepare shipping options
        /// </summary>
        /// <param name="rateResponse">Rate response</param>
        /// <returns>Shipping options</returns>
        private IEnumerable<ShippingOption> PrepareShippingOptions(UPSRate.RateResponse rateResponse)
        {
            var shippingOptions = new List<ShippingOption>();

            if (!rateResponse?.RatedShipment?.Any() ?? true)
                return shippingOptions;

            //prepare offered delivery services
            var servicesCodes = _upsSettings.CarrierServicesOffered.Split(':', StringSplitOptions.RemoveEmptyEntries)
                .Select(idValue => idValue.Trim('[', ']')).ToList();
            var allServices = DeliveryService.Standard.ToSelectList(false).Select(item =>
            {
                var serviceCode = GetUpsCode((DeliveryService)int.Parse(item.Value));
                return new { Name = $"UPS {item.Text?.TrimStart('_')}", Code = serviceCode, Offered = servicesCodes.Contains(serviceCode) };
            }).ToList();

            //get shipping options
            foreach (var rate in rateResponse.RatedShipment)
            {
                //weed out unwanted or unknown service rates
                var serviceCode = rate.Service?.Code;
                var deliveryService = allServices.FirstOrDefault(service => service.Code == serviceCode);
                if (!deliveryService?.Offered ?? true)
                    continue;

                //get rate value
                var regularValue = decimal.TryParse(rate.TotalCharges?.MonetaryValue, out var value) ? (decimal?)value : null;
                var negotiatedValue = decimal.TryParse(rate.NegotiatedRateCharges?.TotalCharge?.MonetaryValue, out value) ? (decimal?)value : null;
                var monetaryValue = negotiatedValue ?? regularValue;
                if (!monetaryValue.HasValue)
                    continue;

                //add shipping option based on service rate
                shippingOptions.Add(new ShippingOption
                {
                    Rate = monetaryValue.Value,
                    Name = deliveryService.Name
                });
            }

            return shippingOptions;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get UPS code of enum value
        /// </summary>
        /// <param name="enumValue">Enum value</param>
        /// <returns>UPS code</returns>
        public string GetUpsCode(Enum enumValue)
        {
            return GetAttributeValue<UPSCodeAttribute>(enumValue)?.Code;
        }

        /// <summary>
        /// Gets all events for a tracking number
        /// </summary>
        /// <param name="trackingNumber">The tracking number to track</param>
        /// <returns>Shipment events</returns>
        public virtual IEnumerable<ShipmentStatusEvent> GetShipmentEvents(string trackingNumber)
        {
            try
            {
                //create request details
                var request = CreateTrackRequest(trackingNumber);

                //get tracking info
                var response = TrackAsync(request).Result;
                return response.Shipment?
                    .SelectMany(shipment => shipment.Package?
                        .SelectMany(package => package.Activity?
                            .Select(activity => PrepareShipmentStatusEvent(activity))));
            }
            catch (Exception exception)
            {
                //log errors
                var message = $"Error while getting UPS shipment tracking info - {trackingNumber}{Environment.NewLine}{exception.Message}";
                _logger.Error(message, exception, _workContext.CurrentCustomer);

                return new List<ShipmentStatusEvent>();
            }
        }

        /// <summary>
        /// Gets shipping rates
        /// </summary>
        /// <param name="shippingOptionRequest">Shipping option request details</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public virtual GetShippingOptionResponse GetRates(GetShippingOptionRequest shippingOptionRequest)
        {
            var response = new GetShippingOptionResponse();

            //get regular rates
            var (shippingOptions, error) = GetShippingOptions(shippingOptionRequest);
            response.ShippingOptions = shippingOptions;
            if (!string.IsNullOrEmpty(error))
                response.Errors.Add(error);

            //get rates for saturday delivery
            if (_upsSettings.SaturdayDeliveryEnabled)
            {
                var (saturdayShippingOptions, saturdayError) = GetShippingOptions(shippingOptionRequest, true);
                foreach (var shippingOption in saturdayShippingOptions)
                {
                    response.ShippingOptions.Add(shippingOption);
                }
                if (!string.IsNullOrEmpty(error))
                    response.Errors.Add(error);
            }

            if (response.ShippingOptions.Any())
                response.Errors.Clear();

            return response;
        }

        #endregion
    }
}