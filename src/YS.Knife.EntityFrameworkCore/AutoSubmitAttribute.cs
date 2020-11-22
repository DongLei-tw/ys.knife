﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AspectCore.DynamicProxy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using YS.Knife.Aop;
using Microsoft.EntityFrameworkCore.Storage;

namespace YS.Knife.EntityFrameworkCore
{
    public class AutoSubmitAttribute : BaseAopAttribute
    {
        public AutoSubmitAttribute()
        {
            this.Order = 10000;
        }
        public async override Task Invoke(AspectContext context, AspectDelegate next)
        {
            var services = context?.ServiceProvider.GetService<IEnumerable<IAutoSubmitContext>>().ToList(); ;
            if (services.Count == 0)
            {
                await next?.Invoke(context);
            }
            else if (services.Count == 1)
            {
                await next?.Invoke(context);
                var dbcontext = services.First().DbContext;
                if (dbcontext.ChangeTracker.HasChanges())
                {
                    await services.First().DbContext.SaveChangesAsync();
                }
            }
            else
            {
                await next?.Invoke(context);
                var allChangedContext = services.Where(p => p.DbContext.ChangeTracker.HasChanges())
                                .Select(p => p.DbContext)
                                .Distinct()
                                .ToList();
                if (allChangedContext.Count > 1)
                {
                    var tran = await allChangedContext.First().Database.BeginTransactionAsync();
                    var dbtran = tran.GetDbTransaction();
                    for (int i = 1; i < allChangedContext.Count; i++)
                    {
                        allChangedContext[i].Database.UseTransaction(dbtran);
                    }
                    try
                    {
                        foreach (var efcontext in allChangedContext)
                        {
                            await efcontext.SaveChangesAsync();
                        }
                        dbtran.Commit();
                    }
                    catch (Exception)
                    {
                        dbtran.Rollback();
                        throw;
                    }
                }
                else
                {
                    await allChangedContext.First().SaveChangesAsync();
                }
            }
        }
    }

    public interface IAutoSubmitContext
    {
        DbContext DbContext { get; }
    }
}
