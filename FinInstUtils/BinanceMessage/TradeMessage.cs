using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace FinInstUtils.BinanceMessage
{
    public class TradeMessage
    {
        [JsonProperty("stream")]
        public string Stream { get; set; }

        [JsonProperty("data")]
        public TradeData Data { get; set; }


        public static Boolean TryDeserialize(string message, [MaybeNullWhen(false)] out TradeMessage tradeMessage)
        {
            try
            {
                tradeMessage = JsonConvert.DeserializeObject<TradeMessage>(message);

                if (tradeMessage.Data == null || tradeMessage.Data.Symbol == null)
                    return false;

                return true;
            }
            catch
            {
                tradeMessage = null;
                return false;
            }
        }
    }
}
