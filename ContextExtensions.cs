using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EntityFrameworkCore
{
   public static class ContextExtensions
    {
        private static MethodInfo method= typeof(DbContext).GetMethod("Set");
        public static IQueryable<object> Set(this DbContext _context, Type t)
        {

            return (IQueryable<object>)method.MakeGenericMethod(t).Invoke(_context, null);
        }
    }
}
