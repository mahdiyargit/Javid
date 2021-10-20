using Grasshopper.Kernel;
using System;
using System.Drawing;
namespace Javid
{
    public class JavidInfo : GH_AssemblyInfo
    {
        public override string Name => "Javid";
        public override string AssemblyVersion => "0.1.0";
        public override Bitmap Icon => null;
        public override string Description =>
            "JAVID is an open-source graphic design and image-processing Grasshopper plug-in, " +
            "developed in the ‘Coding for architects’ workshop at IAC. " +
            "It can represent Images using different graphical techniques, such as ‘ASCII Art’, ‘String Art’ and many others.";
        public override Guid Id => new Guid("EE048085-1F5E-451E-BEF4-5AEE42B03436");
        public override string AuthorName => "Mahdiyar";
        public override string AuthorContact => "info@mahdiyar.io";
    }
}