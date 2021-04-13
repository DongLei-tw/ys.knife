﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace YS.Knife.Aop
{
    public class StringResourcesAttribute : DynamicProxyAttribute
    {
        public StringResourcesAttribute() : base(ServiceLifetime.Singleton)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class SrAttribute : BaseAopAttribute
    {
        public SrAttribute(string key, string defaultValue)
        {
            this.Key = key;
            this.Value = defaultValue;
        }

        public string Key { get; }

        public string Value { get; }

        public override Task Invoke(AspectContext context, AspectDelegate next)
        {
            var type = typeof(IStringLocalizer<>).MakeGenericType(context.ServiceMethod.DeclaringType);
            var localizer = context.ServiceProvider.GetRequiredService(type) as IStringLocalizer;
            var resourceKey = string.IsNullOrEmpty(Key) ? context.ServiceMethod.Name : Key;
            var localizedString = localizer.GetString(resourceKey);
            var template = localizedString.ResourceNotFound ? this.Value : localizedString.Value;
            context.ReturnValue = FormatTemplate(template, context);
            return context.Break();
        }


        private string FormatTemplate(string template, AspectContext context)
        {
            var formatter = ValuesFormatter.FromText(template ?? string.Empty);
            var kwArgs = context.ServiceMethod.GetParameters()
                .Zip(context.Parameters, (pInfo, val) => new KeyValuePair<string, object>(pInfo.Name, val))
                .ToList();
            return formatter.Format(kwArgs);
        }
    }


}
