using DurableTask.Core.Serializing;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Extensions;
using maskx.ARMOrchestration.Orchestrations;
using maskx.DurableTask.SQLServer.SQL;
using maskx.Expression;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace maskx.ARMOrchestration.Functions
{
    public class ARMFunctions
    {
        private readonly Dictionary<string, Action<FunctionArgs, Dictionary<string, object>>> Functions = new Dictionary<string, Action<FunctionArgs, Dictionary<string, object>>>();
        private readonly ARMOrchestrationOptions options;
        private readonly IServiceProvider serviceProvider;
        private readonly IInfrastructure infrastructure;
        private readonly DataConverter _DataConverter = new JsonDataConverter();

        public ARMFunctions(IOptions<ARMOrchestrationOptions> options,
            IServiceProvider serviceProvider,
            IInfrastructure infrastructure)
        {
            this.options = options?.Value;
            this.serviceProvider = serviceProvider;
            this.infrastructure = infrastructure;

            this.InitBuiltInFunction();
        }

        private void InitBuiltInFunction()
        {
            #region Array and object

            Functions.Add("array", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                string str = string.Empty;
                if (par1 is string)
                {
                    str = $"[\"{par1}\"]";
                }
                else if (par1 is bool f)
                {
                    str = f ? "true" : "false";
                }
                else if (par1 is JsonValue)
                {
                    str = $"[{(par1 as JsonValue).RawString}]";
                }
                else
                    str = $"[{par1}]";
                args.Result = new JsonValue(str);
            });
            Functions.Add("coalesce", (args, cxt) =>
            {
                for (int i = 0; i < args.Parameters.Length; i++)
                {
                    var a = args.Parameters[i].Evaluate(cxt);
                    if (a != null)
                    {
                        args.Result = a;
                        break;
                    }
                }
            });
            Functions.Add("concat", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                if (pars[0] is JsonValue)
                    args.Result = new JsonValue($"[{string.Join(",", pars)}]");
                else
                    args.Result = string.Join("", pars);
            });
            Functions.Add("contains", (args, cxt) =>
            {
                args.Result = false;
                var pars = args.EvaluateParameters(cxt);
                if (pars[0] is string s)
                {
                    if (s.IndexOf(pars[1] as string) > 0)
                        args.Result = true;
                }
                else if (pars[0] is JsonValue jv)
                {
                    args.Result = jv.Contains(pars[1]);
                }
            });
            Functions.Add("createarray", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = new JsonValue($"[{string.Join(",", pars.Select(JsonValue.PackageJson))}]");
            });
            Functions.Add("first", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                if (par1 is string s)
                    args.Result = s[0].ToString();
                else if (par1 is JsonValue jv)
                    args.Result = jv[0];
            });
            Functions.Add("intersection", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                JsonValue jv = pars[0] as JsonValue;
                for (int i = 1; i < pars.Length; i++)
                {
                    jv = jv.Intersect(pars[i] as JsonValue);
                }
                args.Result = jv;
            });
            Functions.Add("json", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-expressions#null-values
                if (par1.ToString() == "null")
                    args.Result = null;
                else
                    args.Result = new JsonValue(par1.ToString());
            });
            Functions.Add("last", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                if (par1 is string s)
                    args.Result = s.Last().ToString();
                else if (par1 is JsonValue jv)
                    args.Result = jv[^1];
            });
            Functions.Add("length", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                if (par1 is string s)
                    args.Result = s.Length;
                else if (par1 is JsonValue jv)
                    args.Result = jv.Length;
            });
            Functions.Add("max", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                if (pars[0] is JsonValue jv)
                {
                    args.Result = jv.Max();
                }
                else
                {
                    args.Result = pars.Max();
                }
            });
            Functions.Add("min", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                if (pars[0] is JsonValue jv)
                {
                    args.Result = jv.Min();
                }
                else
                {
                    args.Result = pars.Min();
                }
            });
            Functions.Add("range", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                List<int> nums = new List<int>();
                int index = Convert.ToInt32(pars[0]);
                int length = index + Convert.ToInt32(pars[1]);
                for (; index < length; index++)
                {
                    nums.Add(index);
                }
                args.Result = new JsonValue($"[{string.Join(",", nums)}]");
            });
            Functions.Add("skip", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                int numberToSkip = Convert.ToInt32(pars[1]);
                if (pars[0] is string s)
                {
                    args.Result = s.Substring(numberToSkip);
                }
                else if (pars[0] is JsonValue jv)
                {
                    List<string> ele = new List<string>();
                    for (int i = numberToSkip; i < jv.Length; i++)
                    {
                        ele.Add(JsonValue.PackageJson(jv[i]));
                    }
                    args.Result = new JsonValue($"[{string.Join(",", ele)}]");
                }
            });
            Functions.Add("take", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                int numberToTake = Convert.ToInt32(pars[1]);
                if (pars[0] is string s)
                {
                    args.Result = s.Substring(0, numberToTake);
                }
                else if (pars[0] is JsonValue jv)
                {
                    List<string> ele = new List<string>();
                    for (int i = 0; i < numberToTake; i++)
                    {
                        ele.Add(JsonValue.PackageJson(jv[i]));
                    }
                    args.Result = new JsonValue($"[{string.Join(",", ele)}]");
                }
            });
            Functions.Add("union", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                JsonValue jv = pars[0] as JsonValue;
                for (int i = 1; i < pars.Length; i++)
                {
                    jv = jv.Union(pars[i] as JsonValue);
                }
                args.Result = jv;
            });

            #endregion Array and object

            #region Comparison

            Functions.Add("equals", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                if (pars[0] is JsonValue jv)
                {
                    args.Result = jv.Equals(pars[1]);
                }
                else
                {
                    args.Result = pars[0].ToString() == pars[1].ToString();
                }
            });
            Functions.Add("greater", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = false;
                if (pars[0] is string s)
                {
                    args.Result = string.Compare(s, pars[1] as string) > 0;
                }
                else
                {
                    //ARM only support int
                    args.Result = Convert.ToInt32(pars[0]) > Convert.ToInt32(pars[1]);
                }
            });
            Functions.Add("greaterorequals", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = false;
                if (pars[0] is string s)
                {
                    args.Result = string.Compare(s, pars[1] as string) >= 0;
                }
                else
                {
                    //ARM only support int
                    args.Result = Convert.ToInt32(pars[0]) >= Convert.ToInt32(pars[1]);
                }
            });
            Functions.Add("less", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = false;
                if (pars[0] is string s)
                {
                    args.Result = string.Compare(s, pars[1] as string) < 0;
                }
                else
                {
                    //ARM only support int
                    args.Result = Convert.ToInt32(pars[0]) < Convert.ToInt32(pars[1]);
                }
            });
            Functions.Add("lessorequals", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = false;
                if (pars[0] is string s)
                {
                    args.Result = string.Compare(s, pars[1] as string) <= 0;
                }
                else
                {
                    //ARM only support int
                    args.Result = Convert.ToInt32(pars[0]) <= Convert.ToInt32(pars[1]);
                }
            });

            #endregion Comparison

            #region Logical

            Functions.Add("and", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = true;
                foreach (var item in pars)
                {
                    if (!(bool)item)
                    {
                        args.Result = false;
                        break;
                    }
                }
            });
            Functions.Add("bool", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt).ToString().ToLower();
                if (par1 == "1" || par1 == "true")
                    args.Result = true;
                else
                    args.Result = false;
            });
            Functions.Add("if", (args, cxt) =>
            {
                if ((bool)args.Parameters[0].Evaluate(cxt))
                    args.Result = args.Parameters[1].Evaluate(cxt);
                else
                    args.Result = args.Parameters[2].Evaluate(cxt);
            });
            Functions.Add("not", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                args.Result = !(bool)par1;
            });
            Functions.Add("or", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = false;
                foreach (var item in pars)
                {
                    if ((bool)item)
                    {
                        args.Result = true;
                        break;
                    }
                }
            });

            #endregion Logical

            #region Deployment

            Functions.Add("parameters", (args, cxt) =>
            {
                // TODO: support securestring
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/key-vault-parameter
                // may be need implement at Resource Provider

                var par1 = args.Parameters[0].Evaluate(cxt).ToString();

                // this is User Defined Functions
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-user-defined-functions
                if (cxt.TryGetValue(ContextKeys.UDF_CONTEXT, out object udfContext))
                {
                    using var jsonDoc = JsonDocument.Parse(udfContext.ToString());
                    if (jsonDoc.RootElement.TryGetProperty(par1, out JsonElement ele))
                    {
                        if (ele.TryGetProperty("value", out JsonElement v))
                        {
                            args.Result = JsonValue.GetElementValue(v);
                        }
                    }
                }
                else
                {
                    if (!cxt.TryGetValue(ContextKeys.ARM_CONTEXT, out object armcxt))
                        throw new Exception("cannot find context in parameters function");
                    var context = armcxt as DeploymentOrchestrationInput;

                    if (!string.IsNullOrEmpty(context.Parameters))
                    {
                        using var jsonDoc = JsonDocument.Parse(context.Parameters);
                        if (!jsonDoc.RootElement.TryGetProperty(par1, out JsonElement ele))
                        {
                            if (ele.TryGetProperty("value", out JsonElement v))
                            {
                                args.Result = JsonValue.GetElementValue(v);
                            }
                        }
                    }
                    if (!args.HasResult)
                    {
                        var pds = context.Template.Parameters;
                        using var defineDoc = JsonDocument.Parse(pds);
                        if (!defineDoc.RootElement.TryGetProperty(par1, out JsonElement parEleDef))
                        {
                            throw new Exception($"ARM Template does not define the parameter:{par1}");
                        }
                        if (parEleDef.TryGetProperty("defaultValue", out JsonElement defValue))
                        {
                            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/linked-templates#using-variables-to-link-templates
                            // paramete's default value can be included function
                            args.Result = JsonValue.GetElementValue(defValue);
                            if (defValue.ValueKind == JsonValueKind.String)
                                args.Result = this.Evaluate(args.Result.ToString(), cxt);
                        }
                    }
                }

                if (args.Result is string s)
                    args.Result = Evaluate(s, cxt);
            });
            Functions.Add("variables", (args, cxt) =>
            {
                if (!cxt.TryGetValue(ContextKeys.ARM_CONTEXT, out object armcxt))
                    return;
                string vars = (armcxt as DeploymentOrchestrationInput).Template.Variables.ToString();

                var par1 = args.Parameters[0].Evaluate(cxt).ToString();
                using var defineDoc = JsonDocument.Parse(vars);
                if (defineDoc.RootElement.TryGetProperty(par1, out JsonElement parEleDef))
                {
                    args.Result = JsonValue.GetElementValue(parEleDef);
                }
                else
                    throw new Exception($"ARM Template does not define the variables:{par1}");
                if (args.Result is string s)
                    args.Result = Evaluate(s, cxt);
            });
            Functions.Add("deployment", (args, cxt) =>
            {
                var input = cxt[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
                int stage = 0;
                using (var db = new DbAccess(options.Database.ConnectionString))
                {
                    db.AddStatement($"select TOP 1 Stage from {options.Database.DeploymentOperationsTableName} where DeploymentId=@DeploymentId and Name=@Name ",
                              new
                              {
                                  input.DeploymentId,
                                  Name = input.Name
                              });
                    db.ExecuteReaderAsync((r, resultSet) =>
                    {
                        stage = (int)r["Stage"];
                    }).Wait();
                }
                using MemoryStream ms = new MemoryStream();
                using Utf8JsonWriter writer = new Utf8JsonWriter(ms);
                writer.WriteStartObject();
                writer.WriteString("name", input.Name);
                writer.WritePropertyName("properties");
                writer.WriteStartObject();
                if (!string.IsNullOrEmpty(input.Parameters))
                    writer.WriteRawString("parameters", input.Parameters);
                if (input.TemplateLink != null)
                {
                    writer.WritePropertyName("templateLink");
                    writer.WriteStartObject();
                    writer.WriteString("uri", input.TemplateLink.Uri);
                    writer.WriteString("contentVersion", input.TemplateLink.ContentVersion);
                    writer.WriteEndObject();
                }
                writer.WriteRawString("template", input.Template.ToString());
                writer.WriteString("mode", input.Mode.ToString().ToLower());
                writer.WriteString("provisioningState", stage.ToString());
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.Flush();
                args.Result = new JsonValue(Encoding.UTF8.GetString(ms.ToArray()));
            });

            #endregion Deployment

            #region String

            Functions.Add("base64", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                var plainTextBytes = Encoding.UTF8.GetBytes(par1 as string);
                args.Result = Convert.ToBase64String(plainTextBytes);
            });
            Functions.Add("base64tostring", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                var base64EncodedBytes = Convert.FromBase64String(par1 as string);
                args.Result = Encoding.UTF8.GetString(base64EncodedBytes);
            });
            Functions.Add("base64tojson", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                var base64EncodedBytes = Convert.FromBase64String(par1 as string);
                args.Result = new JsonValue(Encoding.UTF8.GetString(base64EncodedBytes));
            });
            Functions.Add("datauri", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                var plainTextBytes = Encoding.UTF8.GetBytes(par1 as string);
                args.Result = "data:text/plain;charset=utf8;base64," + Convert.ToBase64String(plainTextBytes);
            });
            Functions.Add("datauritostring", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                var s = (par1 as string);
                s = s.Substring(s.LastIndexOf(',') + 1).Trim();
                var base64EncodedBytes = Convert.FromBase64String(s);
                args.Result = Encoding.UTF8.GetString(base64EncodedBytes);
            });
            Functions.Add("endswith", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = (pars[0] as string).EndsWith(pars[1] as string, StringComparison.InvariantCultureIgnoreCase);
            });
            Functions.Add("startswith", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = (pars[0] as string).StartsWith(pars[1] as string, StringComparison.InvariantCultureIgnoreCase);
            });
            Functions.Add("format", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = string.Format((pars[0] as string), pars.Skip(1).ToArray());
            });
            Functions.Add("guid", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] bytes = md5.ComputeHash(Encoding.Unicode.GetBytes(string.Join('-', pars)));
                args.Result = new Guid(bytes).ToString();
            });
            // https://stackoverflow.com/a/48305669
            Functions.Add("uniquestring", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                string result = "";
                var buffer = Encoding.UTF8.GetBytes(string.Join('-', pars));
                var hashArray = new SHA512Managed().ComputeHash(buffer);
                for (int i = 1; i <= 13; i++)
                {
                    var b = hashArray[i];
                    if (b >= 48 && b <= 57)// keep number
                        result += Convert.ToChar(b);
                    else // change to letter
                        result += Convert.ToChar((b % 26) + (byte)'a');
                }
                args.Result = result;
            });
            Functions.Add("indexof", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = (pars[0] as string).IndexOf(pars[1] as string, StringComparison.InvariantCultureIgnoreCase);
            });
            Functions.Add("lastindexof", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = (pars[0] as string).LastIndexOf(pars[1] as string, StringComparison.InvariantCultureIgnoreCase);
            });
            Functions.Add("padleft", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                var s = pars[0] as string;
                var width = (int)pars[1];
                if (pars.Length > 2)
                {
                    char c = pars[2].ToString()[0];
                    args.Result = s.PadLeft(width, c);
                }
                else
                    args.Result = s.PadLeft(width);
            });
            Functions.Add("replace", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = (pars[0] as string).Replace(pars[1] as string, pars[2] as string);
            });
            Functions.Add("split", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                var str = pars[0] as string;
                string[] rtv;
                if (pars[1] is string split)
                    rtv = str.Split(split[0]);
                else if (pars[1] is JsonValue jv)
                {
                    var splits = new char[jv.Length];
                    for (int i = 0; i < splits.Length; i++)
                    {
                        splits[i] = (jv[i] as string)[0];
                    }
                    rtv = str.Split(splits);
                }
                else
                    rtv = null;
                args.Result = new JsonValue($"[\"{ string.Join("\",\"", rtv)}\"]");
            });
            Functions.Add("newguid", (args, cxt) =>
            {
                args.Result = Guid.NewGuid().ToString();
            });
            Functions.Add("string", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                args.Result = par1.ToString();
            });
            Functions.Add("empty", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                args.Result = false;
                if (par1 is null)
                    args.Result = true;
                else if (par1 is JsonValue)
                {
                    var p = par1 as JsonValue;
                    if (p.RawString == "{}" || p.RawString == "[]" || p.RawString == "null")
                        args.Result = true;
                }
                else if (par1 is string && string.IsNullOrEmpty(par1 as string))
                    args.Result = true;
            });
            Functions.Add("substring", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                var s = pars[0] as string;
                var startIndex = (int)pars[1];
                if (pars.Length > 2)
                {
                    args.Result = s.Substring(startIndex, (int)pars[2]);
                }
                else
                {
                    args.Result = s.Substring(startIndex);
                }
            });
            Functions.Add("tolower", (args, cxt) =>
            {
                args.Result = args.Parameters[0].Evaluate(cxt).ToString().ToLower();
            });
            Functions.Add("toupper", (args, cxt) =>
            {
                args.Result = args.Parameters[0].Evaluate(cxt).ToString().ToUpper();
            });
            Functions.Add("trim", (args, cxt) =>
            {
                args.Result = args.Parameters[0].Evaluate(cxt).ToString().Trim();
            });
            Functions.Add("uri", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = System.IO.Path.Combine(pars[0] as string, pars[1] as string);
            });
            Functions.Add("uricomponent", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                args.Result = Uri.EscapeDataString(par1 as string);
            });
            Functions.Add("uricomponenttostring", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                args.Result = Uri.UnescapeDataString(par1 as string);
            });
            Functions.Add("utcnow", (args, cxt) =>
            {
                if (args.Parameters.Length > 0)
                {
                    args.Result = DateTime.UtcNow.ToString(args.Parameters[0].Evaluate(cxt).ToString());
                }
                else
                {
                    args.Result = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
                }
            });

            #endregion String

            #region Numeric

            Functions.Add("add", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = Convert.ToInt32(pars[0]) + Convert.ToInt32(pars[1]);
            });
            Functions.Add("copyindex", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                string loopName = string.Empty; ;
                int offset = 0;
                if (pars.Length == 0)
                {
                    loopName = cxt[ContextKeys.CURRENT_LOOP_NAME].ToString();
                }
                else if (pars.Length == 1)
                {
                    if (pars[0] is string s)
                    {
                        loopName = s;
                    }
                    else
                    {
                        loopName = cxt[ContextKeys.CURRENT_LOOP_NAME].ToString();
                        offset = (int)pars[0];
                    }
                }
                else
                {
                    loopName = pars[0] as string;
                    offset = (int)pars[1];
                }
                args.Result = (cxt[ContextKeys.COPY_INDEX] as Dictionary<string, int>)[loopName] + offset;
            });
            Functions.Add("div", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = Convert.ToInt32(pars[0]) / Convert.ToInt32(pars[1]);
            });
            Functions.Add("float", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                args.Result = Convert.ToDecimal(par1);
            });
            Functions.Add("int", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                args.Result = Convert.ToInt32(par1);
            });
            Functions.Add("mod", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                var d = Math.DivRem(Convert.ToInt32(pars[0]), Convert.ToInt32(pars[1]), out int result);
                args.Result = result;
            });
            Functions.Add("mul", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = Convert.ToInt32(pars[0]) * Convert.ToInt32(pars[1]);
            });
            Functions.Add("sub", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = Convert.ToInt32(pars[0]) - Convert.ToInt32(pars[1]);
            });

            #endregion Numeric

            #region Resource

            Functions.Add("extensionresourceid", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                var fullnames = pars[1].ToString().Split('/');
                string nestr = "";
                int typeIndex = 2;
                foreach (var item in pars.Skip(3))
                {
                    nestr += $"/{fullnames[typeIndex]}/{item}";
                    typeIndex++;
                }
                args.Result = $"{pars[0]}/{infrastructure.BuiltinPathSegment.Provider}/{fullnames[0]}/{fullnames[1]}/{pars[2]}/{nestr}";
            });
            Functions.Add("resourceid", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                var input = cxt[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
                var t = input.Template;
                if (t.DeployLevel == DeployLevel.ResourceGroup)
                    args.Result = ResourceId(input, pars);
                else if (t.DeployLevel == DeployLevel.Subscription)
                    args.Result = SubscriptionResourceId(input, pars);
                else
                    args.Result = TenantResourceId(pars);
            });
            Functions.Add("managementgroupresourceid", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                var input = cxt[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
                args.Result = ManagementResourceId(input, pars);
            });
            Functions.Add("subscriptionresourceid", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                var input = cxt[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
                args.Result = SubscriptionResourceId(input, pars);
            });
            Functions.Add("tenantresourceid", (args, cxt) =>
            {
                args.Result = TenantResourceId(args.EvaluateParameters(cxt));
            });

            Functions.Add("reference", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                string resourceName = pars[0].ToString();
                var context = cxt[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
                string apiVersion = string.Empty;
                if (pars.Length > 1)
                    apiVersion = pars[1].ToString();
                bool full = false;
                if (pars.Length > 2)
                    full = "full".Equals(pars[2].ToString(), StringComparison.InvariantCultureIgnoreCase);
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#implicit-dependency
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource#resource-name-or-identifier
                // if the referenced resource is provisioned within same template and you refer to the resource by its name (not resource ID)
                // reference 'ResourceProvider/ServiceType/ResourceName' will create a implicit dependency
                if (!(resourceName.StartsWith(infrastructure.BuiltinPathSegment.ManagementGroup)
                || resourceName.StartsWith(infrastructure.BuiltinPathSegment.Subscription)
                || resourceName.StartsWith(infrastructure.BuiltinPathSegment.ResourceGroup)))
                {
                    List<string> dependsOn;
                    if (cxt.TryGetValue(ContextKeys.DEPENDSON, out object d))
                    {
                        dependsOn = d as List<string>;
                    }
                    else
                    {
                        dependsOn = new List<string>();
                        cxt.Add(ContextKeys.DEPENDSON, dependsOn);
                    }
                    dependsOn.Add(resourceName);
                }
                if (!context.IsRuntime)
                {
                    args.Result = new FakeJsonValue(resourceName);
                }
                else
                {
                    string id = resourceName;
                    if (!(resourceName.StartsWith(infrastructure.BuiltinPathSegment.ManagementGroup)
                    || resourceName.StartsWith(infrastructure.BuiltinPathSegment.Subscription)
                    || resourceName.StartsWith(infrastructure.BuiltinPathSegment.ResourceGroup)))
                    {
                        id = context.Template.Resources[resourceName].ResourceId;
                    }
                    var taskResult = this.infrastructure.Reference(context, id, apiVersion, full);
                    if (taskResult.Code == 200)
                        args.Result = new JsonValue(taskResult.Content.ToString());
                    else
                        args.Result = null;
                }
            });

            Functions.Add("resourcegroup", (args, cxt) =>
            {
                var context = cxt[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput;
                var taskResult = this.infrastructure.Reference(
                    context,
                    $"/{infrastructure.BuiltinPathSegment.Subscription}/{context.SubscriptionId}/{infrastructure.BuiltinPathSegment.ResourceGroup}/{context.ResourceGroup}",
                    string.Empty,
                    true);
                if (taskResult.Code == 200)
                {
                    args.Result = new JsonValue(taskResult.Content.ToString());
                }
                else
                {
                    args.Result = taskResult.Content;
                }
            });

            #endregion Resource
        }

        public string ResourceId(DeploymentOrchestrationInput input, params object[] pars)
        {
            string groupType = infrastructure.BuiltinPathSegment.Subscription;
            string groupId = input.SubscriptionId;
            if (!string.IsNullOrEmpty(input.ManagementGroupId))
            {
                groupType = infrastructure.BuiltinPathSegment.ManagementGroup;
                groupId = input.ManagementGroupId;
            }
            string resourceGroupName = input.ResourceGroup;
            string[] fullnames;
            IEnumerable<object> nestResources;
            string resource;
            if (pars[0].ToString().IndexOf('/') > 0)
            {
                fullnames = pars[0].ToString().Split('/');
                resource = pars[1].ToString();
                nestResources = pars.Skip(2);
            }
            else if (Guid.TryParse(pars[0].ToString(), out Guid subid))
            {
                groupId = subid.ToString();
                if (pars[1].ToString().IndexOf('/') > 0)
                {
                    fullnames = pars[1].ToString().Split('/');
                    resource = pars[2].ToString();
                    nestResources = pars.Skip(3);
                }
                else
                {
                    resourceGroupName = pars[1].ToString();
                    fullnames = pars[2].ToString().Split('/');
                    resource = pars[3].ToString();
                    nestResources = pars.Skip(4);
                }
            }
            else
            {
                resourceGroupName = pars[0].ToString();
                fullnames = pars[1].ToString().Split('/');
                resource = pars[2].ToString();
                nestResources = pars.Skip(3);
            }
            string nestr = "";
            int typeIndex = 2;
            foreach (var item in nestResources)
            {
                nestr += $"/{fullnames[typeIndex]}/{item}";
                typeIndex++;
            }
            if (string.IsNullOrEmpty(resourceGroupName))
                return $"/{groupType}/{groupId}/{infrastructure.BuiltinPathSegment.Provider}/{fullnames[0]}/{fullnames[1]}/{resource}{nestr}";
            return $"/{groupType}/{groupId}/{infrastructure.BuiltinPathSegment.ResourceGroup}/{resourceGroupName}/{infrastructure.BuiltinPathSegment.Provider}/{fullnames[0]}/{fullnames[1]}/{resource}{nestr}";
        }

        public string SubscriptionResourceId(DeploymentOrchestrationInput input, params object[] pars)
        {
            string subscriptionId = input.SubscriptionId;
            string[] fullnames;
            IEnumerable<object> nestResources;
            string resource;
            if (pars[0].ToString().IndexOf('/') > 0)
            {
                fullnames = pars[0].ToString().Split('/');
                resource = pars[1].ToString();
                nestResources = pars.Skip(2);
            }
            else
            {
                subscriptionId = pars[0].ToString();
                fullnames = pars[1].ToString().Split('/');
                resource = pars[2].ToString();
                nestResources = pars.Skip(3);
            }
            string nestr = "";
            int typeIndex = 2;
            foreach (var item in nestResources)
            {
                nestr += $"/{fullnames[typeIndex]}/{item}";
                typeIndex++;
            }
            return $"/{infrastructure.BuiltinPathSegment.Subscription}/{subscriptionId}/{infrastructure.BuiltinPathSegment.Provider}/{fullnames[0]}/{fullnames[1]}/{resource}{nestr}";
        }

        public string ManagementResourceId(DeploymentOrchestrationInput input, params object[] pars)
        {
            string managementId = input.ManagementGroupId;
            string[] fullnames;
            IEnumerable<object> nestResources;
            string resource;
            if (pars[0].ToString().IndexOf('/') > 0)
            {
                fullnames = pars[0].ToString().Split('/');
                resource = pars[1].ToString();
                nestResources = pars.Skip(2);
            }
            else
            {
                managementId = pars[0].ToString();
                fullnames = pars[1].ToString().Split('/');
                resource = pars[2].ToString();
                nestResources = pars.Skip(3);
            }
            string nestr = "";
            int typeIndex = 2;
            foreach (var item in nestResources)
            {
                nestr += $"/{fullnames[typeIndex]}/{item}";
                typeIndex++;
            }
            return $"/{infrastructure.BuiltinPathSegment.ManagementGroup}/{managementId}/{infrastructure.BuiltinPathSegment.Provider}/{fullnames[0]}/{fullnames[1]}/{resource}{nestr}";
        }

        public string TenantResourceId(params object[] pars)
        {
            string[] fullnames;
            IEnumerable<object> nestResources;

            fullnames = pars[0].ToString().Split('/');
            string resource = pars[1].ToString();
            nestResources = pars.Skip(2);

            string nestr = "";
            int typeIndex = 2;
            foreach (var item in nestResources)
            {
                nestr += $"/{fullnames[typeIndex]}/{item}";
                typeIndex++;
            }
            return $"/{infrastructure.BuiltinPathSegment.Provider}/{fullnames[0]}/{fullnames[1]}/{resource}{nestr}";
        }

        /// <summary>
        /// Evaluate expression
        /// </summary>
        /// <param name="function"></param>
        /// <param name="context">
        /// parametersdefine
        /// variabledefine
        /// functionsdefine
        /// parameters
        /// </param>
        /// <returns></returns>
        public object Evaluate(string function, Dictionary<string, object> context)
        {
            if (string.IsNullOrEmpty(function))
                return string.Empty;
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-expressions#escape-characters
            if (function.StartsWith("[") && function.EndsWith("]"))
            {
                if (function.StartsWith("[["))
                    return function.Remove(0, 1);
                string functionString = function[1..^1];
                var expression = new Expression.Expression(functionString)
                {
                    EvaluateFunction = (name, args, cxt) =>
                    {
                        if (Functions.TryGetValue(name.ToLower(), out Action<FunctionArgs, Dictionary<string, object>> func))
                        {
                            func(args, cxt);
                        }
                        else if (TryGetCustomFunction(name, context, out Member member))
                        {
                            EvaluateCustomFunc(args, cxt, member);
                        }
                        else if (name.StartsWith("list", StringComparison.OrdinalIgnoreCase))
                        {
                            var pars = args.EvaluateParameters(context);
                            var resourceName = pars[0].ToString();
                            if (!(cxt[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput).IsRuntime)
                            {
                                if (resourceName.IndexOf('/') < 0)
                                {
                                    List<string> dependsOn;
                                    if (cxt.TryGetValue(ContextKeys.DEPENDSON, out object d))
                                    {
                                        dependsOn = d as List<string>;
                                    }
                                    else
                                    {
                                        dependsOn = new List<string>();
                                        cxt.Add(ContextKeys.DEPENDSON, dependsOn);
                                    }
                                    dependsOn.Add(resourceName);
                                }
                                cxt.TryAdd(ContextKeys.NEED_REEVALUATE, true);
                                args.Result = new FakeJsonValue(resourceName);
                            }
                            else
                            {
                                var r = this.infrastructure.List(
                                                                cxt[ContextKeys.ARM_CONTEXT] as DeploymentOrchestrationInput,
                                                                resourceName,
                                                                pars[1].ToString(),
                                                                pars.Length == 3 ? pars[2].ToString() : string.Empty,
                                                                name.Remove(0, 4));
                                args.Result = r.Content;
                            }
                        }
                    }
                };
                return expression.Evaluate(context);
            }
            return function;
        }

        private void EvaluateCustomFunc(FunctionArgs args, Dictionary<string, object> cxt, Member member)
        {
            Dictionary<string, object> udfContext = new Dictionary<string, object>();
            using JsonDocument udfPar = JsonDocument.Parse(member.Parameters);
            var rootEle = udfPar.RootElement;
            var pars = args.EvaluateParameters(cxt);
            JObject jObject = new JObject();
            for (int i = 0; i < pars.Length; i++)
            {
                var t = rootEle[i].GetProperty("type").GetString();
                JProperty p;
                if ("object" == t)
                    p = new JProperty("value", JObject.Parse(pars[i].ToString()));
                else if ("array" == t)
                    p = new JProperty("value", JArray.Parse(pars[i].ToString()));
                else
                    p = new JProperty("value", pars[i]);
                jObject.Add(rootEle[i].GetProperty("name").GetString(), new JObject(p));
            }
            foreach (var key in cxt.Keys)
            {
                udfContext.Add(key, cxt[key]);
            }
            udfContext.Add(ContextKeys.UDF_CONTEXT, jObject.ToString(Newtonsoft.Json.Formatting.None));

            args.Result = GetOutput(member.Output, udfContext);
        }

        private bool TryGetCustomFunction(string function, Dictionary<string, object> context, out ARMTemplate.Member member)
        {
            member = null;
            if (!context.TryGetValue(ContextKeys.ARM_CONTEXT, out object armcxt))
                return false;
            var udfs = (armcxt as DeploymentOrchestrationInput).Template.Functions;
            if (udfs == null)
                return false;
            var names = function.Split('.');
            if (names.Length != 2)
                return false;
            var ns = udfs[names[0]];
            if (ns == null)
                return false;
            member = ns[names[1]];
            if (member == null)
                return false;
            return true;
        }

        public void SetFunction(string name, Action<FunctionArgs, Dictionary<string, object>> func)
        {
            Functions[name] = func;
        }

        public object GetOutput(string output, Dictionary<string, object> context)
        {
            using JsonDocument outDoc = JsonDocument.Parse(output);
            var rootEle = outDoc.RootElement;
            var type = rootEle.GetProperty("type").GetString();
            var value = rootEle.GetProperty("value").GetString();
            var v = this.Evaluate(value, context);
            return v;
        }
    }
}