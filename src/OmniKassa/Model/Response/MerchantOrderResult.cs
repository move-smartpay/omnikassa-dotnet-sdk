﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OmniKassa.Model.Enums;
using OmniKassa.Utils;

namespace OmniKassa.Model.Response
{
    /// <summary>
    /// Result and state of a single order 
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class MerchantOrderResult
    {
        /// <summary>
        /// Unique ID of the webshop. Only relevant when multiple webshops make use of the same webhook.
        /// </summary>
        [JsonProperty(PropertyName = "poiId")]
        public int PointOfInteractionId { get; private set; }

        /// <summary>
        /// Order ID given during the announce
        /// </summary>
        [JsonProperty(PropertyName = "merchantOrderId")]
        public String MerchantOrderId { get; private set; }

        /// <summary>
        /// Unique ID assigned by the OmniKassa API
        /// </summary>
        [JsonProperty(PropertyName = "omnikassaOrderId")]
        public String OmnikassaOrderId { get; private set; }

        /// <summary>
        /// List of transactions
        /// </summary>
        [JsonProperty(PropertyName = "transactions")]
        public IReadOnlyList<TransactionInfo> TransactionInfo { get; private set; }

        [JsonProperty(PropertyName = "orderStatus")]
        #pragma warning disable 0649
        private String orderStatus;
        #pragma warning restore 0649

        /// <summary>
        /// State of the order
        /// </summary>
        public PaymentStatus? OrderStatus
        {
            get
            {
                return PaymentStatusParser.GetStatus(orderStatus);
            }
        }

        /// <summary>
        /// Error code
        /// </summary>
        [JsonProperty(PropertyName = "errorCode")]
        public String ErrorCode { get; private set; }

        [JsonProperty(PropertyName = "orderStatusDateTime")]
        #pragma warning disable 0649
        private String orderStatusDateTime;
        #pragma warning restore 0649

        /// <summary>
        /// Currency which was used to pay
        /// </summary>
        [JsonProperty(PropertyName = "paidAmount")]
        public Money PaidAmount { get; private set; }

        /// <summary>
        /// Amount paid by the consumer
        /// </summary>
        [JsonProperty(PropertyName = "totalAmount")]
        public Money TotalAmount { get; private set; }

        /// <summary>
        /// DateTime the order was updated to given state
        /// </summary>
        public DateTime OrderStatusDateTime
        {
            get 
            {
                return DateTimeUtils.StringToDate(orderStatusDateTime);
            } 
        }

        /// <summary>
        /// Gets the signature data
        /// </summary>
        /// <returns>Signature data</returns>
        public List<String> GetSignatureData()
        {
            List<String> data = new List<String>
            {
                MerchantOrderId,
                OmnikassaOrderId,
                Convert.ToString(PointOfInteractionId),
                orderStatus,
                orderStatusDateTime,
                ErrorCode,
                PaidAmount.Currency.ToString(),
                Convert.ToString(PaidAmount.GetAmountInCents()),
                TotalAmount.Currency.ToString(),
                Convert.ToString(TotalAmount.GetAmountInCents())
            };
            if (TransactionInfo != null)
            {
                foreach (TransactionInfo info in TransactionInfo)
                {
                    data.AddRange(info.GetSignatureData());
                }
            }
            return data;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
        public override Boolean Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj == null)
            {
                return false;
            }
            if (!(obj is MerchantOrderResult))
            {
                return false;
            }
            MerchantOrderResult order = (MerchantOrderResult)obj;
            return Equals(PointOfInteractionId, order.PointOfInteractionId) &&
                Equals(MerchantOrderId, order.MerchantOrderId) &&
                Equals(OmnikassaOrderId, order.OmnikassaOrderId) &&
                Equals(OrderStatus, order.OrderStatus) &&
                Equals(ErrorCode, order.ErrorCode) &&
                Equals(OrderStatusDateTime, order.OrderStatusDateTime) &&
                Equals(PaidAmount, order.PaidAmount) &&
                Equals(TotalAmount, order.TotalAmount) &&
                EqualsTransactionInfo(TransactionInfo, order.TransactionInfo);
        }

        private bool EqualsTransactionInfo(
            IReadOnlyList<TransactionInfo> one,
            IReadOnlyList<TransactionInfo> two
        ) {
            // Handle empty lists as null
            var tmpOne = one != null && one.Count > 0 ? one : null;
            var tmpTwo = two != null && two.Count > 0 ? two : null;

            // When both are null, items are the same.
            if (tmpOne == null && tmpTwo == null)
            {
                return true;
            }
            // If only one is null, items are not the same.
            if (tmpOne == null || tmpTwo == null)
            {
                return false;
            }
            return Enumerable.SequenceEqual(tmpOne, tmpTwo);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 0x51ed270b;
                hash = (hash * -1521134295) + PointOfInteractionId.GetHashCode();
                hash = (hash * -1521134295) + (MerchantOrderId == null ? 0 : MerchantOrderId.GetHashCode());
                hash = (hash * -1521134295) + (OmnikassaOrderId == null ? 0 : OmnikassaOrderId.GetHashCode());
                hash = (hash * -1521134295) + (OrderStatus == null ? 0 : OrderStatus.GetHashCode());
                hash = (hash * -1521134295) + (ErrorCode == null ? 0 : ErrorCode.GetHashCode());
                hash = (hash * -1521134295) + (OrderStatusDateTime == null ? 0 : OrderStatusDateTime.GetHashCode());
                hash = (hash * -1521134295) + (PaidAmount == null ? 0 : PaidAmount.GetHashCode());
                hash = (hash * -1521134295) + (TotalAmount == null ? 0 : TotalAmount.GetHashCode());
                if (TransactionInfo != null)
                {
                    foreach (TransactionInfo info in TransactionInfo)
                    {
                        hash = (hash * -1521134295) + info.GetHashCode();
                    }
                }
                return hash;
            }
        }
    }
}
