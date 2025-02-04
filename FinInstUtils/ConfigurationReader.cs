using FinInstUtils.ConfigElements;
using System.Xml.Linq;
using Serilog;

namespace FinInstUtils
{
    /// <summary>
    /// Reads configuration file (list of RabbitMQ instances and pairs)
    /// </summary>
    public class ConfigurationReader
    {
        private readonly string _filePath;
        private readonly ILogger _logger;

        private Configuration _config;

        public static String FILENAME = "FinInstConf.xml";

        public ConfigurationReader(string filePath, ILogger logger)
        {
            _filePath = filePath;
            _logger = logger;

            ReadConfiguration();
        }

        private Configuration ReadConfiguration()
        {
            try
            {
                _config = new Configuration
                {
                    RabbitMQInstances = new List<RabbitMQInstance>(),
                    Instruments = new List<Instrument>()
                };

                var doc = XDocument.Load(_filePath);

                foreach (var instanceElement in doc.Descendants("Instance"))
                {
                    var instance = new RabbitMQInstance
                    {
                        HostName = instanceElement.Attribute("hostname").Value,
                        Port = int.Parse(instanceElement.Attribute("port").Value),
                        UserName = instanceElement.Attribute("username").Value,
                        Password = instanceElement.Attribute("password").Value
                    };
                    _config.RabbitMQInstances.Add(instance);
                }

                foreach (var instrumentElement in doc.Descendants("Instrument"))
                {
                    var instrument = new Instrument
                    {
                        Name = instrumentElement.Attribute("name").Value
                    };
                    _config.Instruments.Add(instrument);
                }

                return _config;
            }
            catch
            {
                _logger.Error("Error loading configuration from conf file");
                throw new ApplicationException("Error loading configuration from conf file");
            }
        }

        public Configuration GetConfig() => _config;
    }
}
