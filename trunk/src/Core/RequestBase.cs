﻿/**
 * RequestBase.cs
 *
 * Copyright (C) 2008,  iron9light
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Text;

namespace Google.API
{
    internal abstract class RequestBase
    {
        #region Fields

        private string m_UrlString;

        #endregion

        #region Constructors

        protected RequestBase(string content)
        {
            Content = content;
        }

        protected RequestBase(string content, string key)
        {
            Content = content;
            Key = key;
        }

        protected RequestBase(string content, string version, string key)
        {
            Content = content;
            Version = version;
            Key = key;
        }

        #endregion

        #region Properties

        [Argument("q", Optional = false, NeedEncode = true)]
        public string Content { get; protected set; }

        [Argument("v", "1.0")]
        public string Version { get; protected set; }

        [Argument("key")]
        public string Key { get; protected set; }

        /// <summary>
        /// Get the url string.
        /// </summary>
        public string UrlString
        {
            get
            {
                if (m_UrlString == null)
                {
                    m_UrlString = GetUrlString();
                }
                return m_UrlString;
            }
        }

        /// <summary>
        /// Get the uri.
        /// </summary>
        public Uri Uri
        {
            get
            {
                return new Uri(UrlString);
            }
        }

        protected abstract string BaseAddress { get; }

        #endregion

        #region Methods

        /// <summary>
        /// Get the web request.
        /// </summary>
        /// <returns>The web request.</returns>
        public WebRequest GetWebRequest()
        {
            return WebRequest.Create(UrlString);
        }

        /// <summary>
        /// Get the web request with time out.
        /// </summary>
        /// <param name="timeout">The length of time, in milliseconds, before the request times out.</param>
        /// <returns>The web request.</returns>
        public WebRequest GetWebRequest(int timeout)
        {
            WebRequest request = GetWebRequest();
            request.Timeout = timeout;
            return request;
        }

        public override string ToString()
        {
            return UrlString;
        }

        private ICollection<KeyValuePair<string, string>> GetArguments()
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            PropertyInfo[] properties = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo info in properties)
            {
                object[] attrs = info.GetCustomAttributes(typeof(ArgumentAttribute), true);
                if (attrs.Length == 0)
                {
                    continue;
                }
                ArgumentAttribute argAttr = attrs[0] as ArgumentAttribute;
                string name = argAttr.Name;
                object value = info.GetValue(this, null);
                if (value == null)
                {
                    if (!argAttr.Optional)
                    {
                        value = argAttr.DefaultValue;
                    }
                    else
                    {
                        continue;
                    }
                }
                if (value == null)
                {
                    throw new GoogleAPIException(string.Format("Property {0}({1}) cannot be null", info.Name, name));
                }

                string valueString = GetValueString(value, info.PropertyType);
                if(valueString == null)
                {
                    continue;
                }

                if (argAttr.NeedEncode)
                {
                    valueString = HttpUtility.UrlEncode(valueString);
                }

                dict[name] = valueString;
            }
            return dict;
        }

        private static string GetValueString(object value, Type valueType)
        {
            string valueString = null;
            if (valueType == typeof(bool))
            {
                // bool value
                if ((bool)value)
                {
                    valueString = "1";
                }
            }
            else if (valueType.IsEnum)
            {
                // enum value
                if ((int)value != 0)
                {
                    valueString = value.ToString();
                }
            }
            else
            {
                valueString = value.ToString();
            }
            return valueString;
        }

        private string GetUrlString()
        {
            ICollection<KeyValuePair<string, string>> arguments = GetArguments();
            if (arguments.Count == 0)
            {
                return BaseAddress;
            }

            StringBuilder sb = new StringBuilder(BaseAddress);
            sb.Append("?");
            bool isFirst = true;
            foreach (KeyValuePair<string, string> argument in arguments)
            {
                if (!isFirst)
                {
                    sb.Append("&");
                }
                else
                {
                    isFirst = false;
                }
                sb.Append(argument.Key);
                sb.Append("=");
                sb.Append(argument.Value);
            }
            return sb.ToString();
        }

        #endregion
    }
}
