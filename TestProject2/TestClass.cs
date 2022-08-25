using log4net;
//
namespace TestProject1
{
    public class TestClass
    {
        public ILog logger { get; set; }

        public void Info(string message) => logger.Info(message);
        public void Warn(string message) => logger.Warn(message);
        public void Error(string message) => logger.Error(message);
        public void Fatal(string message) => logger.Fatal(message);
        public void Debug(string message) => logger.Debug(message);
    }
}