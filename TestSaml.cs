using System;
using System.Reflection;
using ITfoxtec.Identity.Saml2.Schemas.Metadata;

class Program {
    static void Main() {
        foreach(var m in typeof(EntityDescriptor).GetMethods()) {
            if (m.Name.Contains("ReadIdPSsoDescriptorFromUrlAsync")) {
                Console.Write(m.Name + "(");
                foreach(var p in m.GetParameters()) Console.Write(p.ParameterType.Name + " " + p.Name + ", ");
                Console.WriteLine(")");
            }
        }
    }
}
