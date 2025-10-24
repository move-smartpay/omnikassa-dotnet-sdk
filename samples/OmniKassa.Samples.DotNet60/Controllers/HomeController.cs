﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Specialized;
using OmniKassa.Model.Response;
using OmniKassa.Exceptions;
using OmniKassa.Model.Enums;
using OmniKassa.Model;
using OmniKassa.Model.Order;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using OmniKassa.Model.Response.Notification;
using OmniKassa.Samples.DotNet50.Configuration;
using Endpoint = OmniKassa.Endpoint;
using example_dotnet60.Models;
using example_dotnet60.Helpers;
using OmniKassa.Model.Request;
using System.Linq;

namespace example_dotnet60.Controllers
{
    public class HomeController : Controller
    {
        private readonly string SIGNING_KEY;
        private readonly string TOKEN;
        private readonly string RETURN_URL;
        private readonly string BASE_URL;
        private readonly string USER_AGENT;
        private readonly string PARTNER_REFERENCE;
        private readonly string FAST_CHECKOUT_RETURN_URL;
        private readonly string SHOPPER_REFERENCE;

        private static string SESSION_WEBSHOP_MODEL = "WEBSHOP_MODEL";

        private static Endpoint omniKassa;

        private int orderId = 0;
        private WebShopModel webShopModel;
        private int orderItemId = 0;

        private static string omniKassaOrderId = null;
        private static ApiNotification notification;

        public HomeController(ConfigurationParameters configurationParameters)
        {
            SIGNING_KEY = configurationParameters.SigningKey;
            TOKEN = configurationParameters.RefreshToken;
            RETURN_URL = configurationParameters.CallbackUrl;
            BASE_URL = configurationParameters.BaseUrl;

            var userAgent = configurationParameters.UserAgent;
            if (!string.IsNullOrEmpty(userAgent))
            {
                USER_AGENT = userAgent;
            }

            var partnerReference = configurationParameters.PartnerReference;
            if (!string.IsNullOrEmpty(partnerReference))
            {
                PARTNER_REFERENCE = partnerReference;
            }

            var fastCheckoutReturnUrl = configurationParameters.FastCheckoutReturnUrl;
            if (!string.IsNullOrEmpty(fastCheckoutReturnUrl))
            {
                FAST_CHECKOUT_RETURN_URL = fastCheckoutReturnUrl;
            }

            var shopperReference = configurationParameters.ShopperReference;
            if (!string.IsNullOrEmpty(shopperReference))
            {
                SHOPPER_REFERENCE = shopperReference;
            }

            if (omniKassa == null)
            {
                InitializeOmniKassaEndpoint();
            }
        }

        private void InitializeOmniKassaEndpoint()
        {
            // The base URL can be one of the following:
            // "https://betalen.rabobank.nl/omnikassa-api/";
            // "https://api.pay.rabobank.nl/omnikassa-api/";
            // "https://betalen.rabobank.nl/omnikassa-api-sandbox/";
            // "https://api.pay-acpt.rabobank.nl/omnikassa-api/";
            omniKassa = Endpoint.Create(BASE_URL, SIGNING_KEY, TOKEN, USER_AGENT, PARTNER_REFERENCE);
        }

        [HttpGet]
        public IActionResult Index()
        {
            SetVersionViewData();
            InitWebshopModel();

            PopulateViewData(webShopModel);
            return View(webShopModel);
        }

        private void SetVersionViewData()
        {
            var mvcName = typeof(Controller).Assembly.GetName();
            var isMono = Type.GetType("Mono.Runtime") != null;

            ViewBag.RuntimeVersion = mvcName.Version.Major + "." + mvcName.Version.Minor;
            ViewBag.Runtime = isMono ? "Mono" : ".NET";
        }

        private void InitWebshopModel()
        {
            webShopModel = SessionVar.Get<WebShopModel>(HttpContext.Session, SESSION_WEBSHOP_MODEL);
            if (webShopModel != null)
            {
                webShopModel.ReCreateBuilder();
                orderId = webShopModel.OrderId;
                orderItemId = webShopModel.GetLastItemId();
            }
            else
            {
                webShopModel = new WebShopModel(GetOrder(++orderId));
                SessionVar.Set<WebShopModel>(HttpContext.Session, SESSION_WEBSHOP_MODEL, webShopModel);
            }
        }

        private void StoreWebshopModel()
        {
            SessionVar.Set<WebShopModel>(HttpContext.Session, SESSION_WEBSHOP_MODEL, webShopModel);
        }

        private void AssignNewOrder()
        {
            webShopModel.AssignOrder(GetOrder(++orderId));
        }

        private void StoreOrderFromRequest(IFormCollection form)
        {
            webShopModel.Order = OrderHelper.CreateOrder(GetCollection(form), webShopModel);
        }

        private void PopulateViewData(WebShopModel model)
        {
            ViewBag.CustomerInformation = model.Order.CustomerInformation;
            ViewBag.GenderItems = WebShopViewData.GetGenderItems(model.Order);
            ViewBag.PaymentBrandItems = WebShopViewData.GetPaymentBrandItems(model.Order);
            ViewBag.PaymentBrandForceItems = WebShopViewData.GetPaymentBrandForceItems(model.Order);
            ViewBag.IdealIssuerItems = WebShopViewData.GetIdealIssuerItems(model);
            ViewBag.InitiatingParty = model.Order.InitiatingParty;
            ViewBag.ShippingDetails = model.Order.ShippingDetails;
            ViewBag.ShippingAddressCountryItems = WebShopViewData.GetShippingAddressCountryItems(model.Order);
            ViewBag.BillingDetails = model.Order.BillingDetails;
            ViewBag.BillingAddressCountryItems = WebShopViewData.GetBillingAddressCountryItems(model.Order);
            ViewBag.EnableCardOnFile = model.Order.PaymentBrandMetaDataObject?.EnableCardOnFile ?? false;
            ViewBag.ShopperReference = model.Order.ShopperReference;
            ViewBag.RequiredCheckoutFieldsCustomerInformation = model.Order.PaymentBrandMetaDataObject?.FastCheckout?.HasRequiredCheckoutFields(RequiredCheckoutFields.CUSTOMER_INFORMATION) ?? false;
            ViewBag.RequiredCheckoutFieldsBillingAddress = model.Order.PaymentBrandMetaDataObject?.FastCheckout?.HasRequiredCheckoutFields(RequiredCheckoutFields.BILLING_ADDRESS) ?? false;
            ViewBag.RequiredCheckoutFieldsShippingAddress = model.Order.PaymentBrandMetaDataObject?.FastCheckout?.HasRequiredCheckoutFields(RequiredCheckoutFields.SHIPPING_ADDRESS) ?? false;
            ViewBag.ShippingCostCurrencyItems = WebShopViewData.GetShippingCostCurrencyItems(model.Order);
            ViewBag.ShippingCostAmount = model.Order.ShippingCost?.Amount;
            ViewBag.OmniKassaOrderId = omniKassaOrderId;
        }

        [HttpPost]
        public IActionResult AddItem()
        {
            SetVersionViewData();
            InitWebshopModel();

            try
            {
                OrderItem item = OrderHelper.CreateOrderItem(GetCollection(Request.Form), ++orderItemId);
                webShopModel.AddItem(item);
            }
            catch (ArgumentException ex)
            {
                webShopModel.Error = ex.Message;
            }

            StoreWebshopModel();
            PopulateViewData(webShopModel);
            return View("Index", webShopModel);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder()
        {
            InitWebshopModel();
            webShopModel.PaymentCompleted = null;

            var collection = GetCollection(Request.Form);
            
            var isFastCheckout = collection.AllKeys.Contains("submitFastCheckout");
            if (isFastCheckout)
            {
                collection["paymentBrandForce"] = PaymentBrandForce.FORCE_ALWAYS.ToString();
                collection["paymentBrand"] = PaymentBrand.IDEAL.ToString();
            }

            try
            {
                webShopModel.Order = OrderHelper.PrepareOrder(collection, webShopModel);
                if (webShopModel.Order != null)
                {
                    MerchantOrderResponse response = await omniKassa.Announce(webShopModel.Order);
                    omniKassaOrderId = response.OmnikassaOrderId;
                    ViewBag.OmniKassaOrderId = omniKassaOrderId;

                    AssignNewOrder();
                    StoreWebshopModel();

                    return new RedirectResult(response.RedirectUrl);
                }
            }
            catch (RabobankSdkException ex)
            {
                webShopModel.Error = ex.Message;
            }
            catch (ArgumentException ex)
            {
                webShopModel.Error = ex.Message;
            }

            if (webShopModel.Order == null)
            {
                AssignNewOrder();
            }
            StoreWebshopModel();
            PopulateViewData(webShopModel);
            return View("Index", webShopModel);
        }

        [HttpGet]
        public IActionResult Callback()
        {
            SetVersionViewData();
            InitWebshopModel();

            Dictionary<String, StringValues> query = QueryHelpers.ParseQuery(Request.QueryString.Value);
            Dictionary<String, String> dictionary = GetDictionary(query);
            try
            {
                PaymentCompletedResponse response = PaymentCompletedResponse.Create(dictionary, SIGNING_KEY);
                webShopModel.PaymentCompleted = response;
                String validatedOrderId = response.OrderId;
                PaymentStatus? validatedStatus = response.Status;

                ViewData["OrderId"] = response.OrderId;
                ViewData["Status"] = response.Status;
            }
            catch (RabobankSdkException ex)
            {
                webShopModel.Error = ex.Message;
            }

            PopulateViewData(webShopModel);
            return View("Index", webShopModel);
        }

        [HttpPost]
        public IActionResult Webhook([FromBody] ApiNotification notification)
        {
            HomeController.notification = notification;
            return new OkObjectResult("");
        }

        [HttpPost]
        public async Task<IActionResult> RetrieveUpdates()
        {
            InitWebshopModel();

            if (notification != null)
            {
                try
                {
                    MerchantOrderStatusResponse response = null;
                    do
                    {
                        response = await omniKassa.RetrieveAnnouncement(notification);
                        webShopModel.Responses.Add(response);
                    } while (response.MoreOrderResultsAvailable);
                }
                catch (RabobankSdkException ex)
                {
                    webShopModel.Error = ex.Message;
                }
            }
            else
            {
                webShopModel.Error = "Order status notification not yet received.";
            }

            PopulateViewData(webShopModel);
            return View("Index", webShopModel);
        }

        [HttpPost]
        public async Task<IActionResult> RetrieveOrder()
        {
            InitWebshopModel();

            NameValueCollection collection = GetCollection(Request.Form);
            var OmniKassaOrderId = collection.Get("omniKassaOrderId");

            if (String.IsNullOrEmpty(OmniKassaOrderId))
            {
                webShopModel.Error = "No OmniKassa Order ID known";
                PopulateViewData(webShopModel);
                return View("Index", webShopModel);
            }

            try
            {
                OrderStatusResponse response = await omniKassa.RetrieveOrder(OmniKassaOrderId);

                if (response == null)
                {
                    webShopModel.Error = "No data for OmniKassa Order with ID " + OmniKassaOrderId;
                    PopulateViewData(webShopModel);
                    return View("Index", webShopModel);
                }

                webShopModel.OrderStatusResult = response.Order;
            }
            catch (RabobankSdkException ex)
            {
                webShopModel.Error = ex.Message;
            }

            PopulateViewData(webShopModel);
            return View("Index", webShopModel);
        }

        [HttpGet]
        public async Task<IActionResult> InitiateRefund()
        {
            InitWebshopModel();

            Dictionary<String, StringValues> query = QueryHelpers.ParseQuery(Request.QueryString.Value);
            var transactionIdStr = query.GetValueOrDefault("transactionId");
            if (!string.IsNullOrEmpty(transactionIdStr))
            {
                var transactionId = Guid.Parse(transactionIdStr);
                webShopModel.TransactionRefundableDetailsResponse = await omniKassa.FetchRefundableTransactionDetails(transactionId);
            }

            return View(webShopModel);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitRefund()
        {
            InitWebshopModel();
            try
            {
                NameValueCollection collection = GetCollection(Request.Form);
                var amountStr = collection.Get("amount");
                var description = collection.Get("description");
                var vatCategoryStr = collection.Get("vat");
                var transactionId = Guid.Parse(collection.Get("transactionId"));

                decimal amountDecimal = Convert.ToDecimal(amountStr);
                Money amount = Money.FromDecimal(Currency.EUR, Decimal.Round(amountDecimal, 2));
                VatCategory vatCategory = (VatCategory)Enum.Parse(typeof(VatCategory), vatCategoryStr);

                var refundRequest = new InitiateRefundRequest(amount, description, vatCategory);

                webShopModel.RefundDetailsResponse = await omniKassa.InitiateRefundTransaction(refundRequest, transactionId, Guid.NewGuid());
            }
            catch (RabobankSdkException ex)
            {
                webShopModel.Error = ex.Message;
            }
            return View("RefundDetails", webShopModel);
        }

        [HttpPost]
        public async Task<ActionResult> FetchRefundDetails()
        {
            InitWebshopModel();
            try
            {
                NameValueCollection collection = GetCollection(Request.Form);
                var transactionIdStr = collection.Get("transactionId");
                var refundIdStr = collection.Get("refundId");

                if (!string.IsNullOrEmpty(transactionIdStr) && !string.IsNullOrEmpty(refundIdStr))
                {
                    var transactionId = Guid.Parse(transactionIdStr);
                    var refundId = Guid.Parse(refundIdStr);

                    webShopModel.RefundDetailsResponse = await omniKassa.FetchRefundTransaction(transactionId, refundId);
                }
            }
            catch (RabobankSdkException ex)
            {
                webShopModel.Error = ex.Message;
            }
            return View("RefundDetails", webShopModel);
        }

        [HttpGet]
        public ActionResult RefundDetails()
        {
            return View(webShopModel);
        }

        public MerchantOrder.Builder GetOrder(int orderId)
        {
            CustomerInformation customerInformation = new CustomerInformation.Builder()
                .WithTelephoneNumber("0204971111")
                .WithInitials("J.D.")
                .WithGender(Gender.M)
                .WithEmailAddress("johndoe@rabobank.com")
                .WithDateOfBirth("20-03-1987")
                .WithFullName("Jan de Ruiter")
                .Build();

            Address shippingDetails = new Address.Builder()
                .WithFirstName("John")
                .WithLastName("Doe")
                .WithStreet("Street")
                .WithHouseNumber("5")
                .WithHouseNumberAddition("a")
                .WithPostalCode("1234AB")
                .WithCity("Haarlem")
                .WithCountryCode(CountryCode.NL)
                .Build();

            Address billingDetails = new Address.Builder()
                .WithFirstName("John")
                .WithLastName("Doe")
                .WithStreet("Factuurstraat")
                .WithHouseNumber("5")
                .WithHouseNumberAddition("a")
                .WithPostalCode("1234AB")
                .WithCity("Haarlem")
                .WithCountryCode(CountryCode.NL)
                .Build();

            PaymentBrandMetaData paymentBrandMetaData = new PaymentBrandMetaData.Builder()
                .WithEnableCardOnFile(true)
                .Build();

            MerchantOrder.Builder order = new MerchantOrder.Builder()
                .WithMerchantOrderId(Convert.ToString(orderId))
                .WithDescription("An example description")
                .WithCustomerInformation(customerInformation)
                .WithShippingDetail(shippingDetails)
                .WithBillingDetail(billingDetails)
                .WithLanguage(Language.NL)
                .WithMerchantReturnURL(RETURN_URL)
                .WithInitiatingParty("LIGHTSPEED")
                .WithShopperReference(SHOPPER_REFERENCE)
                .WithPaymentBrandMetaData(paymentBrandMetaData)
                .WithShippingCost(Currency.EUR, 0.01m);

            return order;
        }

        [HttpPost]
        public async Task<IActionResult> RetrievePaymentBrands()
        {
            InitWebshopModel();
            try
            {
                var paymentBrands = await omniKassa.RetrievePaymentBrands();
                webShopModel.PaymentBrandsResponse = paymentBrands;
            }
            catch (RabobankSdkException ex)
            {
                webShopModel.Error = ex.Message;
            }

            PopulateViewData(webShopModel);
            return View("Index", webShopModel);
        }

        [HttpPost]
        public async Task<IActionResult> RetrieveIdealIssuers()
        {
            InitWebshopModel();
            try
            {
                var idealIssuers = await omniKassa.RetrieveIdealIssuers();
                webShopModel.IdealIssuersResponse = idealIssuers;
            }
            catch (RabobankSdkException ex)
            {
                webShopModel.Error = ex.Message;
            }

            PopulateViewData(webShopModel);
            return View("Index", webShopModel);
        }

        private Dictionary<String, String> GetDictionary(Dictionary<String, StringValues> collection)
        {
            Dictionary<String, String> dictionary = new Dictionary<String, String>();
            foreach(KeyValuePair<String, StringValues> pair in collection)
            {
                dictionary.Add(pair.Key, pair.Value);
            }
            return dictionary;
        }

        private NameValueCollection GetCollection(IFormCollection formCollection)
        {
            NameValueCollection collection = new NameValueCollection();

            foreach (var key in formCollection.Keys)
            {
                string value = formCollection[key];
                collection.Add(key, value);
            }
            return collection;
        }

        [HttpPost]
        public async Task<IActionResult> RetrieveShopperPaymentDetails()
        {
            InitWebshopModel();
            try
            {
                NameValueCollection collection = GetCollection(Request.Form);
                string shopperReference = collection.Get("shopperReference");
                ShopperPaymentDetailsResponse response = await omniKassa.RetrieveShopperPaymentDetails(shopperReference);
                webShopModel.CardsOnFile = response.CardOnFileList;
            }
            catch (RabobankSdkException ex)
            {
                webShopModel.Error = ex.Message;
            }

            PopulateViewData(webShopModel);
            return View("Index", webShopModel);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteShopperPaymentDetail()
        {
            InitWebshopModel();
            try
            {
                NameValueCollection collection = GetCollection(Request.Form);
                string shopperReference = collection.Get("shopperReference");
                string id = collection.Get("id");
                await omniKassa.DeleteShopperPaymentDetail(id, shopperReference);
            }
            catch (RabobankSdkException ex) 
            {
                webShopModel.Error = ex.Message;
            }

            InitWebshopModel();
            try
            {
                NameValueCollection collection = GetCollection(Request.Form);
                string shopperReference = collection.Get("shopperReference");
                ShopperPaymentDetailsResponse response = await omniKassa.RetrieveShopperPaymentDetails(shopperReference);
                webShopModel.CardsOnFile = response.CardOnFileList;
            }
            catch (RabobankSdkException ex)
            {
                webShopModel.Error = ex.Message;
            }

            PopulateViewData(webShopModel);
            return View("Index", webShopModel);
        }
    }
}
