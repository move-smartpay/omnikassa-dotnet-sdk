using Newtonsoft.Json;
using System;
using OmniKassa.Model.Enums;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("OmniKassa.Tests")]

namespace OmniKassa.Model.Response
{
    /// <summary>
    /// Represents a stored card (card on file) for a shopper
    /// </summary>
    public class CardOnFile
    {
        /// <summary>
        /// Uniquely identifies the card on file
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; internal set; }

        /// <summary>
        /// The last 4 digits of the card number
        /// </summary>
        [JsonProperty("last4Digits")]
        public string Last4Digits { get; internal set; }

        /// <summary>
        /// The brand of the card
        /// </summary>
        [JsonProperty("brand")]
        public string Brand { get; internal set; }

        /// <summary>
        /// The expiry of the card in YYYY-MM format
        /// </summary>
        [JsonProperty("cardExpiry")]
        public string CardExpiry { get; internal set; }

        /// <summary>
        /// The expiry of the token in YYYY-MM format
        /// </summary>
        [JsonProperty("tokenExpiry")]
        public string TokenExpiry { get; internal set; }

        /// <summary>
        /// The status of the token
        /// </summary>
        [JsonProperty("status")]
        [JsonConverter(typeof(EnumJsonConverter<CardStatus>))]
        public CardStatus? Status { get; internal set; }
    }
}
