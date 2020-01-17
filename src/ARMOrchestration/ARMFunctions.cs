using maskx.Expression;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using maskx.ARMOrchestration.Orchestrations;

namespace maskx.ARMOrchestration
{
    public static class ARMFunctions
    {
        private static Dictionary<string, Action<FunctionArgs, Dictionary<string, object>>> Functions = new Dictionary<string, Action<FunctionArgs, Dictionary<string, object>>>();

        static ARMFunctions()
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
                args.Result = new JsonValue(par1.ToString());
            });
            Functions.Add("last", (args, cxt) =>
            {
                var par1 = args.Parameters[0].Evaluate(cxt);
                if (par1 is string s)
                    args.Result = s.Last().ToString();
                else if (par1 is JsonValue jv)
                    args.Result = jv[jv.Length - 1];
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
                var pars = args.EvaluateParameters(cxt);
                if ((bool)pars[0])
                    args.Result = pars[1];
                else
                    args.Result = pars[2];
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
                if (cxt.TryGetValue("parameters", out object parameters))
                {
                    if (parameters != null && !string.IsNullOrEmpty(parameters.ToString()))
                    {
                        using var jsonDoc = JsonDocument.Parse(parameters.ToString());
                        if (jsonDoc.RootElement.TryGetProperty(par1, out JsonElement ele))
                        {
                            if (ele.TryGetProperty("value", out JsonElement v))
                            {
                                args.Result = JsonValue.GetElementValue(v);
                            }
                        }
                    }
                }
                if (!args.HasResult && cxt.TryGetValue("armcontext", out object armcxt))
                {
                    var pds = new ARMTemplate.Template((armcxt as TemplateOrchestrationInput).Template, cxt).Parameters;
                    using var defineDoc = JsonDocument.Parse(pds.ToString());
                    if (!defineDoc.RootElement.TryGetProperty(par1, out JsonElement parEleDef))
                    {
                        throw new Exception($"ARM Template does not define the parameter:{par1}");
                    }
                    if (parEleDef.TryGetProperty("defaultValue", out JsonElement defValue))
                    {
                        args.Result = JsonValue.GetElementValue(defValue);
                    }
                }
                if (args.Result is string s)
                    args.Result = Evaluate(s, cxt);
            });
            Functions.Add("variables", (args, cxt) =>
            {
                if (!cxt.TryGetValue("armcontext", out object armcxt))
                    return;
                string vars = new ARMTemplate.Template((armcxt as TemplateOrchestrationInput).Template, cxt).Variables;

                var par1 = args.Parameters[0].Evaluate(cxt).ToString();
                using var defineDoc = JsonDocument.Parse(vars);
                if (!defineDoc.RootElement.TryGetProperty(par1, out JsonElement parEleDef))
                {
                    throw new Exception($"ARM Template does not define the variables:{par1}");
                }
                args.Result = JsonValue.GetElementValue(parEleDef);
                if (args.Result is string s)
                    args.Result = Evaluate(s, cxt);
            });
            Functions.Add("deployment", (args, cxt) =>
            {
                TemplateOrchestrationInput input = cxt["armcontext"] as TemplateOrchestrationInput;
                JObject obj = new JObject();
                obj.Add("name", input.Name);
                JObject properties = new JObject();
                properties.Add("template", JObject.Parse(input.Template));
                properties.Add("parameters", JObject.Parse(input.Parameters));
                properties.Add("mode", input.Mode);
                // TODO: Set provisioningState
                properties.Add("provisioningState", "Accepted");
                properties.Add("templateLink", new JObject("uri", input.TemplateLink));
                obj.Add("properties", properties);
                args.Result = new JsonValue(obj.ToString(Newtonsoft.Json.Formatting.None));
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
                    loopName = cxt["currentloopname"].ToString();
                }
                else if (pars.Length == 1)
                {
                    if (pars[0] is string s)
                    {
                        loopName = s;
                    }
                    else
                    {
                        loopName = cxt["currentloopname"].ToString();
                        offset = (int)pars[0];
                    }
                }
                else
                {
                    loopName = pars[0] as string;
                    offset = (int)pars[1];
                }
                args.Result = (cxt["copyindex"] as Dictionary<string, int>)[loopName] + offset;
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
                IEnumerable<object> nestResources = pars.Skip(3);
                foreach (var item in nestResources)
                {
                    nestr += $"/{fullnames[typeIndex]}/{item}";
                    typeIndex++;
                }
                args.Result = $"{pars[0]}/providers/{fullnames[0]}/{fullnames[1]}/{pars[2]}/{nestr}";
            });
            Functions.Add("resourceid", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                var input = cxt["armcontext"] as TemplateOrchestrationInput;
                var t = new ARMTemplate.Template(input.Template, cxt);
                if (t.DeployLevel == ARMTemplate.Template.ResourceGroupDeploymentLevel)
                    args.Result = resourceId(input, pars);
                else if (t.DeployLevel == ARMTemplate.Template.SubscriptionDeploymentLevel)
                    args.Result = subscriptionResourceId(input, pars);
                else
                    args.Result = tenantResourceId(pars);
            });
            Functions.Add("subscriptionresourceid", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                var input = cxt["armcontext"] as TemplateOrchestrationInput;
                args.Result = subscriptionResourceId(input, pars);
            });
            Functions.Add("tenantresourceid", (args, cxt) =>
            {
                args.Result = tenantResourceId(args.EvaluateParameters(cxt));
            });

            #endregion Resource
        }

        public static string resourceId(TemplateOrchestrationInput input, params object[] pars)
        {
            string subscriptionId = input.SubscriptionId;
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
                subscriptionId = subid.ToString();
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
            return $"/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/{fullnames[0]}/{fullnames[1]}/{resource}{nestr}";
        }

        public static string subscriptionResourceId(TemplateOrchestrationInput input, params object[] pars)
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
            return $"/subscriptions/{subscriptionId}/providers/{fullnames[0]}/{fullnames[1]}/{resource}{nestr}";
        }

        public static string tenantResourceId(params object[] pars)
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
            return $"/providers/{fullnames[0]}/{fullnames[1]}/{resource}{nestr}";
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="function"></param>
        /// <param name="context">
        /// parametersdefine
        /// variabledefine
        /// functionsdefine
        /// parameters
        /// </param>
        /// <returns></returns>
        public static object Evaluate(string function, Dictionary<string, object> context)
        {
            // https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-expressions#escape-characters
            if (function.StartsWith("[") && function.EndsWith("]") && !function.StartsWith("[["))
            {
                string functionString = function.TrimStart('[').TrimEnd(']');
                var expression = new maskx.Expression.Expression(functionString)
                {
                    EvaluateFunction = (name, args, cxt) =>
                    {
                        if (Functions.TryGetValue(name.ToLower(), out Action<FunctionArgs, Dictionary<string, object>> func))
                        {
                            func(args, cxt);
                            return;
                        }
                        if (!cxt.TryGetValue("armcontext", out object armcxt))
                            return;
                        var udfs = new ARMTemplate.Template((armcxt as TemplateOrchestrationInput).Template, cxt).Functions;
                        if (udfs == null)
                            return;
                        var names = name.Split('.');
                        var ns = udfs[names[0]];
                        if (ns == null)
                            return;

                        var member = ns[names[1]];
                        Expression.Expression expression1 = new Expression.Expression(member.Output);
                        Dictionary<string, object> udfContext = new Dictionary<string, object>();
                        using JsonDocument udfPar = JsonDocument.Parse(member.Parameters);
                        var rootEle = udfPar.RootElement;
                        var pars = args.EvaluateParameters(cxt);
                        JObject jObject = new JObject();
                        for (int i = 0; i < pars.Length; i++)
                        {
                            var t = rootEle[i].GetProperty("type").GetString();
                            JProperty p = null;
                            if ("object" == t)
                                p = new JProperty("value", JObject.Parse(pars[i].ToString()));
                            else if ("array" == t)
                                p = new JProperty("value", JArray.Parse(pars[i].ToString()));
                            else
                                p = new JProperty("value", pars[i]);
                            jObject.Add(rootEle[i].GetProperty("name").GetString(), new JObject(p));
                        }
                        udfContext.Add("parameters", jObject.ToString(Newtonsoft.Json.Formatting.None));
                        args.Result = GetOutput(member.Output, udfContext);
                    }
                };
                return expression.Evaluate(context);
            }
            return function;
        }

        public static void SetFunction(string name, Action<FunctionArgs, Dictionary<string, object>> func)
        {
            Functions[name] = func;
        }

        public static object GetOutput(string output, Dictionary<string, object> context)
        {
            using JsonDocument outDoc = JsonDocument.Parse(output);
            var rootEle = outDoc.RootElement;
            var type = rootEle.GetProperty("type").GetString();
            var value = rootEle.GetProperty("value").GetString();
            var v = ARMFunctions.Evaluate(value, context);
            return v;
        }

        public static string GetOutputs(string outputs, Dictionary<string, object> context)
        {
            using JsonDocument outDoc = JsonDocument.Parse(outputs);
            var outputDefineElement = outDoc.RootElement;
            JObject jOutput = new JObject();
            foreach (var item in outputDefineElement.EnumerateObject())
            {
                var v = item.Value.GetProperty("value");
                switch (v.ValueKind)
                {
                    case JsonValueKind.Undefined:
                        jOutput.Add(item.Name, JValue.CreateUndefined());
                        break;

                    case JsonValueKind.Object:
                        jOutput.Add(item.Name, JObject.Parse(v.GetRawText()));
                        break;

                    case JsonValueKind.Array:
                        jOutput.Add(item.Name, JArray.Parse(v.GetRawText()));
                        break;

                    case JsonValueKind.String:
                        var r = ARMFunctions.Evaluate(v.GetString(), context);
                        var t = item.Value.GetProperty("type").GetString().ToLower();
                        if ("object" == t)
                            jOutput.Add(item.Name, JObject.Parse(r.ToString()));
                        else if ("array" == t)
                            jOutput.Add(item.Name, JArray.Parse(r.ToString()));
                        else
                            jOutput.Add(item.Name, new JValue(r));
                        break;

                    case JsonValueKind.Number:
                        jOutput.Add(item.Name, v.GetInt32());
                        break;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        jOutput.Add(item.Name, v.GetBoolean());
                        break;

                    case JsonValueKind.Null:
                        jOutput.Add(item.Name, JValue.CreateNull());
                        break;

                    default:
                        break;
                }
            }
            return jOutput.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
}