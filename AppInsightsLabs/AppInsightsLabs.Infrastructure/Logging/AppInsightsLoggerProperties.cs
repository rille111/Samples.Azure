using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace AppInsightsLabs.Infrastructure.Logging
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class AppInsightsLoggerProperties : ILoggerProperties
    {
        public AppInsightsLoggerProperties([CallerMemberName]string memberName = "")
        {
            CallerMember = memberName;
        }

        /// <summary>
        /// Name of the calling method
        /// </summary>
        public string CallerMember { get; private set; }

        /// <summary>
        /// There's a field in AppInsights with this name. Default is: Assembly.GetEntryAssembly().GetName().Name
        /// </summary>
        public string application_Name {
            get
            {
                if (_application_Name == null)
                {
                    var ass = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                    _application_Name = ass.GetName().Name;
                }
                return _application_Name;
            }
            set { _application_Name = value; }
        }
        private string _application_Name;

        /// <summary>
        /// There's a field in AppInsights with this name. Default is: Assembly.GetEntryAssembly().GetName().Version.ToString();
        /// </summary>
        public string application_Version
        {
            get
            {
                if (_application_Version == null)
                {
                    var ass = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                    _application_Version = ass.GetName().Version.ToString();
                }
                return _application_Version;
            }
            set { _application_Version = value; }
        }
        private string _application_Version;

        /// <summary>
        /// This is a custom field. You can use it however, for example easier to group logs when using AppInsights analytics.
        /// </summary>
        public string application_LogContext { get; set; }
    }
}