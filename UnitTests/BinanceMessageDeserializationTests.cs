using FinInstUtils.BinanceMessage;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinInstUnitTests
{
    internal class BinanceMessageDeserializationTests
    {
        [TestCase("""{"stream":"btcusdt@aggTrade","data":{"e":"aggTrade","E":1738529619963,"s":"BTCUSDT","a":3412505070,"p":"97859.98000000","q":"0.00042000","f":4504374967,"l":4504374973,"T":1738529619963,"m":false,"M":true}}""")]
        public void Deserialize_ValidJson_ReturnsTradeMessage(string message)
        {
            // Act
            if (TradeMessage.TryDeserialize(message, out TradeMessage tradeMessage))
            {
                Assert.NotNull(tradeMessage);
                Assert.That(tradeMessage.Stream, Is.EqualTo("btcusdt@aggTrade"));
                Assert.NotNull(tradeMessage.Data);
                Assert.That(tradeMessage.Data.Symbol, Is.EqualTo("BTCUSDT"));
                Assert.That(tradeMessage.Data.Price, Is.EqualTo("97859.98000000"));
            }
            else 
                Assert.Fail(); 
        }

        [TestCase("""{"result":null,"id":1}""")]
        [TestCase("not a json at all")]
        public void Deserialize_InvalidJson_ThrowsException(string message)
        {
            if (TradeMessage.TryDeserialize(message, out TradeMessage tradeMessage))
            {
                Assert.Fail();
            }
            else
                Assert.Pass();
        }
    }
}
