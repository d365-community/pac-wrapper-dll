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
            var encoding = Encoding.GetEncoding(int.Parse(Environment.GetEnvironmentVariable("D365_PAC_CODEPAGE") ?? "1252"));
            var pacDebug = bool.Parse(Environment.GetEnvironmentVariable("D365_PAC_DEBUG") ?? "false");
            var pacTrace = bool.Parse(Environment.GetEnvironmentVariable("D365_PAC_TRACE") ?? "false");
            if (pacDebug) Console.WriteLine($"pac: {pac}");
            var result = new List<string>();
            var failed = false;
            try
            {
                Console.InputEncoding = encoding;
                Console.OutputEncoding = encoding;
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = pac,
                        Arguments = "--non-interactive",
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardErrorEncoding = encoding,
                        StandardOutputEncoding = encoding,
                        CreateNoWindow = true
                    }
                };
                process.Start();

                var verbose = args.Any(a => "--verbose".Equals(a) || "--verbose-wrapper".Equals(a));

                var arguments = "{\"Arguments\":[" + string.Join(",", args.Select(a => $"\"{a}\"")) + "]}" + Environment.NewLine;
                if (pacTrace) Console.Write(arguments);
                var exit = "{\"Arguments\":[\"exit\"]}" + Environment.NewLine;

                var argumentsBuffer = encoding.GetBytes(arguments);
                var exitBuffer = encoding.GetBytes(exit);

                using (var bs = process.StandardInput.BaseStream)
                {
                    bs.Write(argumentsBuffer, 0, argumentsBuffer.Length);
                    bs.Write(exitBuffer, 0, exitBuffer.Length);
                    bs.Close();
                }

                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;
                    if (pacDebug) Console.WriteLine(line);
                    result.Add(line);
                    using (var ms = new MemoryStream(encoding.GetBytes(line)))
                    {
                        var pacResult = (PacResult)new DataContractJsonSerializer(typeof(PacResult), Settings).ReadObject(ms);
                        if (pacResult.Status?.ToLowerInvariant() == "success")
                        {
                            foreach (var warning in pacResult.Warnings)
                            {
                                Console.WriteLine($"Warning: {warning}");
                            }
                            if (pacDebug || verbose)
                            {
                                foreach (var information in pacResult.Information)
                                {
                                    Console.WriteLine(information);
                                }
                            }
                        }
                        else
                        {
                            failed = true;
                            foreach (var error in pacResult.Errors)
                            {
                                Console.Error.WriteLine($"Error: {error}");
                            }
                            foreach (var warning in pacResult.Warnings)
                            {
                                Console.WriteLine($"Warning: {warning}");
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
                }
                process.WaitForExit();
            }
            catch (Exception e)
            {
                failed = true;
                Console.Error.WriteLine($"{e.GetBaseException().GetType().Name}: {e.GetBaseException().Message}");
                Console.Error.WriteLine(e.StackTrace);
                Console.Error.WriteLine("--------------------- StandardOut ---------------------");
                Console.Error.WriteLine(string.Join("\r\n", result));
            }

            if (failed)
            {
                throw new Exception("pac wrapper failed!");
            }
            return string.Join("\r\n", result.Skip(1));
        }
    }
}
