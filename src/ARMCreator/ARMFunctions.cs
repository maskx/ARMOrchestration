using maskx.Expression;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

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
                if (par1 is string || par1 is bool)
                {
                    str = $"[\"{par1}\"]";
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
                if (pars[0] is string)
                {
                    if ((pars[0] as string).IndexOf(pars[1] as string) > 0)
                        args.Result = true;
                }
                else if (pars[0] is JsonValue)
                {
                    args.Result = (pars[0] as JsonValue).Contains(pars[1]);
                }
            });
            #endregion Array and object

            #region Logical functions

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

            #endregion Logical functions

            #region Deployment value

            Functions.Add("parameters", (args, cxt) =>
            {
                if (!cxt.TryGetValue("parametersDefine", out object pds))
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
            });

            #endregion Deployment value

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
                    if (p.RawString == "{}" || p.RawString == "[]")
                        args.Result = true;
                }
                else if (par1 is string && string.IsNullOrEmpty(par1 as string))
                    args.Result = true;

            });

            #endregion String
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
    }
}