using System;
using System.IO;
using System.CommandLine;
using System.Collections.Generic;

namespace Vk.Generator
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Option<DirectoryInfo> outputPathOpt = new Option<DirectoryInfo>(
                ["-o", "--out"],
                () => new DirectoryInfo(AppContext.BaseDirectory),
                "The folder into which code is generated. Defaults to the application directory.");

            RootCommand cmd = new RootCommand();
            cmd.AddOption(outputPathOpt);

            cmd.SetHandler(outputPath =>
            {
                Configuration.CodeOutputPath = outputPath.FullName;

                if (!outputPath.Exists)
                {
                    outputPath.Create();
                }

                using (var fs = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "vk.xml")))
                {
                    VulkanSpecification vs = VulkanSpecification.LoadFromXmlStream(fs);
                    TypeNameMappings tnm = new TypeNameMappings();
                    foreach (var typedef in vs.Typedefs)
                    {
                        if (typedef.Requires != null)
                        {
                            tnm.AddMapping(typedef.Requires, typedef.Name);
                        }
                        else
                        {
                            tnm.AddMapping(typedef.Name, "uint");
                        }
                    }

                    HashSet<string> definedBaseTypes = new HashSet<string>
                    {
                        "VkBool32"
                    };

                    if (Configuration.MapBaseTypes)
                    {
                        foreach (var baseType in vs.BaseTypes)
                        {
                            if (!definedBaseTypes.Contains(baseType.Key))
                            {
                                tnm.AddMapping(baseType.Key, baseType.Value);
                            }
                        }
                    }

                    CodeGenerator.GenerateCodeFiles(vs, tnm, Configuration.CodeOutputPath);
                }
            }, outputPathOpt);

            return cmd.Invoke(args);
        }
    }
}
