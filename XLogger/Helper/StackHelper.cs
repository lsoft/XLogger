using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace XLogger.Helper
{
    /// <summary>
    /// Stack-related helper.
    /// </summary>
    public static class StackHelper
    {
        public static int StackFrame
        {
            get;
            set;
        }

        static StackHelper()
        {
            StackHelper.StackFrame = 2;
        }

        internal static string GetClassNameFromStack()
        {
            var result = "N/A";

#if !WindowsCE
            try
            {
                var stack = new StackTrace(0, true);

                var method = stack.GetFrame(StackHelper.StackFrame).GetMethod();
                result = GetClassName(method.DeclaringType);
            }
            catch
            {
                //nothing can be done here: it's logger, no way to log any logger error
            }
#endif

            return
                result;
        }

#if !WindowsCE
        private static string GetClassName(Type type)
        {
            string result;

            try
            {
                if (type.IsGenericType)
                {
                    var p = string.Join(
                        ",",
                        type
                            //.GetGenericTypeDefinition()
                            .GetGenericArguments()
                            .ToList()
                            .ConvertAll(j => GetClassName(j))
                            .ToArray());

                    var ppp = FullNameConverter(type.Name);
                    ppp = ppp.Substring(0, ppp.IndexOf('`'));
                    ppp = ParameterTypeStringConverter(ppp);

                    result = string.Format(
                        "{0}<{1}>",
                        ppp,
                        p);
                }
                else
                {
                    result = FullNameConverter(type.Name);
                }
            }
            catch
            {
                result = type.Name;
            }

            return result;

        }

        private static string FullNameConverter(string fullname)
        {
            //вложенные классы в рефлексии отображаются через знак плюс
            var fn0 = fullname.Replace(
                "+",
                "."
                );

            return fn0;
        }

        private static string ParameterTypeStringConverter(string pt)
        {
            var pt0 = pt.Replace(
                "&",
                string.Empty);

            var pt1 = pt0.Replace(
                "+",
                ".");

            return pt1;
        }
#endif

    }
}
