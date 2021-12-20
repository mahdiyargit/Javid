using Grasshopper;
using Grasshopper.Kernel;
using Javid.Properties;

namespace Javid
{
    public class Javid : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("Javid", Resources.javid);
            Instances.ComponentServer.AddCategorySymbolName("Javid", 'J');
            return GH_LoadingInstruction.Proceed;
        }
    }
}