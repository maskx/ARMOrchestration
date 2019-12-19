using maskx.Expression;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace maskx.OrchestrationCreator
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
                if (pars[0] is string)
                    args.Result = string.Join("", pars);
                else
                    args.Result = new JsonValue($"[{string.Join(",", pars)}]");
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
                if (!cxt.TryGetValue("parametersdefine", out object pds))
                    return;
                if (string.IsNullOrEmpty(pds.ToString()))
                {
                    throw new Exception("ARM Template does not define the parameters");
                }
                var par1 = args.Parameters[0].Evaluate(cxt).ToString();
                using var defineDoc = JsonDocument.Parse(pds.ToString());
                if (!defineDoc.RootElement.TryGetProperty(par1, out JsonElement parEleDef))
                {
                    throw new Exception($"ARM Template does not define the parameter:{par1}");
                }
                if (cxt.TryGetValue("parameters", out object parameters))
                {
                    if (!string.IsNullOrEmpty(parameters.ToString()))
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
                if (!args.HasResult && parEleDef.TryGetProperty("defaultValue", out JsonElement defValue))
                {
                    args.Result = JsonValue.GetElementValue(defValue);
                }
                if (args.Result is string s)
                    args.Result = Run(s, cxt);
            });
            Functions.Add("variables", (args, cxt) =>
            {
                if (!cxt.TryGetValue("variabledefine", out object pds))
                    return;
                if (string.IsNullOrEmpty(pds.ToString()))
                {
                    throw new Exception("ARM Template does not define the variables");
                }
                var par1 = args.Parameters[0].Evaluate(cxt).ToString();
                using var defineDoc = JsonDocument.Parse(pds.ToString());
                if (!defineDoc.RootElement.TryGetProperty(par1, out JsonElement parEleDef))
                {
                    throw new Exception($"ARM Template does not define the variables:{par1}");
                }
                args.Result = JsonValue.GetElementValue(parEleDef);
                if (args.Result is string s)
                    args.Result = Run(s, cxt);
            });

            #endregion Deployment

            #region String

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

            #endregion String

            #region Numeric

            Functions.Add("add", (args, cxt) =>
            {
                var pars = args.EvaluateParameters(cxt);
                args.Result = Convert.ToInt32(pars[0]) + Convert.ToInt32(pars[1]);
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
        }

        public static object Run(string function, Dictionary<string, object> context)
        {
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
                        }
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
    }
}