using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.eShopWeb.ApplicationCore;
public class TriggerSettings
{
    public const string SectionName  = "TriggerSettings";
    public string FunctionUrl { get; set; }

    public string FunctionKey { get; set; }

    public string ServiceBusConnection { get; set; }

    public string ServiceBusQueue { get; set; }
}
