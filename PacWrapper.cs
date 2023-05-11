using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace D365.Community.Pac.Wrapper
{
    public class PacWrapper
    {
        private static readonly DataContractJsonSerializerSettings Settings = new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true,
            DateTimeFormat = new DateTimeFormat("yyyy-MM-ddTHH:mm:ssZ")
        };

        public static string Execute(string pac, params string[] args)
        {
            var pacDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PAC_DEBUG") ?? "false");
            var result = new List<string>();
            var failed = false;
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pac,
                        Arguments = "--non-interactive",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();

                using (var sw = process.StandardInput)
                {
                    sw.WriteLine("{\"Arguments\":[" + string.Join(",", args.Select(a => $"\"{a}\"")) + "]}");
                    sw.WriteLine("{\"Arguments\":[\"exit\"]}");
                    sw.Close();
                }

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (pacDebug) Console.WriteLine(line);
                    result.Add(line);
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(line)))
                    {
                        var pacResult = (PacResult)new DataContractJsonSerializer(typeof(PacResult), Settings).ReadObject(ms);
                        if (pacResult.Status?.ToLowerInvariant() == "success") continue;
                        failed = true;
                        foreach (var error in pacResult.Errors)
                        {
                            Console.Error.WriteLine(error);
                        }
                        foreach (var warning in pacResult.Warnings)
                        {
                            Console.Error.WriteLine(warning);
                        }
                        if (pacDebug)
                        {
                            foreach (var information in pacResult.Information)
                            {
                                Console.Error.WriteLine(information);
                            }
                        }
                    }
                }
                process.WaitForExit();
            }
            catch (Exception e)
            {
                failed = true;
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.StackTrace);
            }

            if (failed)
            {
                throw new Exception("pac wrapper failed!");
            }
            return string.Join("\r\n", result.Skip(1));
        }
    }
}
