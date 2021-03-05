using DurableTask.Core.Serializing;
using maskx.ARMOrchestration.ARMTemplate;
using maskx.ARMOrchestration.Extensions;
using maskx.DurableTask.SQLServer.SQL;
using maskx.Expression;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
            #region https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-array?tabs=json
            Functions.Add("array", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt)[0];
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
            Functions.Add("empty", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt)[0];
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
            Functions.Add("first", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt)[0];
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
            Functions.Add("last", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt)[0];
                if (par1 is string s)
                    args.Result = s.Last().ToString();
                else if (par1 is JsonValue jv)
                    args.Result = jv[^1];
            });
            Functions.Add("length", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
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
                    args.Result = s[numberToSkip..];
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
            #endregion

            #region https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-comparison?tabs=json
            Functions.Add("coalesce", (args, cxt) =>
            {
                for (int i = 0; i < args.Parameters.Length; i++)
                {
                    var a = EvaluateParameters(args, cxt, i);
                    if (a != null)
                    {
                        args.Result = a;
                        break;
                    }
                }
            });
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


            #endregion Array and object

            #region https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-date?tabs=json

            Functions.Add("datetimeadd", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                if (!DateTime.TryParseExact(pars[0].ToString(), "yyyyMMdd'T'HHmmss'Z'", null, System.Globalization.DateTimeStyles.None, out DateTime dt))
                    throw new Exception($"wrong format of datetime {pars[0]}");
                var duration = pars[1].ToString();
                bool negative = false;
                bool isMonth = false;
                int num = 0;
                for (int i = 0; i < duration.Length; i++)
                {
                    switch (duration[i])
                    {
                        case '-': negative = true; break;
                        case 'P': isMonth = true; break;
                        case 'T': isMonth = false; break;
                        case 'Y': dt.AddYears(num); break;
                        case 'M':
                            if (isMonth) dt.AddMonths(num);
                            else dt.AddMinutes(num);
                            break;
                        case 'W': dt.AddDays(num * 7); break;
                        case 'D': dt.AddDays(num); break;
                        case 'H': dt.AddHours(num); break;
                        case 'S': dt.AddSeconds(num); break;
                        default:
                            int.TryParse(duration[i].ToString(), out num);
                            num = negative ? 0 - num : num;
                            break;
                    }
                }
                args.Result = dt.ToString("yyyyMMdd'T'HHmmss'Z'");
            });
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-date?tabs=json#utcnow
            // This function can only be used in the default value for a parameter.
            Functions.Add("utcnow", (args, cxt) =>
            {
                if (args.Parameters.Length > 0)
                {
                    args.Result = DateTime.UtcNow.ToString(EvaluateParameters(args, cxt, 0).ToString());
                }
                else
                {
                    args.Result = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'");
                }
            });
            #endregion

            #region https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-deployment?tabs=json
            Functions.Add("deployment", (args, cxt) =>
            {
                var input = cxt[ContextKeys.ARM_CONTEXT] as Deployment;
                int stage = 0;
                using (var db = new DbAccess(options.Database.ConnectionString))
                {
                    db.AddStatement($"select TOP 1 Stage from {options.Database.DeploymentOperationsTableName} where DeploymentId=@DeploymentId and Name=@Name ",
                              new
                              {
                                  input.DeploymentId,
                                  input.Name
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
            Functions.Add("environment", (args, cxt) =>
            {
                // TODO: environment
            });
            Functions.Add("parameters", (args, cxt) =>
            {
                // TODO: support securestring
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/key-vault-parameter
                // may be need implement at Resource Provider
                var par1 = EvaluateParameters(args, cxt, 0).ToString();
                var rtv = GetParameter(par1, cxt);
                args.Result = rtv.Result;
            });
            Functions.Add("variables", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0).ToString();
                var rtv = GetVariables(par1, cxt);
                args.Result = rtv.Result;
            });
            #endregion Deployment

            #region https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-logical?tabs=json

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
                var par1 = EvaluateParameters(args, cxt, 0).ToString().ToLower();
                if (par1 == "1" || par1 == "true")
                    args.Result = true;
                else
                    args.Result = false;
            });
            Functions.Add("false", (args, cxt) =>
            {
                args.Result = false;
            });
            Functions.Add("if", (args, cxt) =>
            {
                if ((bool)EvaluateParameters(args, cxt, 0))
                {
                    args.Result = EvaluateParameters(args, cxt, 1);
                }
                else
                {
                    args.Result = EvaluateParameters(args, cxt, 2);
                }

            });
            Functions.Add("not", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                args.Result = !(bool)par1;
            });
            Functions.Add("or", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
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
            Functions.Add("true", (args, cxt) =>
            {
                args.Result = true;
            });
            #endregion Logical

            #region https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-string?tabs=json

            Functions.Add("base64", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                var plainTextBytes = Encoding.UTF8.GetBytes(par1 as string);
                args.Result = Convert.ToBase64String(plainTextBytes);
            });
            Functions.Add("base64tojson", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                var base64EncodedBytes = Convert.FromBase64String(par1 as string);
                args.Result = new JsonValue(Encoding.UTF8.GetString(base64EncodedBytes));
            });
            Functions.Add("base64tostring", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                var base64EncodedBytes = Convert.FromBase64String(par1 as string);
                args.Result = Encoding.UTF8.GetString(base64EncodedBytes);
            });
            // concat in array function group
            // contains in array function group
            Functions.Add("datauri", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                var plainTextBytes = Encoding.UTF8.GetBytes(par1 as string);
                args.Result = "data:text/plain;charset=utf8;base64," + Convert.ToBase64String(plainTextBytes);
            });
            Functions.Add("datauritostring", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                var s = (par1 as string);
                s = s[(s.LastIndexOf(',') + 1)..].Trim();
                var base64EncodedBytes = Convert.FromBase64String(s);
                args.Result = Encoding.UTF8.GetString(base64EncodedBytes);
            });
            // empty in array function group
            Functions.Add("endswith", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                args.Result = (pars[0] as string).EndsWith(pars[1] as string, StringComparison.InvariantCultureIgnoreCase);
            });
            // first in array function group           
            Functions.Add("format", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                args.Result = string.Format((pars[0] as string), pars.Skip(1).ToArray());
            });
            Functions.Add("guid", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] bytes = md5.ComputeHash(Encoding.Unicode.GetBytes(string.Join('-', pars)));
                args.Result = new Guid(bytes).ToString();
            });
            Functions.Add("indexof", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                args.Result = (pars[0] as string).IndexOf(pars[1] as string, StringComparison.InvariantCultureIgnoreCase);
            });
            // json in ojbect function group
            // last in array function group
            Functions.Add("lastindexof", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                args.Result = (pars[0] as string).LastIndexOf(pars[1] as string, StringComparison.InvariantCultureIgnoreCase);
            });
            // length in array function group
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-string?tabs=json#newguid
            // This function can only be used in the default value for a parameter.
            Functions.Add("newguid", (args, cxt) =>
            {
                var deployment = cxt[ContextKeys.ARM_CONTEXT] as Deployment;
                if (!cxt.TryGetValue(ContextKeys.FUNCTION_PATH, out object p))
                    throw new Exception("You can only use newGuid function within an expression for the default value of a parameter ");
                var segs = p.ToString().Split('/');
                if(segs.Length<3 || segs[0]!="parameters" || segs[2]!="defaultValue")
                    throw new Exception("You can only use newGuid function within an expression for the default value of a parameter ");
                if (!deployment.PersistenceValue.TryGetValue(p.ToString(), out object v))
                {
                    v = Guid.NewGuid().ToString();
                    args.Result = v;
                    deployment.PersistenceValue.Add(p.ToString(), v);
                }
                args.Result = v;
            });
            Functions.Add("padleft", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
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
                var pars = EvaluateParameters(args, cxt);
                args.Result = (pars[0] as string).Replace(pars[1] as string, pars[2] as string);
            });
            // skip in array function group
            Functions.Add("split", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
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
            Functions.Add("startswith", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                args.Result = (pars[0] as string).StartsWith(pars[1] as string, StringComparison.InvariantCultureIgnoreCase);
            });
            Functions.Add("string", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                args.Result = par1.ToString();
            });
            Functions.Add("substring", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt); ;
                var s = pars[0] as string;
                var startIndex = (int)pars[1];
                if (pars.Length > 2)
                {
                    args.Result = s.Substring(startIndex, (int)pars[2]);
                }
                else
                {
                    args.Result = s[startIndex..];
                }
            });
            // take in array function group
            Functions.Add("tolower", (args, cxt) =>
            {
                args.Result = EvaluateParameters(args, cxt, 0).ToString().ToLower();
            });
            Functions.Add("toupper", (args, cxt) =>
            {
                args.Result = EvaluateParameters(args, cxt, 0).ToString().ToUpper();
            });
            Functions.Add("trim", (args, cxt) =>
            {
                args.Result = EvaluateParameters(args, cxt, 0).ToString().Trim();
            });
            // https://stackoverflow.com/a/48305669
            Functions.Add("uniquestring", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
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
            Functions.Add("uri", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                args.Result = System.IO.Path.Combine(pars[0] as string, pars[1] as string);
            });
            Functions.Add("uricomponent", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                args.Result = Uri.EscapeDataString(par1 as string);
            });
            Functions.Add("uricomponenttostring", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                args.Result = Uri.UnescapeDataString(par1 as string);
            });
            #endregion String

            #region https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-numeric?tabs=json

            Functions.Add("add", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                args.Result = Convert.ToInt32(pars[0]) + Convert.ToInt32(pars[1]);
            });
            Functions.Add("copyindex", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
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
                var pars = EvaluateParameters(args, cxt);
                args.Result = Convert.ToInt32(pars[0]) / Convert.ToInt32(pars[1]);
            });
            Functions.Add("float", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt);
                args.Result = Convert.ToDecimal(par1);
            });
            Functions.Add("int", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                args.Result = Convert.ToInt32(par1);
            });
            // max in array function group
            // min in array function group
            Functions.Add("mod", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                var d = Math.DivRem(Convert.ToInt32(pars[0]), Convert.ToInt32(pars[1]), out int result);
                args.Result = result;
            });
            Functions.Add("mul", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                args.Result = Convert.ToInt32(pars[0]) * Convert.ToInt32(pars[1]);
            });
            Functions.Add("sub", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                args.Result = Convert.ToInt32(pars[0]) - Convert.ToInt32(pars[1]);
            });

            #endregion Numeric

            #region https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-object?tabs=json
            // contains in array function gruop
            Functions.Add("createobject", (args, cxt) =>
            {
                List<string> rtv = new List<string>();
                var pars = EvaluateParameters(args, cxt);
                JObject jo = new JObject();
                for (int i = 0; i < pars.Length - 1; i += 2)
                {
                    if (pars[i + 1] is JsonValue j)
                    {
                        if (j.ValueKind == JsonValueKind.Array)
                        {
                            jo.Add(pars[i].ToString(), JArray.Parse(j.RawString));
                        }
                        else
                        {
                            jo.Add(pars[i].ToString(), JObject.Parse(j.RawString));
                        }
                    }
                    else
                    {
                        jo.Add(pars[i].ToString(), JToken.FromObject(pars[i + 1]));
                    }

                }
                args.Result = new JsonValue(jo.ToString());
            });
            // empty  in array function gruop
            // intersection in array function gruop
            Functions.Add("json", (args, cxt) =>
            {
                var par1 = EvaluateParameters(args, cxt, 0);
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-expressions#null-values
                if (par1.ToString() == "null")
                    args.Result = null;
                else
                    args.Result = new JsonValue(par1.ToString());
            });
            // length in array function gruop
            Functions.Add("null", (args, cxt) =>
            {
                args.Result = null;
            });
            // union in array function gruop
            #endregion

            #region https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource?tabs=json

            Functions.Add("extensionresourceid", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
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
            // list* in Evaluate method
            Functions.Add("pickzones", (args, cxt) =>
            {
                // todo: pickZones
            });
            Functions.Add("providers", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                var taskResult = this.infrastructure.Providers(pars[0].ToString(), pars[1].ToString());
                args.Result = new JsonValue(taskResult.Content.ToString());
            });
            //https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions-resource?tabs=json#reference
            //The reference function can only be used in the properties of a resource definition and the outputs section of a template or deployment
            Functions.Add("reference", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                string resourceName = pars[0].ToString();
                var context = cxt[ContextKeys.ARM_CONTEXT] as Deployment;
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
                if (!(resourceName.StartsWith("/" + infrastructure.BuiltinPathSegment.ManagementGroup)
                        || resourceName.StartsWith("/" + infrastructure.BuiltinPathSegment.Subscription)
                        || resourceName.StartsWith(infrastructure.BuiltinPathSegment.ManagementGroup)
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
                    if (!(resourceName.StartsWith("/" + infrastructure.BuiltinPathSegment.ManagementGroup)
                        || resourceName.StartsWith("/" + infrastructure.BuiltinPathSegment.Subscription)
                        || resourceName.StartsWith(infrastructure.BuiltinPathSegment.ManagementGroup)
                        || resourceName.StartsWith(infrastructure.BuiltinPathSegment.Subscription)
                        || resourceName.StartsWith(infrastructure.BuiltinPathSegment.ResourceGroup)))
                    {
                        id = context.GetFirstResource(resourceName).ResourceId;
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
                var context = cxt[ContextKeys.ARM_CONTEXT] as Deployment;
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
            Functions.Add("resourceid", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                var input = cxt[ContextKeys.ARM_CONTEXT] as Deployment;
                var t = input.Template;
                if (t.DeployLevel == DeployLevel.ResourceGroup)
                    args.Result = ResourceId(input, pars);
                else if (t.DeployLevel == DeployLevel.Subscription)
                    args.Result = SubscriptionResourceId(input, pars);
                else
                    args.Result = TenantResourceId(pars);
            });
            Functions.Add("subscription", (args, cxt) =>
            {
                // todo: subscription
            });
            Functions.Add("subscriptionresourceid", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                var input = cxt[ContextKeys.ARM_CONTEXT] as Deployment;
                args.Result = SubscriptionResourceId(input, pars);
            });
            Functions.Add("tenantresourceid", (args, cxt) =>
            {
                args.Result = TenantResourceId(EvaluateParameters(args, cxt));
            });
            Functions.Add("managementgroupresourceid", (args, cxt) =>
            {
                var pars = EvaluateParameters(args, cxt);
                var input = cxt[ContextKeys.ARM_CONTEXT] as Deployment;
                args.Result = ManagementResourceId(input, pars);
            });
            #endregion Resource
        }
        internal FunctionArgs GetVariables(string name, Dictionary<string, object> cxt)
        {
            FunctionArgs rtv = new FunctionArgs();
            if (!cxt.TryGetValue(ContextKeys.ARM_CONTEXT, out object armcxt))
                throw new Exception("cannot find context in parameters function");
            var input = armcxt as Deployment;
            if (!string.IsNullOrEmpty(input.ParentId) &&
                (string.IsNullOrEmpty(input.ExpressionEvaluationOptions) || !input.ExpressionEvaluationOptions.Equals("inner", StringComparison.InvariantCultureIgnoreCase)))
            {
                Dictionary<string, object> parentCxt = new Dictionary<string, object>();
                foreach (var item in cxt)
                {
                    if (item.Key == ContextKeys.ARM_CONTEXT)
                        parentCxt.Add(ContextKeys.ARM_CONTEXT, input.Parent);
                    else
                        parentCxt.Add(item.Key, item.Value);
                }
                rtv = GetVariables(name, parentCxt);
            }
            if (!rtv.HasResult)
            {
                string path = $"variables/{name}";
                if (!string.IsNullOrEmpty(input.Template.Variables.ToString()))
                {
                    using var defineDoc = JsonDocument.Parse(input.Template.Variables.ToString());
                    var ele = defineDoc.RootElement;
                    if (ele.TryGetProperty(name, out JsonElement parEleDef))
                    {
                        if (parEleDef.ValueKind == JsonValueKind.Object)
                        {
                            rtv.Result = new JsonValue(parEleDef.ExpandObject(cxt, path));
                        }
                        else if (parEleDef.ValueKind == JsonValueKind.Array)
                        {
                            rtv.Result = new JsonValue(parEleDef.ExpandArray(cxt, path));
                        }
                        else
                            rtv.Result = JsonValue.GetElementValue(parEleDef);
                    }
                    else if (ele.TryGetProperty("copy", out JsonElement copyE))
                    {
                        foreach (var item in copyE.EnumerateArray())
                        {
                            if (item.TryGetProperty("name", out JsonElement nameE) && nameE.GetString() == name)
                            {
                                rtv.Result = ExpandCopy(item, cxt);
                            }
                        }
                    }
                }
                if (rtv.HasResult && rtv.Result is string s)
                {
                    rtv.Result = Evaluate(s, cxt,path);
                }
            }
            return rtv;
        }
        internal FunctionArgs GetParameter(string name, Dictionary<string, object> cxt)
        {
            if (!cxt.TryGetValue(ContextKeys.ARM_CONTEXT, out object armcxt))
                throw new Exception("cannot find context in parameters function");
            var input = armcxt as Deployment;
            FunctionArgs rtv = new FunctionArgs();
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/linked-templates#scope-for-expressions-in-nested-templates
            if (!string.IsNullOrEmpty(input.ParentId) &&
                (string.IsNullOrEmpty(input.ExpressionEvaluationOptions) || !input.ExpressionEvaluationOptions.Equals("inner", StringComparison.InvariantCultureIgnoreCase)))
            {
                Dictionary<string, object> parentCxt = new Dictionary<string, object>();
                foreach (var item in cxt)
                {
                    if (item.Key == ContextKeys.ARM_CONTEXT)
                        parentCxt.Add(ContextKeys.ARM_CONTEXT, input.Parent);
                    else if (item.Key == ContextKeys.UDF_CONTEXT)
                        continue;
                    else
                        parentCxt.Add(item.Key, item.Value);
                }
                rtv = GetParameter(name, parentCxt);
            }

            if (!rtv.HasResult)
            {
                string path = $"parameters/{name}";
                // this is User Defined Functions
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-user-defined-functions
                if (cxt.TryGetValue(ContextKeys.UDF_CONTEXT, out object udfContext))
                {
                    using var jsonDoc = JsonDocument.Parse(udfContext.ToString());
                    if (jsonDoc.RootElement.TryGetProperty(name, out JsonElement ele) && ele.TryGetProperty("value", out JsonElement v))
                        rtv.Result = JsonValue.GetElementValue(v);
                }
                else
                {
                    if (!string.IsNullOrEmpty(input.Parameters))
                    {
                        using var jsonDoc = JsonDocument.Parse(input.Parameters);
                        if (jsonDoc.RootElement.TryGetProperty(name, out JsonElement ele) && ele.TryGetProperty("value", out JsonElement v))
                        {
                            rtv.Result = JsonValue.GetElementValue(v);
                        }
                    }
                    if (!rtv.HasResult && !string.IsNullOrEmpty(input.Template.Parameters))
                    {
                        using var defineDoc = JsonDocument.Parse(input.Template.Parameters);
                        if (defineDoc.RootElement.TryGetProperty(name, out JsonElement parEleDef) && parEleDef.TryGetProperty("defaultValue", out JsonElement defValue))
                        {
                            rtv.Result = JsonValue.GetElementValue(defValue);
                            path += "/defaultValue";
                        }
                    }
                }
                // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/linked-templates#using-variables-to-link-templates
                // paramete's default value can be included function
                if (rtv.HasResult && rtv.Result is string s)
                {
                    rtv.Result = Evaluate(s, cxt,path);
                }
            }
            return rtv;
        }

        public string ResourceId(Deployment input, params object[] pars)
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

        public string SubscriptionResourceId(Deployment input, params object[] pars)
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

        public string ManagementResourceId(Deployment input, params object[] pars)
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

        public object Evaluate(string function, Dictionary<string, object> cxt, string path)
        {
            cxt.TryGetValue(ContextKeys.FUNCTION_PATH, out object p);
            cxt[ContextKeys.FUNCTION_PATH] = path;
            var r = Evaluate(function, cxt);
            cxt[ContextKeys.FUNCTION_PATH] = p;
            return r;
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
                            if (!(cxt[ContextKeys.ARM_CONTEXT] as Deployment).IsRuntime)
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
                                                                cxt[ContextKeys.ARM_CONTEXT] as Deployment,
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
            udfContext.Add(ContextKeys.UDF_CONTEXT, jObject.ToString(Formatting.Indented));

            args.Result = GetOutput(member.Output, udfContext);
        }

        private bool TryGetCustomFunction(string function, Dictionary<string, object> context, out Member member)
        {
            member = null;
            if (!context.TryGetValue(ContextKeys.ARM_CONTEXT, out object armcxt))
                return false;
            var input = armcxt as Deployment;
            if (!string.IsNullOrEmpty(input.ParentId) &&
               (string.IsNullOrEmpty(input.ExpressionEvaluationOptions) || !input.ExpressionEvaluationOptions.Equals("inner", StringComparison.InvariantCultureIgnoreCase)))
            {
                Dictionary<string, object> parentCxt = new Dictionary<string, object>();
                foreach (var item in context)
                {
                    if (item.Key == ContextKeys.ARM_CONTEXT)
                        parentCxt.Add(ContextKeys.ARM_CONTEXT, input.Parent);
                    else
                        parentCxt.Add(item.Key, item.Value);
                }
                if (TryGetCustomFunction(function, parentCxt, out Member parentMmeber))
                {
                    member = parentMmeber;
                    return true;
                }
            }
            var udfs = input.Template.Functions;
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

        public object[] EvaluateParameters(FunctionArgs args, Dictionary<string, object> context)
        {
            var values = new object[args.Parameters.Length];
            context.TryGetValue(ContextKeys.FUNCTION_PATH, out object p);
            for (int i = 0; i < values.Length; i++)
            {
                context[ContextKeys.FUNCTION_PATH] = $"{p}/par[{i}]";
                values[i] = args.Parameters[i].Evaluate(context);
            }
            context[ContextKeys.FUNCTION_PATH] = p;
            return values;
        }
        public object EvaluateParameters(FunctionArgs args, Dictionary<string, object> context, int index)
        {
            var p = context[ContextKeys.FUNCTION_PATH];
            context[ContextKeys.FUNCTION_PATH] = $"{p}/par[{index}]";
            var r = args.Parameters[index].Evaluate(context);
            context[ContextKeys.FUNCTION_PATH] = p;
            return r;
        }

        public JsonValue ExpandCopy(JsonElement item, Dictionary<string, object> context)
        {
            string path = $"{context[ContextKeys.FUNCTION_PATH]}.copy";
            using MemoryStream ms = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);

            Copy copy = copy = new Copy(JObject.Parse(item.GetRawText()), context);
            var copyindex = new Dictionary<string, int>() { { copy.Name, 0 } };
            Dictionary<string, object> copyContext = new Dictionary<string, object>
                        {
                            { "copyindex", copyindex },
                            { "currentloopname", copy.Name }
                        };
            foreach (var k in context.Keys)
            {
                copyContext.Add(k, context[k]);
            }
            writer.WriteStartArray();
            if (!item.TryGetProperty("input", out JsonElement inputE))
            {
                throw new Exception($"cannot find input property in path: {path}");
            }
            for (int i = 0; i < copy.Count; i++)
            {
                copyindex[copy.Name] = i;
                writer.WriteElement(inputE, copyContext, path);
            }

            writer.WriteEndArray();
            if (copyContext.TryGetValue(ContextKeys.DEPENDSON, out object copyDependsOn))
            {
                List<string> dependsOn;
                if (context.TryGetValue(ContextKeys.DEPENDSON, out object d))
                {
                    dependsOn = d as List<string>;
                }
                else
                {
                    dependsOn = new List<string>();
                    context.Add(ContextKeys.DEPENDSON, dependsOn);
                }
                dependsOn.AddRange(copyDependsOn as List<string>);
            }
            writer.Flush();
            return new JsonValue(Encoding.UTF8.GetString(ms.ToArray()));
        }
    }
}